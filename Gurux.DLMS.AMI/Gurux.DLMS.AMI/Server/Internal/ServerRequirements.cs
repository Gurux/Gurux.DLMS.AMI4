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
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Server.Models;
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server.Internal
{
    internal static class ServerRequirements
    {

        /// <summary>
        /// Adds a policy to check for required scopes.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="scope">List of any required scopes. The token must contain at least one of the listed scopes.</param>
        /// <returns></returns>
        public static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder builder, params string[] scope)
        {
            return builder.RequireClaim("scope", scope);
        }

        /// <summary>
        /// Add system error requirements that are used for policy.
        /// </summary>
        public static void AddServerRequirements(AuthorizationOptions options, string issuer)
        {
            AddSystemLogRequirements(options, issuer);
            AddDeviceTemplateRequirements(options, issuer);
            AddDeviceTemplateGroupRequirements(options, issuer);
            AddDeviceRequirements(options, issuer);
            AddDeviceGroupRequirements(options, issuer);
            AddDeviceErrorRequirements(options, issuer);
            AddDeviceTraceRequirements(options, issuer);
            AddDeviceActionRequirements(options, issuer);
            AddTokenRequirements(options, issuer);
            AddModuleRequirements(options, issuer);
            AddModuleGroupRequirements(options, issuer);
            AddModuleLogRequirements(options, issuer);
            AddWorkflowRequirements(options, issuer);
            AddWorkflowGroupRequirements(options, issuer);
            AddWorkflowLogRequirements(options, issuer);
            AddScriptRequirements(options, issuer);
            AddManufacturerRequirements(options, issuer);
            AddManufacturerGroupRequirements(options, issuer);
            AddUserSettingRequirements(options, issuer);
            AddScriptGroupRequirements(options, issuer);
            AddScriptLogRequirements(options, issuer);
            AddUserRequirements(options, issuer);
            AddUserGroupRequirements(options, issuer);
            AddUserErrorRequirements(options, issuer);
            AddUserActionRequirements(options, issuer);
            AddAgentRequirements(options, issuer);
            AddAgentGroupRequirements(options, issuer);
            AddAgentLogRequirements(options, issuer);
            AddScheduleErrorRequirements(options, issuer);
            AddSchedulerRequirements(options, issuer);
            AddScheduleGroupRequirements(options, issuer);
            AddBlockRequirements(options, issuer);
            AddBlockGroupRequirements(options, issuer);
            AddLocalizationRequirements(options, issuer);
            AddTaskRequirements(options, issuer);
            AddComponentViewRequirements(options, issuer);
            AddComponentViewGroupRequirements(options, issuer);
            AddConfigurationRequirements(options, issuer);
            AddOptionRequirements(options, issuer);
            AddValueRequirements(options, issuer);
            AddObjectRequirements(options, issuer);
            AddAttributeRequirements(options, issuer);
            AddObjectTemplateRequirements(options, issuer);
            AddAttributeTemplateRequirements(options, issuer);
            AddTriggerRequirements(options, issuer);
            AddTriggerGroupRequirements(options, issuer);
            AddRoleRequirements(options, issuer);
            AddKeyManagementRequirements(options, issuer);
            AddKeyManagementGroupRequirements(options, issuer);
            AddKeyManagementLogRequirements(options, issuer);
            AddGatewayGroupRequirements(options, issuer);
            AddGatewayRequirements(options, issuer);
            AddGatewayLogRequirements(options, issuer);
            AddSubtotalRequirements(options, issuer);
            AddSubtotalGroupRequirements(options, issuer);
            AddSubtotalValueRequirements(options, issuer);
            AddSubtotalLogRequirements(options, issuer);
            //Add roles from the database.
        }

        /// <summary>
        /// Add system error requirements that are used for policy.
        /// </summary>
        private static void AddSystemLogRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXSystemLogPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXSystemLogPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.SystemLogManager } });
            });
            options.AddPolicy(GXSystemLogPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXSystemLogPolicies.Clear, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.SystemLogManager } });
            });
            options.AddPolicy(GXSystemLogPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXSystemLogPolicies.Close, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.SystemLogManager } });
            });
        }

        /// <summary>
        /// Add device error requirements that are used for policy.
        /// </summary>
        private static void AddDeviceErrorRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXDeviceErrorPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDeviceErrorPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceLogManager } });
            });
            options.AddPolicy(GXDeviceErrorPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDeviceErrorPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceLogManager } });
            });
            options.AddPolicy(GXDeviceErrorPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDeviceErrorPolicies.Clear, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceLogManager } });
            });
            options.AddPolicy(GXDeviceErrorPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDeviceErrorPolicies.Close, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceLogManager } });
            });
        }

        /// <summary>
        /// Add token requirements that are used for policy.
        /// </summary>
        private static void AddTokenRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXTokenPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXTokenPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.TokenManager } });
            });
            options.AddPolicy(GXTokenPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXTokenPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.TokenManager } });
            });
            options.AddPolicy(GXTokenPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXTokenPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.TokenManager } });
            });
            options.AddPolicy(GXTokenPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXTokenPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.TokenManager } });
            });
        }

        /// <summary>
        /// Add module requirements that are used for policy.
        /// </summary>
        private static void AddModuleRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXModulePolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXModulePolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ModuleLogManager } });
            });
            options.AddPolicy(GXModulePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXModulePolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ModuleLogManager } });
            });
            options.AddPolicy(GXModulePolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXModulePolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ModuleLogManager } });
            });
            options.AddPolicy(GXModulePolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXModulePolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ModuleLogManager } });
            });
        }

        /// <summary>
        /// Add module log requirements that are used for policy.
        /// </summary>
        private static void AddModuleLogRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXModuleLogPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXModuleLogPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ModuleLogManager } });
            });
            options.AddPolicy(GXModuleLogPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXModuleLogPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ModuleLogManager } });
            });
            options.AddPolicy(GXModuleLogPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXModuleLogPolicies.Clear, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ModuleLogManager } });
            });
            options.AddPolicy(GXModuleLogPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXModuleLogPolicies.Close, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ModuleLogManager } });
            });
        }

        /// <summary>
        /// Add workflow requirements that are used for policy.
        /// </summary>
        private static void AddWorkflowRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXWorkflowPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.WorkflowLogManager } });
            });
            options.AddPolicy(GXWorkflowPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.WorkflowLogManager } });
            });
            options.AddPolicy(GXWorkflowPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.WorkflowLogManager } });
            });
            options.AddPolicy(GXWorkflowPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.WorkflowLogManager } });
            });
        }

        /// <summary>
        /// Add workflow group requirements that are used for policy.
        /// </summary>
        private static void AddWorkflowGroupRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXWorkflowGroupPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowGroupPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.WorkflowLogManager } });
            });
            options.AddPolicy(GXWorkflowGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowGroupPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.WorkflowLogManager } });
            });
            options.AddPolicy(GXWorkflowGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowGroupPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.WorkflowLogManager } });
            });
            options.AddPolicy(GXWorkflowGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowGroupPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.WorkflowLogManager } });
            });
        }

        /// <summary>
        /// Add workflow log requirements that are used for policy.
        /// </summary>
        private static void AddWorkflowLogRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXWorkflowLogPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowLogPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.WorkflowLogManager } });
            });
            options.AddPolicy(GXWorkflowLogPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowLogPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.WorkflowLogManager } });
            });
            options.AddPolicy(GXWorkflowLogPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowLogPolicies.Clear, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.WorkflowLogManager } });
            });
            options.AddPolicy(GXWorkflowLogPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowLogPolicies.Close, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.WorkflowLogManager } });
            });
        }

        /// <summary>
        /// Add block requirements that are used for policy.
        /// </summary>
        private static void AddBlockRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXBlockPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXBlockPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.BlockManager } });
            });
            options.AddPolicy(GXBlockPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXBlockPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.BlockManager } });
            });
            options.AddPolicy(GXBlockPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXBlockPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.BlockManager } });
            });
            options.AddPolicy(GXBlockPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXBlockPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.BlockManager } });
            });
            options.AddPolicy(GXBlockPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXBlockPolicies.Close, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.User, GXRoles.BlockManager } });
            });
        }

        /// <summary>
        /// Add block group requirements that are used for policy.
        /// </summary>
        private static void AddBlockGroupRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXBlockGroupPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXBlockGroupPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.BlockGroupManager } });
            });
            options.AddPolicy(GXBlockGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXBlockGroupPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.BlockGroupManager } });
            });
            options.AddPolicy(GXBlockGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXBlockGroupPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.BlockGroupManager } });
            });
            options.AddPolicy(GXBlockGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXBlockGroupPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.BlockGroupManager } });
            });
        }

        /// <summary>
        /// Add localization requirements that are used for policy.
        /// </summary>
        private static void AddLocalizationRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXLocalizationPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXLocalizationPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.User, GXRoles.Agent, GXRoles.LocalizationManager } });
            });
            options.AddPolicy(GXLocalizationPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXLocalizationPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.LocalizationManager } });
            });
            options.AddPolicy(GXLocalizationPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXLocalizationPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.LocalizationManager } });
            });
            options.AddPolicy(GXLocalizationPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXLocalizationPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.LocalizationManager } });
            });
            options.AddPolicy(GXLocalizationPolicies.Refresh, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXLocalizationPolicies.Refresh, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.LocalizationManager } });
            });
        }

        /// <summary>
        /// Add task requirements that are used for policy.
        /// </summary>
        private static void AddTaskRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXTaskPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXTaskPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.TaskManager } });
            });
            options.AddPolicy(GXTaskPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXTaskPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.TaskManager } });
            });
            options.AddPolicy(GXTaskPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXTaskPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.TaskManager } });
            });
            options.AddPolicy(GXTaskPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXTaskPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.TaskManager } });
            });
        }
        /// <summary>
        /// Add schedule error requirements that are used for policy.
        /// </summary>
        private static void AddScheduleErrorRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXScheduleLogPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXScheduleLogPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScheduleLogManager } });
            });
            options.AddPolicy(GXScheduleLogPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXScheduleLogPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScheduleLogManager } });
            });
            options.AddPolicy(GXScheduleLogPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXScheduleLogPolicies.Clear, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScheduleLogManager } });
            });
            options.AddPolicy(GXScheduleLogPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXScheduleLogPolicies.Close, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScheduleLogManager } });
            });
        }

        /// <summary>
        /// Add script requirements that are used for policy.
        /// </summary>
        private static void AddScriptRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXScriptPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXScriptPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScriptManager } });
            });
            options.AddPolicy(GXScriptPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXScriptPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScriptManager } });
            });
            options.AddPolicy(GXScriptPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXScriptPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScriptManager } });
            });
            options.AddPolicy(GXScriptPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXScriptPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScriptManager } });
            });
        }

        /// <summary>
        /// Add manufacturer requirements that are used for policy.
        /// </summary>
        private static void AddManufacturerRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXManufacturerPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXManufacturerPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.Manufacturer, GXRoles.ManufacturerManager } });
            });
            options.AddPolicy(GXManufacturerPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXManufacturerPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ManufacturerManager } });
            });
            options.AddPolicy(GXManufacturerPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXManufacturerPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ManufacturerManager } });
            });
            options.AddPolicy(GXManufacturerPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXManufacturerPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ManufacturerManager } });
            });
        }

        /// <summary>
        /// Add manufacturer group requirements that are used for policy.
        /// </summary>
        private static void AddManufacturerGroupRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXManufacturerGroupPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXManufacturerGroupPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ManufacturerGroup, GXRoles.ManufacturerGroupManager } });
            });
            options.AddPolicy(GXManufacturerGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXManufacturerGroupPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ManufacturerGroupManager } });
            });
            options.AddPolicy(GXManufacturerGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXManufacturerGroupPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ManufacturerGroupManager } });
            });
            options.AddPolicy(GXManufacturerGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXManufacturerGroupPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ManufacturerGroupManager } });
            });
        }

        /// <summary>
        /// Add key management requirements that are used for policy.
        /// </summary>
        private static void AddKeyManagementRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXKeyManagementPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.KeyManagement, GXRoles.KeyManagementManager } });
            });
            options.AddPolicy(GXKeyManagementPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.KeyManagementManager } });
            });
            options.AddPolicy(GXKeyManagementPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.KeyManagementManager } });
            });
            options.AddPolicy(GXKeyManagementPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.KeyManagementManager } });
            });
        }

        /// <summary>
        /// Add key management group requirements that are used for policy.
        /// </summary>
        private static void AddKeyManagementGroupRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXKeyManagementGroupPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementGroupPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.KeyManagementGroup, GXRoles.KeyManagementGroupManager } });
            });
            options.AddPolicy(GXKeyManagementGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementGroupPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.KeyManagementGroupManager } });
            });
            options.AddPolicy(GXKeyManagementGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementGroupPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.KeyManagementGroupManager } });
            });
            options.AddPolicy(GXKeyManagementGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementGroupPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.KeyManagementGroupManager } });
            });
        }

        /// <summary>
        /// Add key management logs requirements that are used for policy.
        /// </summary>
        private static void AddKeyManagementLogRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXKeyManagementLogPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementLogPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.KeyManagementLogManager } });
            });
            options.AddPolicy(GXKeyManagementLogPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementLogPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.KeyManagementLogManager } });
            });
            options.AddPolicy(GXKeyManagementLogPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementLogPolicies.Clear, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.KeyManagementLogManager } });
            });
            options.AddPolicy(GXKeyManagementLogPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementLogPolicies.Close, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.KeyManagementLogManager } });
            });
        }

        /// <summary>
        /// Add user setting requirements that are used for policy.
        /// </summary>
        private static void AddUserSettingRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXUserSettingPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserSettingPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.User } });
            });
            options.AddPolicy(GXUserSettingPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserSettingPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.User } });
            });
            options.AddPolicy(GXUserSettingPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserSettingPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.User } });
            });
            options.AddPolicy(GXUserSettingPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserSettingPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.User } });
            });
        }

        /// <summary>
        /// Add script group requirements that are used for policy.
        /// </summary>
        private static void AddScriptGroupRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXScriptGroupPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXScriptGroupPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScriptLogManager } });
            });
            options.AddPolicy(GXScriptGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXScriptGroupPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScriptLogManager } });
            });
            options.AddPolicy(GXScriptGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXScriptGroupPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScriptLogManager } });
            });
            options.AddPolicy(GXScriptGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXScriptGroupPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScriptLogManager } });
            });
        }

        /// <summary>
        /// Add script logs requirements that are used for policy.
        /// </summary>
        private static void AddScriptLogRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXScriptLogPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXScriptLogPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScriptLogManager } });
            });
            options.AddPolicy(GXScriptLogPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXScriptLogPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScriptLogManager } });
            });
            options.AddPolicy(GXScriptLogPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXScriptLogPolicies.Clear, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScriptLogManager } });
            });
            options.AddPolicy(GXScriptLogPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXScriptLogPolicies.Close, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScriptLogManager } });
            });
        }

        /// <summary>
        /// Add user error requirements that are used for policy.
        /// </summary>
        private static void AddUserErrorRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXUserErrorPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserErrorPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.UserErrorManager } });
            });
            options.AddPolicy(GXUserErrorPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserErrorPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.UserErrorManager } });
            });
            options.AddPolicy(GXUserErrorPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserErrorPolicies.Clear, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.UserErrorManager } });
            });
            options.AddPolicy(GXUserErrorPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserErrorPolicies.Close, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.UserErrorManager } });
            });
        }

        /// <summary>
        /// Add agent requirements that are used for policy.
        /// </summary>
        private static void AddAgentRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXAgentPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXAgentPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.Agent, GXRoles.AgentManager } });
            });
            options.AddPolicy(GXAgentPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXAgentPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.AgentManager } });
            });
            options.AddPolicy(GXAgentPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXAgentPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.AgentManager } });
            });
            options.AddPolicy(GXAgentPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXAgentPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.AgentManager } });
            });
        }

        /// <summary>
        /// Add module group requirements that are used for policy.
        /// </summary>
        private static void AddModuleGroupRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXModuleGroupPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXModuleGroupPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ModuleGroupManager } });
            });
            options.AddPolicy(GXModuleGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXModuleGroupPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ModuleGroupManager } });
            });
            options.AddPolicy(GXModuleGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXModuleGroupPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ModuleGroupManager } });
            });
            options.AddPolicy(GXModuleGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXModuleGroupPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ModuleGroupManager } });
            });
        }


        /// <summary>
        /// Add agent group requirements that are used for policy.
        /// </summary>
        private static void AddAgentGroupRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXAgentGroupPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXAgentGroupPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.AgentManager } });
            });
            options.AddPolicy(GXAgentGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXAgentGroupPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.AgentManager } });
            });
            options.AddPolicy(GXAgentGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXAgentGroupPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.AgentManager } });
            });
            options.AddPolicy(GXAgentGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXAgentGroupPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.AgentManager } });
            });
        }

        /// <summary>
        /// Add agent log requirements that are used for policy.
        /// </summary>
        private static void AddAgentLogRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXAgentLogPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXAgentLogPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.AgentLogManager } });
            });
            options.AddPolicy(GXAgentLogPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXAgentLogPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.AgentLogManager } });
            });
            options.AddPolicy(GXAgentLogPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXAgentLogPolicies.Clear, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.AgentLogManager } });
            });
            options.AddPolicy(GXAgentLogPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXAgentLogPolicies.Close, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.AgentLogManager } });
            });
        }

        /// <summary>
        /// Add schedule requirements that are used for policy.
        /// </summary>
        private static void AddSchedulerRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXSchedulePolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScheduleManager } });
            });
            options.AddPolicy(GXSchedulePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScheduleManager } });
            });
            options.AddPolicy(GXSchedulePolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScheduleManager } });
            });
            options.AddPolicy(GXSchedulePolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScheduleManager } });
            });
        }

        /// <summary>
        /// Add schedule group requirements that are used for policy.
        /// </summary>
        private static void AddScheduleGroupRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXScheduleGroupPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScheduleGroupManager } });
            });
            options.AddPolicy(GXScheduleGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScheduleGroupManager } });
            });
            options.AddPolicy(GXScheduleGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScheduleGroupManager } });
            });
            options.AddPolicy(GXScheduleGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ScheduleGroupManager } });
            });
        }


        /// <summary>
        /// Add user action requirements that are used for policy.
        /// </summary>
        private static void AddUserActionRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXUserActionPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserActionPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.UserActionManager } });
            });
            options.AddPolicy(GXUserActionPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserActionPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.UserActionManager } });
            });
            options.AddPolicy(GXUserActionPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserActionPolicies.Clear, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.UserActionManager } });
            });
        }

        /// <summary>
        /// Add user requirements that are used for policy.
        /// </summary>
        private static void AddUserRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXUserPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.UserManager } });
            });
            options.AddPolicy(GXUserPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.UserManager } });
            });
            options.AddPolicy(GXUserPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.UserManager } });
            });
            options.AddPolicy(GXUserPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.UserManager } });
            });
        }

        /// <summary>
        /// Add user group requirements that are used for policy.
        /// </summary>
        private static void AddUserGroupRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXUserGroupPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserGroupPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.UserGroupManager } });
            });
            options.AddPolicy(GXUserGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserGroupPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.UserManager } });
            });
            options.AddPolicy(GXUserGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserGroupPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.UserManager } });
            });
            options.AddPolicy(GXUserGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXUserGroupPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.UserManager } });
            });
        }

        /// <summary>
        /// Add device requirements that are used for policy.
        /// </summary>
        private static void AddDeviceRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXDevicePolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDevicePolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceManager, GXRoles.Device } });
            });
            options.AddPolicy(GXDevicePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDevicePolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceManager } });
            });
            options.AddPolicy(GXDevicePolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDevicePolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceManager } });
            });
            options.AddPolicy(GXDevicePolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDevicePolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceManager } });
            });
        }

        /// <summary>
        /// Add device group requirements that are used for policy.
        /// </summary>
        private static void AddDeviceGroupRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXDeviceGroupPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDeviceGroupPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceGroupManager, GXRoles.DeviceGroup } });
            });
            options.AddPolicy(GXDeviceGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDeviceGroupPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceGroupManager } });
            });
            options.AddPolicy(GXDeviceGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDeviceGroupPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceGroupManager } });
            });
            options.AddPolicy(GXDeviceGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDeviceGroupPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceGroupManager } });
            });
        }

        /// <summary>
        /// Add device traces requirements that are used for policy.
        /// </summary>
        private static void AddDeviceTraceRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXDeviceTracePolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDeviceTracePolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceTraceManager } });
            });
            options.AddPolicy(GXDeviceTracePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDeviceTracePolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceTraceManager } });
            });
            options.AddPolicy(GXDeviceTracePolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDeviceTracePolicies.Clear, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceTraceManager } });
            });
        }

        /// <summary>
        /// Add device actions requirements that are used for policy.
        /// </summary>
        private static void AddDeviceActionRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXDeviceActionPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDeviceActionPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceActionManager } });
            });
            options.AddPolicy(GXDeviceActionPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDeviceActionPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceActionManager } });
            });
            options.AddPolicy(GXDeviceActionPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDeviceActionPolicies.Clear, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceActionManager } });
            });
        }

        /// <summary>
        /// Add device template requirements that are used for policy.
        /// </summary>
        private static void AddDeviceTemplateRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXDeviceTemplatePolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDeviceTemplatePolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceTemplateManager, GXRoles.DeviceTemplate } });
            });
            options.AddPolicy(GXDeviceTemplatePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDeviceTemplatePolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceTemplateManager } });
            });
            options.AddPolicy(GXDeviceTemplatePolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDeviceTemplatePolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceTemplateManager } });
            });
            options.AddPolicy(GXDeviceTemplatePolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDeviceTemplatePolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceTemplateManager } });
            });
        }

        /// <summary>
        /// Add device template group requirements that are used for policy.
        /// </summary>
        private static void AddDeviceTemplateGroupRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXDeviceTemplateGroupPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDeviceTemplateGroupPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceTemplateGroupManager, GXRoles.DeviceTemplate } });
            });
            options.AddPolicy(GXDeviceTemplateGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDeviceTemplateGroupPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceTemplateGroupManager } });
            });
            options.AddPolicy(GXDeviceTemplateGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDeviceTemplateGroupPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceTemplateGroupManager } });
            });
            options.AddPolicy(GXDeviceTemplateGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXDeviceTemplateGroupPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceTemplateGroupManager } });
            });
        }

        /// <summary>
        /// Add component view requirements that are used for policy.
        /// </summary>
        private static void AddComponentViewRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXComponentViewPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXComponentViewPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ComponentViewManager } });
            });
            options.AddPolicy(GXComponentViewPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXComponentViewPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ComponentViewManager } });
            });
            options.AddPolicy(GXComponentViewPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXComponentViewPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ComponentViewManager } });
            });
            options.AddPolicy(GXComponentViewPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXComponentViewPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ComponentViewManager } });
            });
            options.AddPolicy(GXComponentViewPolicies.Refresh, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXComponentViewPolicies.Refresh, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ComponentViewManager } });
            });
        }

        /// <summary>
        /// Add component view requirements that are used for policy.
        /// </summary>
        private static void AddComponentViewGroupRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXComponentViewGroupPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXComponentViewGroupPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ComponentViewGroupManager } });
            });
            options.AddPolicy(GXComponentViewGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXComponentViewGroupPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ComponentViewGroupManager } });
            });
            options.AddPolicy(GXComponentViewGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXComponentViewGroupPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ComponentViewGroupManager } });
            });
            options.AddPolicy(GXComponentViewGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXComponentViewGroupPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ComponentViewGroupManager } });
            });
        }

        /// <summary>
        /// Add configuration requirements that are used for policy.
        /// </summary>
        private static void AddConfigurationRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXConfigurationPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXConfigurationPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin } });
            });
            options.AddPolicy(GXConfigurationPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXConfigurationPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin } });
            });
            options.AddPolicy(GXConfigurationPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXConfigurationPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin } });
            });
            options.AddPolicy(GXConfigurationPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXConfigurationPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin } });
            });
            options.AddPolicy(GXConfigurationPolicies.Cron, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXConfigurationPolicies.Cron, issuer)
                { Roles = new string[] { GXRoles.Admin } });
            });
        }

        /// <summary>
        /// Add role requirements that are used for policy.
        /// </summary>
        private static void AddRoleRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXRolePolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXRolePolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin } });
            });
            options.AddPolicy(GXRolePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXRolePolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin } });
            });
            options.AddPolicy(GXRolePolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXRolePolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin } });
            });
            options.AddPolicy(GXRolePolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXRolePolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin } });
            });
        }

        /// <summary>
        /// Add option requirements that are used for policy.
        /// </summary>
        private static void AddOptionRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXOptionPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXOptionPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.User } });
            });
            options.AddPolicy(GXOptionPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXOptionPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.User } });
            });
            options.AddPolicy(GXOptionPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXOptionPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.User } });
            });
            options.AddPolicy(GXOptionPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXOptionPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.User } });
            });
        }


        /// <summary>
        /// Add value requirements that are used for policy.
        /// </summary>
        private static void AddValueRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXValuePolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXValuePolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ComponentViewGroupManager } });
            });
            options.AddPolicy(GXValuePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXValuePolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ComponentViewGroupManager } });
            });
            options.AddPolicy(GXValuePolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXValuePolicies.Clear, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ComponentViewGroupManager } });
            });
        }

        /// <summary>
        /// Add subtotal value requirements that are used for policy.
        /// </summary>
        private static void AddSubtotalValueRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXSubtotalValuePolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalValuePolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ComponentViewGroupManager } });
            });
            options.AddPolicy(GXSubtotalValuePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalValuePolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ComponentViewGroupManager } });
            });
            options.AddPolicy(GXSubtotalValuePolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalValuePolicies.Clear, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.ComponentViewGroupManager } });
            });
        }

        /// <summary>
        /// Add subtotal log requirements that are used for policy.
        /// </summary>
        private static void AddSubtotalLogRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXSubtotalLogPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalLogPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.SubtotalLogManager } });
            });
            options.AddPolicy(GXSubtotalLogPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalLogPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.SubtotalLogManager } });
            });
            options.AddPolicy(GXSubtotalLogPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalLogPolicies.Clear, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.SubtotalLogManager } });
            });
            options.AddPolicy(GXSubtotalLogPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalLogPolicies.Close, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.SubtotalLogManager } });
            });
        }

        /// <summary>
        /// Add object requirements that are used for policy.
        /// </summary>
        private static void AddObjectRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXObjectPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(new string[] { GXObjectPolicies.View, GXObjectPolicies.Edit, GXObjectPolicies.Delete, GXObjectPolicies.Clear }, issuer)
                {
                    Roles = new string[] { GXRoles.Admin, GXRoles.User }
                });
            });
            options.AddPolicy(GXObjectPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXObjectPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.User } });
            });
            options.AddPolicy(GXObjectPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXObjectPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.User } });
            });
            options.AddPolicy(GXObjectPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXObjectPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.User } });
            });
            options.AddPolicy(GXObjectPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXObjectPolicies.Clear, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.User } });
            });
        }

        /// <summary>
        /// Add attribute requirements that are used for policy.
        /// </summary>
        private static void AddAttributeRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXAttributePolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXAttributePolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.User } });
            });
            options.AddPolicy(GXAttributePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXAttributePolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.User } });
            });
            options.AddPolicy(GXAttributePolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXAttributePolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.User } });
            });
            options.AddPolicy(GXAttributePolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXAttributePolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.User } });
            });
            options.AddPolicy(GXAttributePolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXAttributePolicies.Clear, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.User } });
            });
        }

        /// <summary>
        /// Add object template requirements that are used for policy.
        /// </summary>
        private static void AddObjectTemplateRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXObjectTemplatePolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXObjectTemplatePolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceTemplateManager } });
            });
            options.AddPolicy(GXObjectTemplatePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXObjectTemplatePolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceTemplateManager } });
            });
            options.AddPolicy(GXObjectTemplatePolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXObjectTemplatePolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceTemplateManager } });
            });
            options.AddPolicy(GXObjectTemplatePolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXObjectTemplatePolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceTemplateManager } });
            });
            options.AddPolicy(GXObjectTemplatePolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXObjectTemplatePolicies.Clear, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceTemplateManager } });
            });
        }

        /// <summary>
        /// Add attribute template requirements that are used for policy.
        /// </summary>
        private static void AddAttributeTemplateRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXAttributeTemplatePolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXAttributeTemplatePolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceTemplateManager } });
            });
            options.AddPolicy(GXAttributeTemplatePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXAttributeTemplatePolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceTemplateManager } });
            });
            options.AddPolicy(GXAttributeTemplatePolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXAttributeTemplatePolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceTemplateManager } });
            });
            options.AddPolicy(GXAttributeTemplatePolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXAttributeTemplatePolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.DeviceTemplateManager } });
            });
            options.AddPolicy(GXAttributeTemplatePolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXAttributeTemplatePolicies.Clear, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.User } });
            });
        }


        /// <summary>
        /// Add trigger requirements that are used for policy.
        /// </summary>
        private static void AddTriggerRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXTriggerPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXTriggerPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.Trigger } });
            });
            options.AddPolicy(GXTriggerPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXTriggerPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.TriggerManager } });
            });
            options.AddPolicy(GXTriggerPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXTriggerPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.TriggerManager } });
            });
            options.AddPolicy(GXTriggerPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXTriggerPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.TriggerManager } });
            });
        }

        /// <summary>
        /// Add trigger group requirements that are used for policy.
        /// </summary>
        private static void AddTriggerGroupRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXTriggerGroupPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXTriggerGroupPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.TriggerGroup } });
            });
            options.AddPolicy(GXTriggerGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXTriggerGroupPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.TriggerGroupManager } });
            });
            options.AddPolicy(GXTriggerGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXTriggerGroupPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.TriggerGroupManager } });
            });
            options.AddPolicy(GXTriggerGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXTriggerGroupPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.TriggerGroupManager } });
            });
        }

        /// <summary>
        /// Add gateway requirements that are used for policy.
        /// </summary>
        private static void AddGatewayRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXGatewayPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXGatewayPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.Gateway, GXRoles.GatewayManager } });
            });
            options.AddPolicy(GXGatewayPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXGatewayPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.GatewayManager } });
            });
            options.AddPolicy(GXGatewayPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXGatewayPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.GatewayManager } });
            });
            options.AddPolicy(GXGatewayPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXGatewayPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.GatewayManager } });
            });
        }

        /// <summary>
        /// Add gateway group requirements that are used for policy.
        /// </summary>
        private static void AddGatewayGroupRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXGatewayGroupPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXGatewayGroupPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.GatewayManager } });
            });
            options.AddPolicy(GXGatewayGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXGatewayGroupPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.GatewayManager } });
            });
            options.AddPolicy(GXGatewayGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXGatewayGroupPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.GatewayManager } });
            });
            options.AddPolicy(GXGatewayGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXGatewayGroupPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.GatewayManager } });
            });
        }

        /// <summary>
        /// Add gateway log requirements that are used for policy.
        /// </summary>
        private static void AddGatewayLogRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXGatewayLogPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXGatewayLogPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.GatewayLogManager } });
            });
            options.AddPolicy(GXGatewayLogPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXGatewayLogPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.GatewayLogManager } });
            });
            options.AddPolicy(GXGatewayLogPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXGatewayLogPolicies.Clear, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.GatewayLogManager } });
            });
            options.AddPolicy(GXGatewayLogPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXGatewayLogPolicies.Close, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.GatewayLogManager } });
            });
        }

        /// <summary>
        /// Add subtotal requirements that are used for policy.
        /// </summary>
        private static void AddSubtotalRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXSubtotalPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.SubtotalManager } });
            });
            options.AddPolicy(GXSubtotalPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.SubtotalManager } });
            });
            options.AddPolicy(GXSubtotalPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.SubtotalManager } });
            });
            options.AddPolicy(GXSubtotalPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.SubtotalManager } });
            });
            options.AddPolicy(GXSubtotalPolicies.Calculate, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalPolicies.Calculate, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.SubtotalManager } });
            });
            options.AddPolicy(GXSubtotalPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalPolicies.Clear, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.SubtotalManager } });
            });
        }

        /// <summary>
        /// Add subtotal group requirements that are used for policy.
        /// </summary>
        private static void AddSubtotalGroupRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXSubtotalGroupPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalGroupPolicies.View, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.SubtotalGroupManager } });
            });
            options.AddPolicy(GXSubtotalGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalGroupPolicies.Add, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.SubtotalGroupManager } });
            });
            options.AddPolicy(GXSubtotalGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalGroupPolicies.Edit, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.SubtotalGroupManager } });
            });
            options.AddPolicy(GXSubtotalGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalGroupPolicies.Delete, issuer)
                { Roles = new string[] { GXRoles.Admin, GXRoles.SubtotalGroupManager } });
            });
        }

    }
}
