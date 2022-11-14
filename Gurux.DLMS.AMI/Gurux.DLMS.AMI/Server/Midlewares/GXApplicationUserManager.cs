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
            var ret = await base.CreateAsync(user);
            bool firstUser = !ServerSettings.AdminAdded;
            if (ret.Succeeded)
            {
                _workflowHandler.Execute(typeof(UserTrigger), UserTrigger.Create, user);
                string?[] roles;
                //If admin has added the user.
                if (user.CreationTime != null)
                {
                    return ret;
                }
                DateTime now = DateTime.Now;
                //Set creation time.
                GXUser u = new GXUser() { CreationTime = now, Id = user.Id };
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
                GXUserGroup ug = new GXUserGroup() { CreationTime = now, Name = Properties.Resources.Default, Default = true };
                ug.Users.Add(u);
                var t = new GXUserGroup[] { ug };
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    IUserGroupRepository userGroupRepository = scope.ServiceProvider.GetRequiredService<IUserGroupRepository>();
                    await userGroupRepository.UpdateAsync(User, t);

                    //Create default schedule group and add user group to it.
                    GXScheduleGroup sg = new GXScheduleGroup() { CreationTime = now, Name = Properties.Resources.Default, Default = true };
                    sg.UserGroups.Add(ug);
                    List<GXSchedule> list = new List<GXSchedule>();
                    GXSchedule m = new GXSchedule();
                    m.Name = Properties.Resources.Minutely;
                    GXDateTime dt = new GXDateTime(DateTime.Now.Date);
                    dt.Skip = DateTimeSkips.Year | DateTimeSkips.Month | DateTimeSkips.Day | DateTimeSkips.Hour | DateTimeSkips.Minute;
                    m.Start = dt.ToFormatString(CultureInfo.InvariantCulture);
                    list.Add(m);
                    GXSchedule h = new GXSchedule();
                    h.Name = Properties.Resources.Hourly;
                    dt.Skip = DateTimeSkips.Year | DateTimeSkips.Month | DateTimeSkips.Day | DateTimeSkips.Hour;
                    h.Start = dt.ToFormatString(CultureInfo.InvariantCulture);
                    list.Add(h);
                    GXSchedule d = new GXSchedule();
                    d.Name = Properties.Resources.Daily;
                    dt.Skip = DateTimeSkips.Year | DateTimeSkips.Month | DateTimeSkips.Day;
                    d.Start = dt.ToFormatString(CultureInfo.InvariantCulture);
                    list.Add(d);
                    sg.Schedules = list;
                    IScheduleGroupRepository scheduleGroupRepository = scope.ServiceProvider.GetRequiredService<IScheduleGroupRepository>();
                    await scheduleGroupRepository.UpdateAsync(User, new GXScheduleGroup[] { sg });

                    //Create default device template group and add user group to it.
                    GXDeviceTemplateGroup dtg = new GXDeviceTemplateGroup() { CreationTime = now, Name = Properties.Resources.Default, Default = true };
                    dtg.UserGroups.Add(ug);
                    IDeviceTemplateGroupRepository deviceTemplateGroupRepository = scope.ServiceProvider.GetRequiredService<IDeviceTemplateGroupRepository>();
                    await deviceTemplateGroupRepository.UpdateAsync(User, new GXDeviceTemplateGroup[] { dtg });

                    //Create default device group and add user group to it.
                    GXDeviceGroup dg = new GXDeviceGroup() { CreationTime = now, Name = Properties.Resources.Default, Default = true };
                    dg.UserGroups.Add(ug);
                    IDeviceGroupRepository deviceGroupRepository = scope.ServiceProvider.GetRequiredService<IDeviceGroupRepository>();
                    await deviceGroupRepository.UpdateAsync(User, new GXDeviceGroup[] { dg });

                    //Create default agent group and add user group to it.
                    GXAgentGroup ag = new GXAgentGroup() { CreationTime = now, Name = Properties.Resources.Default, Default = true };
                    ag.UserGroups.Add(ug);
                    IAgentGroupRepository agentGroupRepository = scope.ServiceProvider.GetRequiredService<IAgentGroupRepository>();
                    await agentGroupRepository.UpdateAsync(User, new GXAgentGroup[] { ag });

                    //Create default module group and add user group to it.
                    GXModuleGroup mg = new GXModuleGroup() { CreationTime = now, Name = Properties.Resources.Default, Default = true };
                    mg.UserGroups.Add(ug);
                    IModuleGroupRepository moduleGroupRepository = scope.ServiceProvider.GetRequiredService<IModuleGroupRepository>();
                    await moduleGroupRepository.UpdateAsync(User, new GXModuleGroup[] { mg });

                    //Create default trigger group and add user group to it.
                    GXTriggerGroup tg = new GXTriggerGroup() { CreationTime = now, Name = Properties.Resources.Default, Default = true };
                    tg.UserGroups.Add(ug);
                    ITriggerGroupRepository triggerGroupRepository = scope.ServiceProvider.GetRequiredService<ITriggerGroupRepository>();
                    await triggerGroupRepository.UpdateAsync(User, new GXTriggerGroup[] { tg });

                    //Create default workflow group and add user group to it.
                    GXWorkflowGroup wfg = new GXWorkflowGroup() { CreationTime = now, Name = Properties.Resources.Default, Default = true };
                    wfg.UserGroups.Add(ug);
                    IWorkflowGroupRepository workflowGroupRepository = scope.ServiceProvider.GetRequiredService<IWorkflowGroupRepository>();
                    await workflowGroupRepository.UpdateAsync(User, new GXWorkflowGroup[] { wfg });

                    //Create default block group and add user group to it.
                    GXBlockGroup bg = new GXBlockGroup() { CreationTime = now, Name = Properties.Resources.Default, Default = true };
                    bg.UserGroups.Add(ug);
                    IBlockGroupRepository blockGroupRepository = scope.ServiceProvider.GetRequiredService<IBlockGroupRepository>();
                    await blockGroupRepository.UpdateAsync(User, new GXBlockGroup[] { bg });

                    //Create default script group and add user group to it.
                    GXScriptGroup sgs = new GXScriptGroup() { CreationTime = now, Name = Properties.Resources.Default, Default = true };
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
