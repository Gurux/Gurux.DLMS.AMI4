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

using System.Net;
using System.Text.Json;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Server.Repository;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Server.Internal;
using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs.User;

namespace Gurux.DLMS.AMI.Server.Midlewares
{
    internal sealed class GXErrorHandlerMidleware
    {
        private readonly IUserErrorRepository _userErrorRepository;
        private readonly RequestDelegate _next;

        public GXErrorHandlerMidleware(RequestDelegate next,
            IUserErrorRepository userErrorRepository)
        {
            _next = next;
            _userErrorRepository = userErrorRepository;
        }

        private async Task AddUserErrorAsync(ClaimsPrincipal user, Exception ex)
        {
            try
            {
                //Add error to user errors.
                GXUserError error = new GXUserError()
                {
                    User = new GXUser()
                    {
                        Id = ServerHelpers.GetUserId(user)
                    },
                    CreationTime = DateTime.Now,
                    Message = ex.Message,
                };
                await _userErrorRepository.AddAsync(user, 
                    new GXUserError[] { error});
            }
            catch (Exception)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public async Task Invoke(HttpContext context,
            ISystemLogRepository systemErrorRepository)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException)
            {
                //Client has canceled the operation.
                //This is ignored.
            }
            catch (GXAMIForbiddenException ex)
            {
                await AddUserErrorAsync(context.User, ex);
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                if (!context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                }
                if (ex.Message == null)
                {
                    await context.Response.WriteAsync("Access forbidden.");
                }
                else
                {
                    await context.Response.WriteAsync(ex.Message);
                }
            }
            catch (GXAmiNotFoundException ex)
            {
                await AddUserErrorAsync(context.User, ex);
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                if (!context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                }
                if (ex.Message == null)
                {
                    await context.Response.WriteAsync("Unknown target.");
                }
                else
                {
                    await context.Response.WriteAsync(ex.Message);
                }
            }

            catch (UnauthorizedAccessException ex)
            {
                await AddUserErrorAsync(context.User, ex);
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                if (!context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                }
                if (ex.Message == null)
                {
                    await context.Response.WriteAsync("Unauthorized access.");
                }
                else
                {
                    await context.Response.WriteAsync(ex.Message);
                }
            }
            catch (ArgumentException ex)
            {
                await AddUserErrorAsync(context.User, ex);
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                if (!context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                }
                if (ex.Message == null)
                {
                    await context.Response.WriteAsync("Invalid argument exception.");
                }
                else
                {
                    await context.Response.WriteAsync(ex.Message);
                }
            }
            catch (GXAmiException ex)
            {
                await AddUserErrorAsync(context.User, ex);
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                if (!context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                }
                if (ex.Message == null)
                {
                    await context.Response.WriteAsync("Invalid argument exception.");
                }
                else
                {
                    await context.Response.WriteAsync(ex.Message);
                }
            }
            catch (Exception ex)
            {
                await AddUserErrorAsync(context.User, ex);
                GXSystemLog err = await systemErrorRepository.AddAsync(context.User, ex);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                if (!context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                }
                GXAmiException error = new GXAmiException("Internal server error has occurred. Details are stored for the database.")
                {
                    Id = err.Id,
                    CreationTime = err.CreationTime.Value
                };
                var result = JsonSerializer.Serialize(error);
                await context.Response.WriteAsync(result);
            }
        }
    }
}
