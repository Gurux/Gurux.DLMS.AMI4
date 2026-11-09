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

using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Repository;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Server.Triggers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Midlewares
{
    /// <summary>
    /// GXBlackListMidleware handles black listed IP addresses.
    /// </summary>
    /// <remarks>
    /// Connection are disallowed, if IP address is added for the black list.
    /// </remarks>
    internal sealed class GXCookieAuthenticationEvents(
        IServiceProvider serviceProvider,
        ILogger<GXCookieAuthenticationEvents> logger,
        IMemoryCache cache,
    //            RequestDelegate next,
    IWorkflowHandler workflowHandler) : GXCompactCookieEvents
    {
        private readonly ILogger<GXCookieAuthenticationEvents> _logger = logger;

        private readonly IMemoryCache _cache = cache;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IWorkflowHandler _workflowHandler = workflowHandler;

        public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
        {
            if (context.Request.Path.StartsWithSegments("/api/Favorite"))
            {
                bool cookiePresent = context.Options.Cookie.Name is string cookieName &&
                    context.Request.Cookies.ContainsKey(cookieName);
                _logger.LogWarning("Favorites request requires authentication. Authentication cookie present: {CookiePresent}; Request ID: {TraceId}",
                    cookiePresent, context.HttpContext.TraceIdentifier);
            }
            return base.RedirectToLogin(context);
        }

        public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            var principal = context.Principal;
            if (principal?.Identity?.IsAuthenticated == true)
            {
                var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
                // Validate the original cookie before issuing a principal with the current security stamp.
                var user = await signInManager.ValidateSecurityStampAsync(principal);
                if (user == null)
                {
                    _logger.LogWarning("Authentication cookie rejected: security stamp validation failed. Path: {Path}; Request ID: {TraceId}",
                        context.Request.Path, context.HttpContext.TraceIdentifier);
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                    return;
                }
                var database = context.HttpContext.RequestServices.GetRequiredService<IDbConnectionFactory>();
                await using var connection = await database.GetConnectionAsync(context.HttpContext.RequestAborted);
                var active = await connection.SingleOrDefaultAsync<GXUser>(GXSelectArgs.Select<GXUser>(u => u.Id!,
                    u => u.Id == user.Id && u.Removed == null && u.IsApproved == true), context.HttpContext.RequestAborted);
                if (active == null)
                {
                    _logger.LogWarning("Authentication cookie rejected: user is not active and approved. Path: {Path}; Request ID: {TraceId}",
                        context.Request.Path, context.HttpContext.TraceIdentifier);
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                    return;
                }
                context.ReplacePrincipal(await signInManager.CreateUserPrincipalAsync(user));
                // The expanded principal is for this request only. Normal sliding renewal
                // uses the original compact ticket, so permissions stay out of the cookie.
                context.ShouldRenew = false;
                principal = context.Principal;
            }
            if (principal?.Identity?.IsAuthenticated == true)
            {
                var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return;
                }
                var sessionKey = $"user_reconnect_{userId}";
                if (!_cache.TryGetValue(sessionKey, out _))
                {
                    using var scope = _serviceProvider.CreateScope();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var user = await userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        _workflowHandler.Execute(typeof(UserTrigger), UserTrigger.Login, user);
                        _cache.Set(sessionKey, true, TimeSpan.FromMinutes(30));
                    }
                }
            }
        }

        public override async Task SigningOut(CookieSigningOutContext context)
        {
            _logger.LogInformation("SigningOut called");
            var userId = context.HttpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var userManager = context.HttpContext.RequestServices
                    .GetRequiredService<UserManager<ApplicationUser>>();
                var user = await userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
                    {
                        _logger.LogInformation("User {UserName} signed out.", user.UserName);
                    }
                    _workflowHandler.Execute(typeof(UserTrigger), UserTrigger.Logout, user);
                }
            }
            await base.SigningOut(context);
        }
    }
}
