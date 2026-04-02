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

using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Repository;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Server.Triggers;
using Gurux.DLMS.AMI.Shared.DIs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Gurux.DLMS.AMI.Server.Midlewares
{
    internal class GXApplicationSignInManager : SignInManager<ApplicationUser>
    {
        private readonly IWorkflowHandler _workflowHandler;
        private readonly BlockService _blockService;
        public GXApplicationSignInManager(
            IWorkflowHandler workflowHandler,
            BlockService blockService,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<ApplicationUser>> logger,
            IAuthenticationSchemeProvider schemeProvider,
            IUserConfirmation<ApplicationUser> confirmation
            )
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemeProvider, confirmation)
        {
            _workflowHandler = workflowHandler;
            _blockService = blockService;
        }

        /// <summary>
        /// This task is used to check that user manager or admin are approved the new user.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public Task<bool> IsConfirmedAsync(UserManager<ApplicationUser> manager,
                                      ApplicationUser user)
        {
            //Admin must approve the new user.
            return Task.FromResult(user.EmailConfirmed && user.IsApproved);
        }

        /// <summary>
        /// Increase Access failed count if user fives wrong password.
        /// </summary>
        public override async Task<SignInResult> CheckPasswordSignInAsync(ApplicationUser user, string password, bool lockoutOnFailure)
        {
            lockoutOnFailure = true;
            var ret = await base.CheckPasswordSignInAsync(user, password, lockoutOnFailure);
            if (ret.Succeeded)
            {
                _workflowHandler.Execute(typeof(UserTrigger), UserTrigger.Login, user);
                if (await _blockService.IsBlockedAsync(Context.Connection.RemoteIpAddress, user.Id))
                {
                    return SignInResult.NotAllowed;
                }
            }
            return ret;
        }
    }
}
