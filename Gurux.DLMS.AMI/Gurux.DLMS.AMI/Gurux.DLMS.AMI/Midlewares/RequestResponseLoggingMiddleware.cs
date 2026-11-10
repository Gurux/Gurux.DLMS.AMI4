// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Module;
using System.Text.Json;
using Gurux.DLMS.AMI.Server.Services;

namespace Gurux.DLMS.AMI.Server.Midlewares
{
    /// <summary>
    /// This class is used to save all user actions for the User log table.
    /// </summary>
    internal class RequestResponseLoggingMiddleware
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
        private readonly IGXAmiModuleServices _modules;
        private readonly GXDiagnosticsService _diagnostics;
        private StatisticSettings statistic => _diagnostics.Settings;

        public IDbConnectionFactory DbFactory => _dbFactory;

        public RequestResponseLoggingMiddleware(RequestDelegate next,
            IDbConnectionFactory dbFactory,
            ILogger<RequestResponseLoggingMiddleware> logger,
            GXDiagnosticsService diagnostics,
            IGXAmiModuleServices modules)
        {
            _next = next;
            _dbFactory = dbFactory;
            _logger = logger;
            _diagnostics = diagnostics;
            _modules = modules;
        }
        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/api/extensions/KeyManagement", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.StartsWithSegments("/api/PasswordChange", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.StartsWithSegments("/api/KeyManagement", StringComparison.OrdinalIgnoreCase))
            {
                // These routes can carry imported secrets or one-exchange material.
                // KeyService writes a metadata-only audit instead of HTTP body logs.
                context.Response.Headers.CacheControl = "no-store";
                await _next(context);
                return;
            }
            if (!context.Request.Path.StartsWithSegments("/_framework"))
            {
                DateTime start = DateTime.Now;
                long startedAt = System.Diagnostics.Stopwatch.GetTimestamp();
                CrudAction action = CrudAction.None;
                string? type = null;
                string? id = ServerHelpers.GetUserId(context.User, false);
                bool isApi = context.Request.Path.StartsWithSegments("/api");
                //Save user actions.
                if (id != null && statistic != null && isApi)
                {
                    string[]? items = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (items != null && items.Length == 3)
                    {
                        type = items[1].ToLowerInvariant();
                        if (string.Compare(items[2], "add", true) == 0)
                        {
                            action = CrudAction.Create;
                        }
                        else if (!Enum.TryParse(items[2], true, out action) || !Enum.IsDefined(action))
                        {
                            action = CrudAction.None;
                            if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
                            {
                                _logger.LogInformation("Unknown action '{Value0}'", context.Request.Path.Value);
                            }
                        }
                    }
                    else
                    {
                        if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
                        {
                            _logger.LogInformation("Unknown path '{Value0}'", context.Request.Path.Value);
                        }
                    }
                }

                if (action != CrudAction.None && type != null)
                {
                    // A lifecycle mutation may stop UserActivity itself and must not wait for its own lease.
                    var moduleLifecycleChange = type == "module" &&
                        action is CrudAction.Create or CrudAction.Update or CrudAction.Delete or CrudAction.Clear;
                    if (id != null && _modules.IsAvailable<IGXAmiUserActivity>() && !moduleLifecycleChange)
                    {
                        await LogUserActivityAsync(context, action, type, startedAt);
                    }
                    else
                    {
                        await _next(context);
                    }
                }
                else
                {
                    await _next(context);
                }
                var requestElapsed = System.Diagnostics.Stopwatch.GetElapsedTime(startedAt);
                try
                {
                    if (action != CrudAction.None && type != null)
                    {
                        await _diagnostics.RecordAsync(id, type, action, context.Response.StatusCode, requestElapsed, context.RequestAborted);
                    }
                    if (id != null && isApi && statistic != null && requestElapsed.TotalMilliseconds >= statistic.SlowRequestThresholdMs)
                    {
                        await LogSlowRequestAsync(context, id, start, requestElapsed);
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to record REST request diagnostics.");
                }
            }
            else
            {
                await _next(context);
            }
        }

        private async Task LogSlowRequestAsync(HttpContext context, string userId, DateTime started, TimeSpan elapsed)
        {
            var duration = (int)Math.Min(int.MaxValue, elapsed.TotalMilliseconds);
            var entry = new GXLog(System.Diagnostics.TraceLevel.Warning)
            {
                CreationTime = new DateTimeOffset(started).ToUniversalTime(),
                User = new GXUser { Id = userId },
                Duration = duration,
                Status = context.Response.StatusCode,
                TargetType = TargetType.SystemLog,
                Message = $"Slow REST request: {context.Request.Method} {context.Request.Path} ({duration} ms).",
                Data = JsonSerializer.Serialize(new
                {
                    context.Request.Method,
                    Path = context.Request.Path.Value,
                    context.TraceIdentifier
                })
            };
            var enumTypes = context.RequestServices.GetRequiredService<IEnumTypeRepository>();
            entry.Type = await enumTypes.GetLogTypeAsync(TargetType.SystemLog, "slow-rest-request", CancellationToken.None);
            await using var connection = await _dbFactory.GetConnectionAsync();
            await GXLogStore.InsertAsync(connection, "system", [entry]);
        }

        private async Task LogUserActivityAsync(HttpContext context, CrudAction action, string target, long startedAt)
        {
            var invoked = await _modules.TryInvokeWithContextAsync<IGXAmiUserActivity>((service, invocation) =>
                service.InvokeAsync(context, http => invocation.RunDownstreamAsync(() => _next(http)),
                    target, (byte)action, startedAt, invocation.RequestCancellationToken), context.RequestAborted);
            if (!invoked)
            {
                await _next(context);
            }
        }
    }
}
