//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Midlewares
{
    public sealed class GXExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GXExceptionHandler> _logger;

        public GXExceptionHandler(ILogger<GXExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext context,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "User exception");
            if (context.Response.HasStarted)
            {
                return false;
            }
            var serviceProvider = context.RequestServices;
            var problem = await CreateProblemDetails(_logger, exception, context, serviceProvider);
            context.Response.Clear();
            context.Response.StatusCode = problem.Status.GetValueOrDefault(StatusCodes.Status500InternalServerError);
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }

        private static async Task AddUserErrorAsync(ILogger<GXExceptionHandler> logger,
            IServiceProvider serviceProvider,
           ClaimsPrincipal user, Exception ex)
        {
            try
            {
                //Add error to user errors.
                GXUserError error = new GXUserError(TraceLevel.Error)
                {
                    User = new GXUser()
                    {
                        Id = ServerHelpers.GetUserId(user)
                    },
                    CreationTime = DateTime.Now,
                    Message = ex.Message,
                };
                IUserErrorRepository? _userRepository = serviceProvider.GetService<IUserErrorRepository>();
                if (_userRepository != null)
                {
                    await _userRepository.AddAsync(TargetType.System, [error]);
                }
            }
            catch (Exception ex2)
            {
                logger.LogError(ex2.ToString());
                logger.LogError(ex.ToString());
            }
        }

        private static async Task<ProblemDetails> CreateProblemDetails(ILogger<GXExceptionHandler> logger,
            Exception? exception,
            HttpContext context, IServiceProvider serviceProvider)
        {
            var problem = new ProblemDetails
            {
                Instance = context.Request.Path
            };

            switch (exception)
            {
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

                default:
                    problem.Title = "An unexpected error occurred";
                    problem.Detail = "Internal server error.";
                    problem.Status = StatusCodes.Status500InternalServerError;
                    break;
            }
            problem.Extensions["traceId"] = context.TraceIdentifier;
            if (exception != null)
            {
                await AddUserErrorAsync(logger, serviceProvider, context.User, exception);
            }
            return problem;
        }
    }

}
