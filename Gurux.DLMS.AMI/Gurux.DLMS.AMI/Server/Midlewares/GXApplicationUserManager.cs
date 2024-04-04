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
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Report;
using Gurux.DLMS.AMI.Shared.DTOs.Subtotal;

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
                string groupName = Properties.Resources.Default;
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
                GXUserGroup ug = new GXUserGroup(groupName) { CreationTime = now, Default = true };
                ug.Users.Add(u);
                var t = new GXUserGroup[] { ug };
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    IUserGroupRepository userGroupRepository = scope.ServiceProvider.GetRequiredService<IUserGroupRepository>();
                    await userGroupRepository.UpdateAsync(User, t, null);

                    //Create default schedule group and add user group to it.
                    GXScheduleGroup sg = new GXScheduleGroup(groupName) { CreationTime = now, Default = true };
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
                    await scheduleGroupRepository.UpdateAsync(User, [sg], null);

                    //Create default device template group and add user group to it.
                    GXDeviceTemplateGroup dtg = new GXDeviceTemplateGroup(groupName) { CreationTime = now, Default = true };
                    dtg.UserGroups.Add(ug);
                    IDeviceTemplateGroupRepository deviceTemplateGroupRepository = scope.ServiceProvider.GetRequiredService<IDeviceTemplateGroupRepository>();
                    await deviceTemplateGroupRepository.UpdateAsync(User, [dtg], null);

                    //Create default device group and add user group to it.
                    GXDeviceGroup dg = new GXDeviceGroup(groupName) { CreationTime = now, Default = true };
                    dg.UserGroups.Add(ug);
                    IDeviceGroupRepository deviceGroupRepository = scope.ServiceProvider.GetRequiredService<IDeviceGroupRepository>();
                    await deviceGroupRepository.UpdateAsync(User, [dg], null);

                    //Create default agent group and add user group to it.
                    GXAgentGroup ag = new GXAgentGroup(groupName) { CreationTime = now, Default = true };
                    ag.UserGroups.Add(ug);
                    IAgentGroupRepository agentGroupRepository = scope.ServiceProvider.GetRequiredService<IAgentGroupRepository>();
                    _ = await agentGroupRepository.UpdateAsync(User, [ag], null);

                    //Create default module group and add user group to it.
                    GXModuleGroup mg = new GXModuleGroup(groupName) { CreationTime = now, Default = true };
                    mg.UserGroups.Add(ug);
                    IModuleGroupRepository moduleGroupRepository = scope.ServiceProvider.GetRequiredService<IModuleGroupRepository>();
                    _ = await moduleGroupRepository.UpdateAsync(User, [mg], null);

                    //Create default trigger group and add user group to it.
                    GXTriggerGroup tg = new GXTriggerGroup(groupName) { CreationTime = now, Default = true };
                    tg.UserGroups.Add(ug);
                    ITriggerGroupRepository triggerGroupRepository = scope.ServiceProvider.GetRequiredService<ITriggerGroupRepository>();
                    _ = await triggerGroupRepository.UpdateAsync(User, [tg], null);

                    //Create default workflow group and add user group to it.
                    GXWorkflowGroup wfg = new GXWorkflowGroup(groupName) { CreationTime = now, Default = true };
                    wfg.UserGroups.Add(ug);
                    IWorkflowGroupRepository workflowGroupRepository = scope.ServiceProvider.GetRequiredService<IWorkflowGroupRepository>();
                    _ = await workflowGroupRepository.UpdateAsync(User, [wfg]);

                    //Create default block group and add user group to it.
                    GXBlockGroup bg = new GXBlockGroup(groupName) { CreationTime = now, Default = true };
                    bg.UserGroups.Add(ug);
                    IBlockGroupRepository blockGroupRepository = scope.ServiceProvider.GetRequiredService<IBlockGroupRepository>();
                    _ = await blockGroupRepository.UpdateAsync(User, [bg]);

                    //Create default script group and add user group to it.
                    GXScriptGroup sgs = new GXScriptGroup(groupName) { CreationTime = now, Default = true };
                    sgs.UserGroups.Add(ug);
                    IScriptGroupRepository scriptGroupRepository = scope.ServiceProvider.GetRequiredService<IScriptGroupRepository>();
                    _ = await scriptGroupRepository.UpdateAsync(User, [sgs]);

                    //Create default gateway group and add user group to it.
                    GXGatewayGroup gg = new GXGatewayGroup(groupName) { CreationTime = now };
                    gg.UserGroups.Add(ug);
                    IGatewayGroupRepository gatewayGroupRepository = scope.ServiceProvider.GetRequiredService<IGatewayGroupRepository>();
                    _ = await gatewayGroupRepository.UpdateAsync(User, [gg], null);

                    //Create default key management group and add user group to it.
                    GXKeyManagementGroup kmg = new GXKeyManagementGroup(groupName) { CreationTime = now, Default = true };
                    kmg.UserGroups.Add(ug);
                    IKeyManagementGroupRepository kmGroupRepository = scope.ServiceProvider.GetRequiredService<IKeyManagementGroupRepository>();
                    _ = await kmGroupRepository.UpdateAsync(User, [kmg]);

                    //Create default subtotal group and add user group to it.
                    GXSubtotalGroup stg = new GXSubtotalGroup(groupName) { CreationTime = now, Default = true };
                    stg.UserGroups.Add(ug);
                    ISubtotalGroupRepository stGroupRepository = scope.ServiceProvider.GetRequiredService<ISubtotalGroupRepository>();
                    await stGroupRepository.UpdateAsync(User, [stg]);
                    //Create default report group and add user group to it.
                    GXReportGroup rg = new GXReportGroup(groupName) { CreationTime = now, Default = true };
                    rg.UserGroups.Add(ug);
                    IReportGroupRepository rGroupRepository = scope.ServiceProvider.GetRequiredService<IReportGroupRepository>();
                    _ = await rGroupRepository.UpdateAsync(User, new GXReportGroup[] { rg });
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
