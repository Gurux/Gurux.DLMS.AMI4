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
using Gurux.DLMS.AMI.Server.Cron;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Server.Triggers;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.Content;
using Gurux.DLMS.AMI.Shared.DTOs.ContentType;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.DLMS.AMI.Shared.DTOs.Report;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Subtotal;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.Enums;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;

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
            bool firstUser = !ServerSettings.AdminAdded;
            if (firstUser)
            {
                user.EmailConfirmed = true;
            }
            var ret = await base.CreateAsync(user);
            if (ret.Succeeded)
            {
                try
                {
                    string groupName = ServerHelpers.GetInvariantString(nameof(Properties.Resources.Default));
                    _workflowHandler.Execute(typeof(UserTrigger), UserTrigger.Create, user);
                    //If admin has added the user.
                    if (adminAdds)
                    {
                        return ret;
                    }
                    DateTime now = DateTime.Now;
                    //Set creation time.
                    GXUser u = new GXUser() { CreationTime = now, Id = user.Id };
                    _host.Connection.Update(GXUpdateArgs.Update(u, q => q.CreationTime));
                    if (!ServerSettings.AdminAdded)
                    {
                        u.Roles = [GXRoles.Admin];
                    }
                    else
                    {
                        //Add default roles.
                        GXSelectArgs args = GXSelectArgs.SelectAll<GXRole>(where => where.Default == true);
                        u.Roles = _host.Connection.Select<GXRole>(args).Select(s => s.Name ?? string.Empty).ToList();
                        //Add default scopes.
                        args = GXSelectArgs.SelectAll<GXScope>(where => where.Default == true);
                        u.Scopes = _host.Connection.Select<GXScope>(args).Select(s => s.Name ?? string.Empty).ToList();
                    }
                    ret = await AddToRolesAsync(user, u.Roles);
                    if (!ret.Succeeded)
                    {
                        var error = new GXSystemLog()
                        {
                            CreationTime = DateTime.Now,
                            Message = "Failed to add default roles" + string.Join(';', ret.Errors)
                        };
                        _host.Connection.Insert(GXInsertArgs.Insert(error));
                    }
                    else
                    {
                        if (!ServerSettings.AdminAdded)
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
                    if (u.Scopes?.Any() == true)
                    {
                        List<Claim> claims = new List<Claim>();
                        foreach (var it in u.Scopes)
                        {
                            claims.Add(new Claim("scope", it));
                        }
                        await base.AddClaimsAsync(user, claims);
                    }
                    //Create default User group and add user to it.
                    GXUserGroup ug = new GXUserGroup(groupName) { CreationTime = now, Default = true };
                    ug.Users?.Add(u);
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        var userManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
                        IHttpContextAccessor? httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                        httpContextAccessor.HttpContext.User = await userManager.CreateUserPrincipalAsync(user);
                        IUserGroupRepository userGroupRepository = scope.ServiceProvider.GetRequiredService<IUserGroupRepository>();
                        await userGroupRepository.UpdateAsync([ug], null);
                        if (firstUser)
                        {
                            ServerSettings.UpdateLanguages(_host);
                            ServerSettings.UpdateAppereances(_host);
                            ServerSettings.AddDefaultMenus(_host);
                        }

                        //Create default schedule group and add user group to it.
                        GXScheduleGroup sg = new GXScheduleGroup(groupName) { CreationTime = now, Default = true };
                        sg.UserGroups?.Add(ug);
                        List<GXSchedule> list = new List<GXSchedule>();
                        GXSchedule m = new GXSchedule(ServerHelpers.GetInvariantString(nameof(Properties.Resources.Minutely)));
                        GXDateTime dt = new GXDateTime(DateTime.Now.Date);
                        dt.Skip = DateTimeSkips.Year | DateTimeSkips.Month | DateTimeSkips.Day | DateTimeSkips.Hour | DateTimeSkips.Minute;
                        m.Start = dt.ToFormatString(CultureInfo.InvariantCulture);
                        list.Add(m);
                        GXSchedule h = new GXSchedule(ServerHelpers.GetInvariantString(nameof(Properties.Resources.Hourly)));
                        dt.Skip = DateTimeSkips.Year | DateTimeSkips.Month | DateTimeSkips.Day | DateTimeSkips.Hour;
                        h.Start = dt.ToFormatString(CultureInfo.InvariantCulture);
                        list.Add(h);
                        GXSchedule d = new GXSchedule(ServerHelpers.GetInvariantString(nameof(Properties.Resources.Daily)));
                        dt.Skip = DateTimeSkips.Year | DateTimeSkips.Month | DateTimeSkips.Day;
                        d.Start = dt.ToFormatString(CultureInfo.InvariantCulture);
                        list.Add(d);
                        sg.Schedules = list;
                        IScheduleGroupRepository scheduleGroupRepository = scope.ServiceProvider.GetRequiredService<IScheduleGroupRepository>();
                        await scheduleGroupRepository.UpdateAsync([sg], null);

                        //Create default device template group and add user group to it.
                        GXDeviceTemplateGroup dtg = new GXDeviceTemplateGroup(groupName) { CreationTime = now, Default = true };
                        dtg.UserGroups?.Add(ug);
                        IDeviceTemplateGroupRepository deviceTemplateGroupRepository = scope.ServiceProvider.GetRequiredService<IDeviceTemplateGroupRepository>();
                        await deviceTemplateGroupRepository.UpdateAsync([dtg], null);

                        //Create default device group and add user group to it.
                        GXDeviceGroup dg = new GXDeviceGroup(groupName) { CreationTime = now, Default = true };
                        dg.UserGroups?.Add(ug);
                        IDeviceGroupRepository deviceGroupRepository = scope.ServiceProvider.GetRequiredService<IDeviceGroupRepository>();
                        await deviceGroupRepository.UpdateAsync([dg], null);

                        //Create default agent group and add user group to it.
                        GXAgentGroup ag = new GXAgentGroup(groupName) { CreationTime = now, Default = true };
                        ag.UserGroups?.Add(ug);
                        IAgentGroupRepository agentGroupRepository = scope.ServiceProvider.GetRequiredService<IAgentGroupRepository>();
                        _ = await agentGroupRepository.UpdateAsync([ag], null);

                        //Create default module group and add user group to it.
                        GXModuleGroup mg = new GXModuleGroup(groupName) { CreationTime = now, Default = true };
                        mg.UserGroups?.Add(ug);
                        IModuleGroupRepository moduleGroupRepository = scope.ServiceProvider.GetRequiredService<IModuleGroupRepository>();
                        _ = await moduleGroupRepository.UpdateAsync([mg], null);

                        //Create default trigger group and add user group to it.
                        GXTriggerGroup tg = new GXTriggerGroup(groupName) { CreationTime = now, Default = true };
                        tg.UserGroups?.Add(ug);
                        ITriggerGroupRepository triggerGroupRepository = scope.ServiceProvider.GetRequiredService<ITriggerGroupRepository>();
                        _ = await triggerGroupRepository.UpdateAsync([tg], null);

                        //Create default workflow group and add user group to it.
                        GXWorkflowGroup wfg = new GXWorkflowGroup(groupName) { CreationTime = now, Default = true };
                        wfg.UserGroups?.Add(ug);
                        IWorkflowGroupRepository workflowGroupRepository = scope.ServiceProvider.GetRequiredService<IWorkflowGroupRepository>();
                        _ = await workflowGroupRepository.UpdateAsync([wfg]);

                        //Create default block group and add user group to it.
                        GXBlockGroup bg = new GXBlockGroup(groupName) { CreationTime = now, Default = true };
                        bg.UserGroups?.Add(ug);
                        IBlockGroupRepository blockGroupRepository = scope.ServiceProvider.GetRequiredService<IBlockGroupRepository>();
                        _ = await blockGroupRepository.UpdateAsync([bg]);

                        //Create default content group and add user group to it.
                        GXContentGroup cg = new GXContentGroup(groupName) { CreationTime = now, Default = true };
                        bg.UserGroups?.Add(ug);
                        IContentGroupRepository contentGroupRepository = scope.ServiceProvider.GetRequiredService<IContentGroupRepository>();
                        _ = await contentGroupRepository.UpdateAsync([cg]);

                        //Create default content group and add user group to it.
                        GXContentTypeGroup ctg = new GXContentTypeGroup(groupName) { CreationTime = now, Default = true };
                        ctg.UserGroups?.Add(ug);
                        IContentTypeGroupRepository contentTypeGroupRepository = scope.ServiceProvider.GetRequiredService<IContentTypeGroupRepository>();
                        _ = await contentTypeGroupRepository.UpdateAsync([ctg]);

                        //Create default menu group and add user group to it.
                        GXMenuGroup mg2 = new GXMenuGroup(groupName) { CreationTime = now, Default = true };
                        bg.UserGroups?.Add(ug);
                        IMenuGroupRepository menuGroupRepository = scope.ServiceProvider.GetRequiredService<IMenuGroupRepository>();
                        _ = await menuGroupRepository.UpdateAsync([mg2]);

                        //Create default script group and add user group to it.
                        GXScriptGroup sgs = new GXScriptGroup(groupName) { CreationTime = now, Default = true };
                        sgs.UserGroups?.Add(ug);
                        IScriptGroupRepository scriptGroupRepository = scope.ServiceProvider.GetRequiredService<IScriptGroupRepository>();
                        _ = await scriptGroupRepository.UpdateAsync([sgs]);

                        //Create default gateway group and add user group to it.
                        GXGatewayGroup gg = new GXGatewayGroup(groupName) { CreationTime = now };
                        gg.UserGroups?.Add(ug);
                        IGatewayGroupRepository gatewayGroupRepository = scope.ServiceProvider.GetRequiredService<IGatewayGroupRepository>();
                        _ = await gatewayGroupRepository.UpdateAsync([gg], null);
                        //Create default key management group and add user group to it.
                        GXKeyManagementGroup kmg = new GXKeyManagementGroup(groupName) { CreationTime = now, Default = true };
                        kmg.UserGroups?.Add(ug);
                        IKeyManagementGroupRepository kmGroupRepository = scope.ServiceProvider.GetRequiredService<IKeyManagementGroupRepository>();
                        _ = await kmGroupRepository.UpdateAsync([kmg]);
                        //Create default subtotal group and add user group to it.
                        GXSubtotalGroup stg = new GXSubtotalGroup(groupName) { CreationTime = now, Default = true };
                        stg.UserGroups?.Add(ug);
                        ISubtotalGroupRepository stGroupRepository = scope.ServiceProvider.GetRequiredService<ISubtotalGroupRepository>();
                        await stGroupRepository.UpdateAsync([stg]);
                        //Create default report group and add user group to it.
                        GXReportGroup rg = new GXReportGroup(groupName) { CreationTime = now, Default = true };
                        rg.UserGroups?.Add(ug);
                        IReportGroupRepository rGroupRepository = scope.ServiceProvider.GetRequiredService<IReportGroupRepository>();
                        _ = await rGroupRepository.UpdateAsync([rg]);
                        //Create default notification group and add user group to it.
                        GXNotificationGroup ng = new GXNotificationGroup(groupName) { CreationTime = now, Default = true };
                        ng.UserGroups?.Add(ug);
                        INotificationGroupRepository nGroupRepository = scope.ServiceProvider.GetRequiredService<INotificationGroupRepository>();
                        _ = await nGroupRepository.UpdateAsync([ng]);

                    }
                    //Run cron after the admin is added.
                    if (firstUser)
                    {
                        using (IServiceScope scope = _serviceProvider.CreateScope())
                        {
                            IGXCronService cronService = scope.ServiceProvider.GetRequiredService<IGXCronService>();
                            await cronService.RunAsync();
                        }
                    }
                }
                catch (Exception)
                {
                    //If something goes wrong, remove the user.
                    _host.Connection.Delete(GXDeleteArgs.DeleteById<GXUser>(user.Id));
                    throw;
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
