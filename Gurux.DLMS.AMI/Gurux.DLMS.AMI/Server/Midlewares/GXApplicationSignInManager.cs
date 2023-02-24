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

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Server.Models;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Server.Triggers;
using Gurux.DLMS.AMI.Server.Internal;

namespace Gurux.DLMS.AMI.Server.Midlewares
{
    internal class GXApplicationSignInManager : SignInManager<ApplicationUser>
    {
        private readonly IGXHost _host;
        private readonly IWorkflowHandler _workflowHandler;

        public GXApplicationSignInManager(
            IGXHost host,
            IWorkflowHandler workflowHandler,
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
            _host = host;
            _workflowHandler = workflowHandler;
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
                GXUser User = new GXUser() { LastLogin = DateTime.Now, Id = user.Id };
                byte[] bytes = Context.Connection.RemoteIpAddress.GetAddressBytes();
                UInt64 ipAddress;
                if (bytes.Length == 4)
                {
                    ipAddress = BitConverter.ToUInt32(bytes);
                }
                else
                {
                    ipAddress = BitConverter.ToUInt64(bytes);
                }
                GXSelectArgs args = GXSelectArgs.SelectAll<GXIpAddress>(where => where.User == User &&
                    where.IPAddress == ipAddress);
                GXIpAddress address = await _host.Connection.SingleOrDefaultAsync<GXIpAddress>(args);
                if (address == null)
                {
                    //User has login from unknow IP address.
                    address = new GXIpAddress()
                    {
                        IPAddress = ipAddress,
                        User = User,
                        Detected = DateTime.Now
                    };
                    await _host.Connection.InsertAsync(GXInsertArgs.Insert(address));
                    _workflowHandler.Execute(typeof(UserTrigger), UserTrigger.NewLogin, user);
                }
                else
                {
                    if (address.Blocked)
                    {
                        return SignInResult.NotAllowed;
                    }
                    //Update detected time.
                    address.Detected = DateTime.Now;
                    await _host.Connection.UpdateAsync(GXUpdateArgs.Update(address, c => c.Detected));
                }
                //Update last login time.
                User.LastLogin = DateTime.Now;
                User.IPAddress = ipAddress;
                _host.Connection.Update(GXUpdateArgs.Update(User, q => new { q.LastLogin, q.IPAddress }));
            }
            return ret;
        }
    }
}
