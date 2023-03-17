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

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.Enums;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.Enums;
using System.Diagnostics;
using System.Globalization;
using Gurux.DLMS.AMI.Server.Models;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Server.Triggers;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Cron;

namespace Gurux.DLMS.AMI.Server.Midlewares
{
    internal class GXApplicationUserManager : UserManager<ApplicationUser>
    {
        private readonly IGXHost _host;
        private readonly IServiceProvider _serviceProvider;
        private readonly IWorkflowHandler _workflowHandler;

        public GXApplicationUserManager(
            IGXHost host,
            IWorkflowHandler workflowHandler,
          IServiceProvider serviceProvider,
            IUserStore<ApplicationUser> userStore, IOptions<IdentityOptions> optionsAccessor,
            IPasswordHasher<ApplicationUser> passwordHasher,
            IEnumerable<IUserValidator<ApplicationUser>> userValidators,
            IEnumerable<IPasswordValidator<ApplicationUser>> passwordValidators, ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors, IServiceProvider services, ILogger<GXApplicationUserManager> logger) :
            base(userStore, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors,
                services, logger)
        {
            _host = host;
            _serviceProvider = serviceProvider;
            _workflowHandler = workflowHandler;
        }

        public override async Task<IdentityResult> CreateAsync(ApplicationUser user)
        {
            bool adminAdds = user.CreationTime != null;
            if (!adminAdds)
            {
                user.CreationTime = DateTime.Now;
            }
            var ret = await base.CreateAsync(user);
            bool firstUser = !ServerSettings.AdminAdded;
            if (ret.Succeeded)
            {
                _workflowHandler.Execute(typeof(UserTrigger), UserTrigger.Create, user);
                string?[] roles;
                //If admin has added the user.
                if (adminAdds)
                {
                    return ret;
                }
                DateTime now = DateTime.Now;
                //Set creation time.
                GXUser u = new GXUser() { CreationTime = now, Id = user.Id };
                u.Roles = new List<string>();
                _host.Connection.Update(GXUpdateArgs.Update(u, q => q.CreationTime));
                if (!ServerSettings.AdminAdded)
                {
                    roles = new string[] { GXRoles.Admin };
                    u.Roles.Add(GXRoles.Admin);
                    //1st user is admin user.
                    ret = await AddToRolesAsync(user, roles);
                    if (!ret.Succeeded)
                    {
                        var error = new GXSystemLog()
                        {
                            CreationTime = DateTime.Now,
                            Message = "Failed to add user to admin group" + string.Join(';', ret.Errors),
                        };
                        _host.Connection.Insert(GXInsertArgs.Insert(error));
                    }
                    else
                    {
                        var error = new GXSystemLog(TraceLevel.Info)
                        {
                            CreationTime = DateTime.Now,
                            Message = "Administrator user created." + string.Join(';', ret.Errors),
                        };
                        _host.Connection.Insert(GXInsertArgs.Insert(error));
                        ServerSettings.AdminAdded = true;
                    }
                }
                else
                {
                    GXSelectArgs args = GXSelectArgs.SelectAll<GXRole>(where => where.Default == true);
                    roles = _host.Connection.Select<GXRole>(args).Select(s => s.Name).ToArray();
                    if (roles == null || !roles.Any())
                    {
                        var error = new GXSystemLog(TraceLevel.Warning)
                        {
                            CreationTime = DateTime.Now,
                            Message = "Default roles aren't selected." + string.Join(';', ret.Errors),
                        };
                        _host.Connection.Insert(GXInsertArgs.Insert(error));
                    }
                    else
                    {
                        ret = await AddToRolesAsync(user, roles);
                        if (!ret.Succeeded)
                        {
                            var error = new GXSystemLog()
                            {
                                CreationTime = DateTime.Now,
                                Message = "Failed to add default roles" + string.Join(';', ret.Errors),
                            };
                            _host.Connection.Insert(GXInsertArgs.Insert(error));
                        }
                    }
                }

                ClaimsIdentity claimsIdentity = new ClaimsIdentity();
                ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                var identity = new ClaimsIdentity();
                ClaimsPrincipal User = new ClaimsPrincipal(identity);
                var ci = (ClaimsIdentity)User.Identity;
                ci.AddClaim(new Claim("sub", user.Id));
                ci.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
                ci.AddClaim(new Claim(identity.RoleClaimType, string.Join(",", roles)));
                ci.AddClaims(await GetClaimsAsync(user));
                //Create default User group and add user to it.
                GXUserGroup ug = new GXUserGroup(Properties.Resources.Default) { CreationTime = now, Default = true };
                ug.Users.Add(u);
                var t = new GXUserGroup[] { ug };
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    IUserGroupRepository userGroupRepository = scope.ServiceProvider.GetRequiredService<IUserGroupRepository>();
                    await userGroupRepository.UpdateAsync(User, t, null);

                    //Create default schedule group and add user group to it.
                    GXScheduleGroup sg = new GXScheduleGroup(Properties.Resources.Default) { CreationTime = now, Default = true };
                    sg.UserGroups.Add(ug);
                    List<GXSchedule> list = new List<GXSchedule>();
                    GXSchedule m = new GXSchedule(Properties.Resources.Minutely);
                    GXDateTime dt = new GXDateTime(DateTime.Now.Date);
                    dt.Skip = DateTimeSkips.Year | DateTimeSkips.Month | DateTimeSkips.Day | DateTimeSkips.Hour | DateTimeSkips.Minute;
                    m.Start = dt.ToFormatString(CultureInfo.InvariantCulture);
                    list.Add(m);
                    GXSchedule h = new GXSchedule(Properties.Resources.Hourly);
                    dt.Skip = DateTimeSkips.Year | DateTimeSkips.Month | DateTimeSkips.Day | DateTimeSkips.Hour;
                    h.Start = dt.ToFormatString(CultureInfo.InvariantCulture);
                    list.Add(h);
                    GXSchedule d = new GXSchedule(Properties.Resources.Daily);
                    dt.Skip = DateTimeSkips.Year | DateTimeSkips.Month | DateTimeSkips.Day;
                    d.Start = dt.ToFormatString(CultureInfo.InvariantCulture);
                    list.Add(d);
                    sg.Schedules = list;
                    IScheduleGroupRepository scheduleGroupRepository = scope.ServiceProvider.GetRequiredService<IScheduleGroupRepository>();
                    await scheduleGroupRepository.UpdateAsync(User, new GXScheduleGroup[] { sg }, null);

                    //Create default device template group and add user group to it.
                    GXDeviceTemplateGroup dtg = new GXDeviceTemplateGroup(Properties.Resources.Default) { CreationTime = now, Default = true };
                    dtg.UserGroups.Add(ug);
                    IDeviceTemplateGroupRepository deviceTemplateGroupRepository = scope.ServiceProvider.GetRequiredService<IDeviceTemplateGroupRepository>();
                    await deviceTemplateGroupRepository.UpdateAsync(User, new GXDeviceTemplateGroup[] { dtg }, null);

                    //Create default device group and add user group to it.
                    GXDeviceGroup dg = new GXDeviceGroup(Properties.Resources.Default) { CreationTime = now, Default = true };
                    dg.UserGroups.Add(ug);
                    IDeviceGroupRepository deviceGroupRepository = scope.ServiceProvider.GetRequiredService<IDeviceGroupRepository>();
                    await deviceGroupRepository.UpdateAsync(User, new GXDeviceGroup[] { dg }, null);

                    //Create default agent group and add user group to it.
                    GXAgentGroup ag = new GXAgentGroup(Properties.Resources.Default) { CreationTime = now, Default = true };
                    ag.UserGroups.Add(ug);
                    IAgentGroupRepository agentGroupRepository = scope.ServiceProvider.GetRequiredService<IAgentGroupRepository>();
                    await agentGroupRepository.UpdateAsync(User, new GXAgentGroup[] { ag }, null);

                    //Create default module group and add user group to it.
                    GXModuleGroup mg = new GXModuleGroup(Properties.Resources.Default) { CreationTime = now, Default = true };
                    mg.UserGroups.Add(ug);
                    IModuleGroupRepository moduleGroupRepository = scope.ServiceProvider.GetRequiredService<IModuleGroupRepository>();
                    await moduleGroupRepository.UpdateAsync(User, new GXModuleGroup[] { mg }, null);

                    //Create default trigger group and add user group to it.
                    GXTriggerGroup tg = new GXTriggerGroup(Properties.Resources.Default) { CreationTime = now, Default = true };
                    tg.UserGroups.Add(ug);
                    ITriggerGroupRepository triggerGroupRepository = scope.ServiceProvider.GetRequiredService<ITriggerGroupRepository>();
                    await triggerGroupRepository.UpdateAsync(User, new GXTriggerGroup[] { tg }, null);

                    //Create default workflow group and add user group to it.
                    GXWorkflowGroup wfg = new GXWorkflowGroup(Properties.Resources.Default) { CreationTime = now, Default = true };
                    wfg.UserGroups.Add(ug);
                    IWorkflowGroupRepository workflowGroupRepository = scope.ServiceProvider.GetRequiredService<IWorkflowGroupRepository>();
                    await workflowGroupRepository.UpdateAsync(User, new GXWorkflowGroup[] { wfg });

                    //Create default block group and add user group to it.
                    GXBlockGroup bg = new GXBlockGroup(Properties.Resources.Default) { CreationTime = now, Default = true };
                    bg.UserGroups.Add(ug);
                    IBlockGroupRepository blockGroupRepository = scope.ServiceProvider.GetRequiredService<IBlockGroupRepository>();
                    await blockGroupRepository.UpdateAsync(User, new GXBlockGroup[] { bg });

                    //Create default script group and add user group to it.
                    GXScriptGroup sgs = new GXScriptGroup(Properties.Resources.Default) { CreationTime = now, Default = true };
                    sgs.UserGroups.Add(ug);
                    IScriptGroupRepository scriptGroupRepository = scope.ServiceProvider.GetRequiredService<IScriptGroupRepository>();
                    await scriptGroupRepository.UpdateAsync(User, new GXScriptGroup[] { sgs });
                }
                //Run cron after the admin is added.
                if (firstUser)
                {
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        IGXCronService cronService = scope.ServiceProvider.GetRequiredService<IGXCronService>();
                        await cronService.RunAsync(ServerHelpers.CreateClaimsPrincipalFromUser(u));
                    }
                }
            }
            return ret;
        }

        public override Task<IdentityResult> UpdateAsync(ApplicationUser user)
        {
            user.Updated = DateTime.Now;
            return base.UpdateAsync(user);
        }
    }
}
