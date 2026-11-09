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
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Server.Triggers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Midlewares
{
    internal class GXApplicationSignInManager(
        IWorkflowHandler workflowHandler,
        IDbConnectionFactory database,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<ApplicationUser>> logger,
        IAuthenticationSchemeProvider schemeProvider,
        IUserConfirmation<ApplicationUser> confirmation
            ) : SignInManager<ApplicationUser>(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemeProvider, confirmation)
    {
        private readonly IWorkflowHandler _workflowHandler = workflowHandler;

        public override async Task<bool> CanSignInAsync(ApplicationUser user)
        {
            return user.IsApproved && await base.CanSignInAsync(user);
        }

        public override async Task SignInWithClaimsAsync(ApplicationUser user,
            AuthenticationProperties? authenticationProperties, IEnumerable<Claim> additionalClaims)
        {
            // Direct SignInAsync calls also reach this overload, bypassing the usual pre-sign-in checks.
            if (!await CanSignInAsync(user))
            {
                throw new InvalidOperationException("Sign-in is not allowed.");
            }
            await using var connection = await database.GetConnectionAsync();
            var now = DateTimeOffset.UtcNow;
            var login = GXUpdateArgs.Update(new GXUser { Id = user.Id, LastLogin = now }, u => u.LastLogin!);
            login.Where.And<GXUser>(u => u.Id == user.Id && u.Removed == null && u.IsApproved == true);
            if (await connection.UpdateAsync(login) != 1)
            {
                throw new InvalidOperationException(Properties.Resources.AccessDenied);
            }
            var activity = await connection.SingleOrDefaultAsync<GXUser>(GXSelectArgs.Select<GXUser>(
                u => new { u.LastLogin, u.ConcurrencyStamp }, u => u.Id == user.Id && u.Removed == null && u.IsApproved == true));
            if (activity == null)
            {
                throw new InvalidOperationException(Properties.Resources.AccessDenied);
            }
            user.LastLogin = activity.LastLogin;
            user.ConcurrencyStamp = activity.ConcurrencyStamp;
            await base.SignInWithClaimsAsync(user, authenticationProperties, additionalClaims);
        }

        /// <summary>
        /// Increase Access failed count if user gives wrong password.
        /// </summary>
        public override async Task<SignInResult> CheckPasswordSignInAsync(ApplicationUser user, string password, bool lockoutOnFailure)
        {
            lockoutOnFailure = true;
            var ret = await base.CheckPasswordSignInAsync(user, password, lockoutOnFailure);
            if (ret.Succeeded)
            {
                _workflowHandler.Execute(typeof(UserTrigger), UserTrigger.Login, user, await CreateUserPrincipalAsync(user));

            }
            else
            {
                if (ret.IsLockedOut)
                {
                    _workflowHandler.Execute(typeof(UserTrigger), UserTrigger.LockedOut, user);
                }
            }
            return ret;
        }
    }
}
