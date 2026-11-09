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
using Gurux.Common;
using Gurux.DLMS.AMI.Client.Pages.User;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading;

namespace Gurux.DLMS.AMI.Server.Services
{
    /// <inheritdoc/>
    internal sealed class GXTaskLateBindService(IServiceProvider serviceProvider,
        ILogger<GXTaskLateBindService> logger) : BackgroundService, ITaskLateBindHandler
    {
        private sealed record GXUserTask(ClaimsPrincipal user, GXTask[] tasks);
        private readonly System.Threading.Channels.Channel<GXUserTask> _tasks =
            System.Threading.Channels.Channel.CreateUnbounded<GXUserTask>(new() { SingleReader = true });

        public void Add(ClaimsPrincipal user, GXTask item) => AddRange(user, [item]);

        public void AddRange(ClaimsPrincipal user, IEnumerable<GXTask> tasks)
        {
            // Capture identities and enumerate lazy input in the caller's scope.
            var captured = new ClaimsPrincipal(user.Identities.Select(identity => identity.Clone()));
            if (!_tasks.Writer.TryWrite(new GXUserTask(captured, tasks.ToArray())))
            {
                throw new InvalidOperationException("Late-bound tasks are no longer accepting work.");
            }
        }

        /// <summary>
        /// Wait for workflow events and execute them.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var task in _tasks.Reader.ReadAllAsync(stoppingToken))
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    try
                    {
                        await serviceProvider.RunAsUserAsync(task.user,
                            services => AddTasksAsync(services, task, stoppingToken), stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Error reporting can itself fail; keep processing subsequent items.
                        logger.LogError(ex, "Failed to process late-bound tasks.");
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _tasks.Writer.TryComplete();
            return base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _tasks.Writer.TryComplete();
            base.Dispose();
        }

        private static async Task AddTasksAsync(IServiceProvider services, GXUserTask task, CancellationToken cancellationToken)
        {
            try
            {
                ITaskRepository repository = services.GetRequiredService<ITaskRepository>();
                await repository.AddAsync(task.tasks, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                //Add user error.
                ILogRepository repository = services.GetRequiredService<ILogRepository>().ForSource("user-error");
                var err = new GXLog()
                {
                    User = new Shared.DTOs.Authentication.GXUser()
                    {
                        Id = ServerHelpers.GetUserId(task.user),
                    },
                    Type = (int)TraceLevel.Error,
                    Message = ex.Message,
                };
                await repository.AddAsync(TargetType.Task, [err], cancellationToken);
                var authorizationService = services.GetRequiredService<IAuthorizationService>();
                if (await authorizationService.CanAccessPolicyAsync(task.user, GXSystemLogPolicies.Add))
                {
                    var se = services.GetRequiredService<ILogRepository>().ForSource("system");
                    await se.AddAsync(TargetType.Task, ex, cancellationToken);
                }
            }
        }
    }
}
