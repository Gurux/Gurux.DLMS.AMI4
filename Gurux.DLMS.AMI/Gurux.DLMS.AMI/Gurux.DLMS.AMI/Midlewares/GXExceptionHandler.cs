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
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Midlewares
{
    /// <summary>Provides exception handler.</summary>
    /// <remarks>
    /// Initializes a new instance of the GXExceptionHandler class.
    /// </remarks>
    /// <param name="logger">The logger used to record exception details.</param>
    /// <param name="dbFactory">The factory for creating database connections.</param>
    public sealed class GXExceptionHandler(ILogger<GXExceptionHandler> logger,
        IDbConnectionFactory dbFactory) : IExceptionHandler
    {
        private readonly ILogger<GXExceptionHandler> _logger = logger;
        private readonly IDbConnectionFactory _dbFactory = dbFactory;

        /// <inheritdoc/>
        public async ValueTask<bool> TryHandleAsync(
            HttpContext context,
            Exception exception,
            CancellationToken cancellationToken)
        {
            if (exception is BadHttpRequestException)
            {
                _logger.LogWarning(exception, "Invalid HTTP request at {Path}", context.Request.Path);
            }
            else if (exception is GXAmiNotFoundException)
            {
                _logger.LogWarning("AMI resource was not found at {Path}", context.Request.Path);
            }
            else if (exception is Gurux.DLMS.AMI.Module.AmiModuleUnavailableException)
            {
                _logger.LogWarning(exception, "AMI module is unavailable");
            }
            else if (exception is GXAmiForbiddenException or UnauthorizedAccessException)
            {
                _logger.LogWarning(exception, "AMI authorization denied");
            }
            else
            {
                _logger.LogError(exception, "User exception");
            }
            if (context.Response.HasStarted)
            {
                return false;
            }
            var serviceProvider = context.RequestServices;
            var problem = await CreateProblemDetailsAsync(_logger, exception, context, serviceProvider);
            context.Response.Clear();
            context.Response.StatusCode = problem.Status.GetValueOrDefault(StatusCodes.Status500InternalServerError);
            context.Response.ContentType = "application/problem+json";
            try
            {
                await context.Response.WriteAsJsonAsync(problem, options: (System.Text.Json.JsonSerializerOptions?)null,
                    contentType: "application/problem+json", cancellationToken: cancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                //The request was cancelled, do not attempt to write response.
                return false;
            }
            return true;
        }

        private static async Task AddUserErrorAsync(ILogger<GXExceptionHandler> logger,
            IServiceProvider serviceProvider,
            ClaimsPrincipal user, Exception ex)
        {
            if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
            {
                logger.LogError("{Message}", ex.ToString());
            }
            try
            {
                if (user.Identity == null || !user.Identity.IsAuthenticated)
                {
                    //User is not authenticated, do not log error to user errors.
                    return;
                }
                var authorization = serviceProvider.GetRequiredService<Microsoft.AspNetCore.Authorization.IAuthorizationService>();
                if (!await authorization.CanAccessPolicyAsync(user, Gurux.DLMS.AMI.Client.Shared.GXUserErrorPolicies.Add))
                {
                    // The original error is already logged. Do not cause a second authorization error.
                    return;
                }
                //Add error to user errors.
                GXLog error = new(TraceLevel.Error)
                {
                    User = new GXUser()
                    {
                        Id = ServerHelpers.GetUserId(user)
                    },
                    CreationTime = DateTime.Now,
                    Message = ex.Message,
                };
                ILogRepository? _userRepository = serviceProvider.GetService<ILogRepository>()?.ForSource("user-error");
                if (_userRepository != null)
                {
                    await _userRepository.AddAsync(TargetType.System, [error]);
                }
            }
            catch (Exception ex2)
            {
                if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
                {
                    logger.LogError("{Message}", ex2.ToString());
                }
            }
        }

        private async Task AddSystemErrorAsync(ILogger<GXExceptionHandler> logger,
            HttpContext context, Exception ex)
        {
            try
            {
                GXLog error = new(TraceLevel.Error)
                {
                    Message = ex.Message,
                    StackTrace = ex.ToString(),
                    Data = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Path = context.Request.Path.Value,
                        TraceId = context.TraceIdentifier
                    })
                };
                // Server diagnostics must not depend on an administrator account,
                // the request user's permissions, or a failed repository scope.
                await using var connection = await _dbFactory.GetConnectionAsync();
                await GXLogStore.InsertAsync(connection, "system", [error]);
            }
            catch (Exception logError)
            {
                logger.LogError(logError, "Could not persist the system error for {Path}, trace {TraceId}",
                    context.Request.Path, context.TraceIdentifier);
            }
        }

        private async Task<ProblemDetails> CreateProblemDetailsAsync(
            ILogger<GXExceptionHandler> logger,
            Exception exception,
            HttpContext context, IServiceProvider serviceProvider)
        {
            var problem = new ProblemDetails
            {
                Instance = context.Request.Path
            };

            switch (exception)
            {
                case BadHttpRequestException ex:
                    problem.Title = "Invalid HTTP request";
                    problem.Detail = "The HTTP request is invalid or incomplete.";
                    problem.Status = ex.StatusCode;
                    break;
                case Gurux.DLMS.AMI.Module.AmiModuleUnavailableException ex:
                    problem.Title = "Module unavailable";
                    problem.Detail = ex.Message;
                    problem.Status = StatusCodes.Status503ServiceUnavailable;
                    break;
                case Gurux.DLMS.AMI.Module.AmiModuleDependencyException ex:
                    problem.Title = "Module dependency conflict";
                    problem.Detail = ex.Message;
                    problem.Status = StatusCodes.Status409Conflict;
                    break;
                case ArgumentException ex:
                    problem.Title = "Invalid argument";
                    problem.Detail = ex.Message;
                    problem.Status = StatusCodes.Status400BadRequest;
                    break;
                case ValidationException ex:
                    problem.Title = "Validation failed";
                    problem.Detail = ex.Message;
                    problem.Status = StatusCodes.Status400BadRequest;
                    break;
                case UnauthorizedAccessException ex:
                    problem.Title = "Unauthorized";
                    problem.Detail = ex.Message;
                    problem.Status = StatusCodes.Status401Unauthorized;
                    break;
                case GXAmiForbiddenException ex:
                    problem.Title = "Forbidden";
                    problem.Detail = "Insufficient permissions.";
                    problem.Status = StatusCodes.Status403Forbidden;
                    if (ex.RequiredPolicies.Count != 0)
                    {
                        problem.Extensions["requiredPolicies"] = ex.RequiredPolicies;
                    }
                    break;
                case GXAmiNotFoundException:
                    problem.Title = "Not found";
                    problem.Detail = "The requested item was not found or is not accessible.";
                    problem.Status = StatusCodes.Status404NotFound;
                    break;
                case HttpRequestException:
                    problem.Title = "Upstream request failed";
                    problem.Detail = "An upstream HTTP request failed.";
                    problem.Status = StatusCodes.Status502BadGateway;
                    break;
                default:
                    problem.Title = "An unexpected error occurred";
                    problem.Detail = "Internal server error.";
                    problem.Status = StatusCodes.Status500InternalServerError;
                    break;
            }
            problem.Extensions["traceId"] = context.TraceIdentifier;
            if (exception is ArgumentException ||
                exception is ValidationException)
            {
                await AddUserErrorAsync(logger, serviceProvider, context.User, exception);
            }
            else if (exception is not BadHttpRequestException and not GXAmiForbiddenException and not UnauthorizedAccessException and not GXAmiNotFoundException and not Gurux.DLMS.AMI.Module.AmiModuleUnavailableException)
            {
                await AddSystemErrorAsync(logger, context, exception);
            }
            return problem;
        }
    }

}
