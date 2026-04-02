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
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

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
            AddContentRequirements(options, issuer);
            AddContentGroupRequirements(options, issuer);
            AddContentTypeRequirements(options, issuer);
            AddContentTypeGroupRequirements(options, issuer);
            AddMenuRequirements(options, issuer);
            AddMenuGroupRequirements(options, issuer);
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
            AddReportRequirements(options, issuer);
            AddReportGroupRequirements(options, issuer);
            AddReportLogRequirements(options, issuer);
            AddNotificationRequirements(options, issuer);
            AddNotificationGroupRequirements(options, issuer);
            AddNotificationLogRequirements(options, issuer);
            AddAppearanceRequirements(options, issuer);
            AddIpAddressRequirements(options, issuer);
            AddLocalizedResourceRequirements(options, issuer);
            AddAnnouncementRequirements(options, issuer);
        }

        /// <summary>
        /// Add system error requirements that are used for policy.
        /// </summary>
        private static void AddSystemLogRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXSystemLogPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSystemLogPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.SystemLogManager] });
            });
            options.AddPolicy(GXSystemLogPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSystemLogPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.SystemLogManager] });
            });
            options.AddPolicy(GXSystemLogPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSystemLogPolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.SystemLogManager] });
            });
            options.AddPolicy(GXSystemLogPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSystemLogPolicies.Close, issuer)
                { Roles = [GXRoles.Admin, GXRoles.SystemLogManager] });
            });
        }

        /// <summary>
        /// Add device error requirements that are used for policy.
        /// </summary>
        private static void AddDeviceErrorRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXDeviceLogPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDeviceLogPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceLogManager] });
            });
            options.AddPolicy(GXDeviceLogPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDeviceLogPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceLogManager] });
            });
            options.AddPolicy(GXDeviceLogPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDeviceLogPolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceLogManager] });
            });
            options.AddPolicy(GXDeviceLogPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDeviceLogPolicies.Close, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceLogManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXTokenPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.TokenManager] });
            });
            options.AddPolicy(GXTokenPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXTokenPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.TokenManager] });
            });
            options.AddPolicy(GXTokenPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXTokenPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.TokenManager] });
            });
            options.AddPolicy(GXTokenPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXTokenPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.TokenManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXModulePolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ModuleLogManager] });
            });
            options.AddPolicy(GXModulePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXModulePolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ModuleLogManager] });
            });
            options.AddPolicy(GXModulePolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXModulePolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ModuleLogManager] });
            });
            options.AddPolicy(GXModulePolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXModulePolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ModuleLogManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXModuleLogPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ModuleLogManager] });
            });
            options.AddPolicy(GXModuleLogPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXModuleLogPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ModuleLogManager] });
            });
            options.AddPolicy(GXModuleLogPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXModuleLogPolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ModuleLogManager] });
            });
            options.AddPolicy(GXModuleLogPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXModuleLogPolicies.Close, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ModuleLogManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.WorkflowLogManager] });
            });
            options.AddPolicy(GXWorkflowPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.WorkflowLogManager] });
            });
            options.AddPolicy(GXWorkflowPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.WorkflowLogManager] });
            });
            options.AddPolicy(GXWorkflowPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.WorkflowLogManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowGroupPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.WorkflowLogManager] });
            });
            options.AddPolicy(GXWorkflowGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowGroupPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.WorkflowLogManager] });
            });
            options.AddPolicy(GXWorkflowGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowGroupPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.WorkflowLogManager] });
            });
            options.AddPolicy(GXWorkflowGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowGroupPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.WorkflowLogManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowLogPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.WorkflowLogManager] });
            });
            options.AddPolicy(GXWorkflowLogPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowLogPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.WorkflowLogManager] });
            });
            options.AddPolicy(GXWorkflowLogPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowLogPolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.WorkflowLogManager] });
            });
            options.AddPolicy(GXWorkflowLogPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXWorkflowLogPolicies.Close, issuer)
                { Roles = [GXRoles.Admin, GXRoles.WorkflowLogManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXBlockPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.BlockManager] });
            });
            options.AddPolicy(GXBlockPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXBlockPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.BlockManager] });
            });
            options.AddPolicy(GXBlockPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXBlockPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.BlockManager] });
            });
            options.AddPolicy(GXBlockPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXBlockPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.BlockManager] });
            });
            options.AddPolicy(GXBlockPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXBlockPolicies.Close, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User, GXRoles.BlockManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXBlockGroupPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.BlockGroupManager] });
            });
            options.AddPolicy(GXBlockGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXBlockGroupPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.BlockGroupManager] });
            });
            options.AddPolicy(GXBlockGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXBlockGroupPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.BlockGroupManager] });
            });
            options.AddPolicy(GXBlockGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXBlockGroupPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.BlockGroupManager] });
            });
        }

        /// <summary>
        /// Add content requirements that are used for policy.
        /// </summary>
        private static void AddContentRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXContentPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXContentPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ContentManager] });
            });
            options.AddPolicy(GXContentPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXContentPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ContentManager] });
            });
            options.AddPolicy(GXContentPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXContentPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ContentManager] });
            });
            options.AddPolicy(GXContentPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXContentPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ContentManager] });
            });
            options.AddPolicy(GXContentPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXContentPolicies.Close, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User, GXRoles.ContentManager] });
            });
        }

        /// <summary>
        /// Add content group requirements that are used for policy.
        /// </summary>
        private static void AddContentGroupRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXContentGroupPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXContentGroupPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ContentGroupManager] });
            });
            options.AddPolicy(GXContentGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXContentGroupPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ContentGroupManager] });
            });
            options.AddPolicy(GXContentGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXContentGroupPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ContentGroupManager] });
            });
            options.AddPolicy(GXContentGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXContentGroupPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ContentGroupManager] });
            });
        }

        /// <summary>
        /// Add content type requirements that are used for policy.
        /// </summary>
        private static void AddContentTypeRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXContentTypePolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXContentTypePolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ContentType] });
            });
            options.AddPolicy(GXContentTypePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXContentTypePolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ContentType] });
            });
            options.AddPolicy(GXContentTypePolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXContentTypePolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ContentType] });
            });
            options.AddPolicy(GXContentTypePolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXContentTypePolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ContentType] });
            });
        }

        /// <summary>
        /// Add content type group requirements that are used for policy.
        /// </summary>
        private static void AddContentTypeGroupRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXContentTypeGroupPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXContentTypeGroupPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ContentTypeGroup] });
            });
            options.AddPolicy(GXContentTypeGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXContentTypeGroupPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ContentTypeGroup] });
            });
            options.AddPolicy(GXContentTypeGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXContentTypeGroupPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ContentTypeGroup] });
            });
            options.AddPolicy(GXContentTypeGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXContentTypeGroupPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ContentTypeGroup] });
            });
        }

        /// <summary>
        /// Add menu requirements that are used for policy.
        /// </summary>
        private static void AddMenuRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXMenuPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXMenuPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.MenuManager] });
            });
            options.AddPolicy(GXMenuPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXMenuPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.MenuManager] });
            });
            options.AddPolicy(GXMenuPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXMenuPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.MenuManager] });
            });
            options.AddPolicy(GXMenuPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXMenuPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.MenuManager] });
            });
        }

        /// <summary>
        /// Add menu group requirements that are used for policy.
        /// </summary>
        private static void AddMenuGroupRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXMenuGroupPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXMenuGroupPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.MenuGroupManager] });
            });
            options.AddPolicy(GXMenuGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXMenuGroupPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.MenuGroupManager] });
            });
            options.AddPolicy(GXMenuGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXMenuGroupPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.MenuGroupManager] });
            });
            options.AddPolicy(GXMenuGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXMenuGroupPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.MenuGroupManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXLocalizationPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User, GXRoles.Agent, GXRoles.LocalizationManager] });
            });
            options.AddPolicy(GXLocalizationPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXLocalizationPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.LocalizationManager] });
            });
            options.AddPolicy(GXLocalizationPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXLocalizationPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.LocalizationManager] });
            });
            options.AddPolicy(GXLocalizationPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXLocalizationPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.LocalizationManager] });
            });
            options.AddPolicy(GXLocalizationPolicies.Refresh, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXLocalizationPolicies.Refresh, issuer)
                { Roles = [GXRoles.Admin, GXRoles.LocalizationManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXTaskPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.TaskManager] });
            });
            options.AddPolicy(GXTaskPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXTaskPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.TaskManager] });
            });
            options.AddPolicy(GXTaskPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXTaskPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.TaskManager] });
            });
            options.AddPolicy(GXTaskPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXTaskPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.TaskManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXScheduleLogPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScheduleLogManager] });
            });
            options.AddPolicy(GXScheduleLogPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXScheduleLogPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScheduleLogManager] });
            });
            options.AddPolicy(GXScheduleLogPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXScheduleLogPolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScheduleLogManager] });
            });
            options.AddPolicy(GXScheduleLogPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXScheduleLogPolicies.Close, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScheduleLogManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXScriptPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScriptManager] });
            });
            options.AddPolicy(GXScriptPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXScriptPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScriptManager] });
            });
            options.AddPolicy(GXScriptPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXScriptPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScriptManager] });
            });
            options.AddPolicy(GXScriptPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXScriptPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScriptManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXManufacturerPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.Manufacturer, GXRoles.ManufacturerManager] });
            });
            options.AddPolicy(GXManufacturerPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXManufacturerPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ManufacturerManager] });
            });
            options.AddPolicy(GXManufacturerPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXManufacturerPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ManufacturerManager] });
            });
            options.AddPolicy(GXManufacturerPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXManufacturerPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ManufacturerManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXManufacturerGroupPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ManufacturerGroup, GXRoles.ManufacturerGroupManager] });
            });
            options.AddPolicy(GXManufacturerGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXManufacturerGroupPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ManufacturerGroupManager] });
            });
            options.AddPolicy(GXManufacturerGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXManufacturerGroupPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ManufacturerGroupManager] });
            });
            options.AddPolicy(GXManufacturerGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXManufacturerGroupPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ManufacturerGroupManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.KeyManagement, GXRoles.KeyManagementManager] });
            });
            options.AddPolicy(GXKeyManagementPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.KeyManagementManager] });
            });
            options.AddPolicy(GXKeyManagementPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.KeyManagementManager] });
            });
            options.AddPolicy(GXKeyManagementPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.KeyManagementManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementGroupPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.KeyManagementGroup, GXRoles.KeyManagementGroupManager] });
            });
            options.AddPolicy(GXKeyManagementGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementGroupPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.KeyManagementGroupManager] });
            });
            options.AddPolicy(GXKeyManagementGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementGroupPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.KeyManagementGroupManager] });
            });
            options.AddPolicy(GXKeyManagementGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementGroupPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.KeyManagementGroupManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementLogPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.KeyManagementLogManager] });
            });
            options.AddPolicy(GXKeyManagementLogPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementLogPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.KeyManagementLogManager] });
            });
            options.AddPolicy(GXKeyManagementLogPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementLogPolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.KeyManagementLogManager] });
            });
            options.AddPolicy(GXKeyManagementLogPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXKeyManagementLogPolicies.Close, issuer)
                { Roles = [GXRoles.Admin, GXRoles.KeyManagementLogManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXUserSettingPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
            });
            options.AddPolicy(GXUserSettingPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXUserSettingPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
            });
            options.AddPolicy(GXUserSettingPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXUserSettingPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
            });
            options.AddPolicy(GXUserSettingPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXUserSettingPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXScriptGroupPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScriptLogManager] });
            });
            options.AddPolicy(GXScriptGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXScriptGroupPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScriptLogManager] });
            });
            options.AddPolicy(GXScriptGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXScriptGroupPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScriptLogManager] });
            });
            options.AddPolicy(GXScriptGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXScriptGroupPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScriptLogManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXScriptLogPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScriptLogManager] });
            });
            options.AddPolicy(GXScriptLogPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXScriptLogPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScriptLogManager] });
            });
            options.AddPolicy(GXScriptLogPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXScriptLogPolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScriptLogManager] });
            });
            options.AddPolicy(GXScriptLogPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXScriptLogPolicies.Close, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScriptLogManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXUserErrorPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.UserErrorManager] });
            });
            options.AddPolicy(GXUserErrorPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXUserErrorPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.UserErrorManager] });
            });
            options.AddPolicy(GXUserErrorPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXUserErrorPolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.UserErrorManager] });
            });
            options.AddPolicy(GXUserErrorPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXUserErrorPolicies.Close, issuer)
                { Roles = [GXRoles.Admin, GXRoles.UserErrorManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAgentPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.Agent] });
            });
            options.AddPolicy(GXAgentPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAgentPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.Agent] });
            });
            options.AddPolicy(GXAgentPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAgentPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.Agent] });
            });
            options.AddPolicy(GXAgentPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAgentPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.Agent] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXModuleGroupPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ModuleGroupManager] });
            });
            options.AddPolicy(GXModuleGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXModuleGroupPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ModuleGroupManager] });
            });
            options.AddPolicy(GXModuleGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXModuleGroupPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ModuleGroupManager] });
            });
            options.AddPolicy(GXModuleGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXModuleGroupPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ModuleGroupManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAgentGroupPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.AgentGroup] });
            });
            options.AddPolicy(GXAgentGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAgentGroupPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.AgentGroup] });
            });
            options.AddPolicy(GXAgentGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAgentGroupPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.AgentGroup] });
            });
            options.AddPolicy(GXAgentGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAgentGroupPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.AgentGroup] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAgentLogPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.AgentLog] });
            });
            options.AddPolicy(GXAgentLogPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAgentLogPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.AgentLog] });
            });
            options.AddPolicy(GXAgentLogPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAgentLogPolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.AgentLog] });
            });
            options.AddPolicy(GXAgentLogPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAgentLogPolicies.Close, issuer)
                { Roles = [GXRoles.Admin, GXRoles.AgentLog] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSchedulePolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScheduleManager] });
            });
            options.AddPolicy(GXSchedulePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSchedulePolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScheduleManager] });
            });
            options.AddPolicy(GXSchedulePolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSchedulePolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScheduleManager] });
            });
            options.AddPolicy(GXSchedulePolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSchedulePolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScheduleManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXScheduleGroupPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScheduleGroupManager] });
            });
            options.AddPolicy(GXScheduleGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXScheduleGroupPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScheduleGroupManager] });
            });
            options.AddPolicy(GXScheduleGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXScheduleGroupPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScheduleGroupManager] });
            });
            options.AddPolicy(GXScheduleGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXScheduleGroupPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ScheduleGroupManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXUserActionPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.UserActionManager] });
            });
            options.AddPolicy(GXUserActionPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXUserActionPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.UserActionManager] });
            });
            options.AddPolicy(GXUserActionPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXUserActionPolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.UserActionManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXUserPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.UserManager] });
            });
            options.AddPolicy(GXUserPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXUserPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.UserManager] });
            });
            options.AddPolicy(GXUserPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXUserPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.UserManager] });
            });
            options.AddPolicy(GXUserPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXUserPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.UserManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXUserGroupPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.UserGroupManager] });
            });
            options.AddPolicy(GXUserGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXUserGroupPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.UserManager] });
            });
            options.AddPolicy(GXUserGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXUserGroupPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.UserManager] });
            });
            options.AddPolicy(GXUserGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXUserGroupPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.UserManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDevicePolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceManager, GXRoles.Device] });
            });
            options.AddPolicy(GXDevicePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDevicePolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceManager] });
            });
            options.AddPolicy(GXDevicePolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDevicePolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceManager] });
            });
            options.AddPolicy(GXDevicePolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDevicePolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDeviceGroupPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceGroupManager, GXRoles.DeviceGroup] });
            });
            options.AddPolicy(GXDeviceGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDeviceGroupPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceGroupManager] });
            });
            options.AddPolicy(GXDeviceGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDeviceGroupPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceGroupManager] });
            });
            options.AddPolicy(GXDeviceGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDeviceGroupPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceGroupManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDeviceTracePolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceTraceManager] });
            });
            options.AddPolicy(GXDeviceTracePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDeviceTracePolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceTraceManager] });
            });
            options.AddPolicy(GXDeviceTracePolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDeviceTracePolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceTraceManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDeviceActionPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceActionManager] });
            });
            options.AddPolicy(GXDeviceActionPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDeviceActionPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceActionManager] });
            });
            options.AddPolicy(GXDeviceActionPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDeviceActionPolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceActionManager] });
            });
        }

        /// <summary>
        /// Add device template requirements that are used for policy.
        /// </summary>
        private static void AddDeviceTemplateRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXDeviceTemplatePolicies.View, (Action<AuthorizationPolicyBuilder>)(policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDeviceTemplatePolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceTemplateManager, GXRoles.DeviceTemplate] });
            }));
            options.AddPolicy(GXDeviceTemplatePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDeviceTemplatePolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceTemplateManager] });
            });
            options.AddPolicy(GXDeviceTemplatePolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDeviceTemplatePolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceTemplateManager] });
            });
            options.AddPolicy(GXDeviceTemplatePolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDeviceTemplatePolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceTemplateManager] });
            });
        }

        /// <summary>
        /// Add device template group requirements that are used for policy.
        /// </summary>
        private static void AddDeviceTemplateGroupRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXDeviceTemplateGroupPolicies.View, (Action<AuthorizationPolicyBuilder>)(policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDeviceTemplateGroupPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceTemplateGroupManager, GXRoles.DeviceTemplate] });
            }));
            options.AddPolicy(GXDeviceTemplateGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDeviceTemplateGroupPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceTemplateGroupManager] });
            });
            options.AddPolicy(GXDeviceTemplateGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDeviceTemplateGroupPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceTemplateGroupManager] });
            });
            options.AddPolicy(GXDeviceTemplateGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXDeviceTemplateGroupPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceTemplateGroupManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXComponentViewPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ComponentViewManager] });
            });
            options.AddPolicy(GXComponentViewPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXComponentViewPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ComponentViewManager] });
            });
            options.AddPolicy(GXComponentViewPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXComponentViewPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ComponentViewManager] });
            });
            options.AddPolicy(GXComponentViewPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXComponentViewPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ComponentViewManager] });
            });
            options.AddPolicy(GXComponentViewPolicies.Refresh, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXComponentViewPolicies.Refresh, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ComponentViewManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXComponentViewGroupPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ComponentViewGroupManager] });
            });
            options.AddPolicy(GXComponentViewGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXComponentViewGroupPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ComponentViewGroupManager] });
            });
            options.AddPolicy(GXComponentViewGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXComponentViewGroupPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ComponentViewGroupManager] });
            });
            options.AddPolicy(GXComponentViewGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXComponentViewGroupPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ComponentViewGroupManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXConfigurationPolicies.View, issuer)
                { Roles = [GXRoles.Admin] });
            });
            options.AddPolicy(GXConfigurationPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXConfigurationPolicies.Add, issuer)
                { Roles = [GXRoles.Admin] });
            });
            options.AddPolicy(GXConfigurationPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXConfigurationPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin] });
            });
            options.AddPolicy(GXConfigurationPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXConfigurationPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin] });
            });
            options.AddPolicy(GXConfigurationPolicies.Cron, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXConfigurationPolicies.Cron, issuer)
                { Roles = [GXRoles.Admin] });
            });
            options.AddPolicy(GXConfigurationPolicies.Prune, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXConfigurationPolicies.Prune, issuer)
                { Roles = [GXRoles.Admin] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXRolePolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
            });
            options.AddPolicy(GXRolePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXRolePolicies.Add, issuer)
                { Roles = [GXRoles.Admin] });
            });
            options.AddPolicy(GXRolePolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXRolePolicies.Edit, issuer)
                { Roles = [GXRoles.Admin] });
            });
            options.AddPolicy(GXRolePolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXRolePolicies.Delete, issuer)
                { Roles = [GXRoles.Admin] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXOptionPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
            });
            options.AddPolicy(GXOptionPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXOptionPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
            });
            options.AddPolicy(GXOptionPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXOptionPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
            });
            options.AddPolicy(GXOptionPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXOptionPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXValuePolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ComponentViewGroupManager] });
            });
            options.AddPolicy(GXValuePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXValuePolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ComponentViewGroupManager] });
            });
            options.AddPolicy(GXValuePolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXValuePolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ComponentViewGroupManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalValuePolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ComponentViewGroupManager] });
            });
            options.AddPolicy(GXSubtotalValuePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalValuePolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ComponentViewGroupManager] });
            });
            options.AddPolicy(GXSubtotalValuePolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalValuePolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ComponentViewGroupManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalLogPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.SubtotalLogManager] });
            });
            options.AddPolicy(GXSubtotalLogPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalLogPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.SubtotalLogManager] });
            });
            options.AddPolicy(GXSubtotalLogPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalLogPolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.SubtotalLogManager] });
            });
            options.AddPolicy(GXSubtotalLogPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalLogPolicies.Close, issuer)
                { Roles = [GXRoles.Admin, GXRoles.SubtotalLogManager] });
            });
        }

        /// <summary>
        /// Add report log requirements that are used for policy.
        /// </summary>
        private static void AddReportLogRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXReportLogPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXReportLogPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ReportLogManager] });
            });
            options.AddPolicy(GXReportLogPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXReportLogPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ReportLogManager] });
            });
            options.AddPolicy(GXReportLogPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXReportLogPolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ReportLogManager] });
            });
            options.AddPolicy(GXReportLogPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXReportLogPolicies.Close, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ReportLogManager] });
            });
        }

        /// <summary>
        /// Add notification log requirements that are used for policy.
        /// </summary>
        private static void AddNotificationLogRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXNotificationLogPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXNotificationLogPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.NotificationLogManager] });
            });
            options.AddPolicy(GXNotificationLogPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXNotificationLogPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.NotificationLogManager] });
            });
            options.AddPolicy(GXNotificationLogPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXNotificationLogPolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.NotificationLogManager] });
            });
            options.AddPolicy(GXNotificationLogPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXNotificationLogPolicies.Close, issuer)
                { Roles = [GXRoles.Admin, GXRoles.NotificationLogManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement([GXObjectPolicies.View], issuer)
                {
                    Roles = [GXRoles.Admin, GXRoles.User]
                });
            });
            options.AddPolicy(GXObjectPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXObjectPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
            });
            options.AddPolicy(GXObjectPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXObjectPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
            });
            options.AddPolicy(GXObjectPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXObjectPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
            });
            options.AddPolicy(GXObjectPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXObjectPolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAttributePolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
            });
            options.AddPolicy(GXAttributePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAttributePolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
            });
            options.AddPolicy(GXAttributePolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAttributePolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
            });
            options.AddPolicy(GXAttributePolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAttributePolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
            });
            options.AddPolicy(GXAttributePolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAttributePolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXObjectTemplatePolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceTemplateManager] });
            });
            options.AddPolicy(GXObjectTemplatePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXObjectTemplatePolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceTemplateManager] });
            });
            options.AddPolicy(GXObjectTemplatePolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXObjectTemplatePolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceTemplateManager] });
            });
            options.AddPolicy(GXObjectTemplatePolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXObjectTemplatePolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceTemplateManager] });
            });
            options.AddPolicy(GXObjectTemplatePolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXObjectTemplatePolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceTemplateManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAttributeTemplatePolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceTemplateManager] });
            });
            options.AddPolicy(GXAttributeTemplatePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAttributeTemplatePolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceTemplateManager] });
            });
            options.AddPolicy(GXAttributeTemplatePolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAttributeTemplatePolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceTemplateManager] });
            });
            options.AddPolicy(GXAttributeTemplatePolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAttributeTemplatePolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.DeviceTemplateManager] });
            });
            options.AddPolicy(GXAttributeTemplatePolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAttributeTemplatePolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXTriggerPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.Trigger] });
            });
            options.AddPolicy(GXTriggerPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXTriggerPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.TriggerManager] });
            });
            options.AddPolicy(GXTriggerPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXTriggerPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.TriggerManager] });
            });
            options.AddPolicy(GXTriggerPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXTriggerPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.TriggerManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXTriggerGroupPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.TriggerGroup] });
            });
            options.AddPolicy(GXTriggerGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXTriggerGroupPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.TriggerGroupManager] });
            });
            options.AddPolicy(GXTriggerGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXTriggerGroupPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.TriggerGroupManager] });
            });
            options.AddPolicy(GXTriggerGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXTriggerGroupPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.TriggerGroupManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXGatewayPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.Gateway, GXRoles.GatewayManager] });
            });
            options.AddPolicy(GXGatewayPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXGatewayPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.GatewayManager] });
            });
            options.AddPolicy(GXGatewayPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXGatewayPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.GatewayManager] });
            });
            options.AddPolicy(GXGatewayPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXGatewayPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.GatewayManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXGatewayGroupPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.GatewayManager] });
            });
            options.AddPolicy(GXGatewayGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXGatewayGroupPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.GatewayManager] });
            });
            options.AddPolicy(GXGatewayGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXGatewayGroupPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.GatewayManager] });
            });
            options.AddPolicy(GXGatewayGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXGatewayGroupPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.GatewayManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXGatewayLogPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.GatewayLogManager] });
            });
            options.AddPolicy(GXGatewayLogPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXGatewayLogPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.GatewayLogManager] });
            });
            options.AddPolicy(GXGatewayLogPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXGatewayLogPolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.GatewayLogManager] });
            });
            options.AddPolicy(GXGatewayLogPolicies.Close, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXGatewayLogPolicies.Close, issuer)
                { Roles = [GXRoles.Admin, GXRoles.GatewayLogManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.SubtotalManager] });
            });
            options.AddPolicy(GXSubtotalPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.SubtotalManager] });
            });
            options.AddPolicy(GXSubtotalPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.SubtotalManager] });
            });
            options.AddPolicy(GXSubtotalPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.SubtotalManager] });
            });
            options.AddPolicy(GXSubtotalPolicies.Calculate, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalPolicies.Calculate, issuer)
                { Roles = [GXRoles.Admin, GXRoles.SubtotalManager] });
            });
            options.AddPolicy(GXSubtotalPolicies.Clear, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalPolicies.Clear, issuer)
                { Roles = [GXRoles.Admin, GXRoles.SubtotalManager] });
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
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalGroupPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.SubtotalGroupManager] });
            });
            options.AddPolicy(GXSubtotalGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalGroupPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.SubtotalGroupManager] });
            });
            options.AddPolicy(GXSubtotalGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalGroupPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.SubtotalGroupManager] });
            });
            options.AddPolicy(GXSubtotalGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXSubtotalGroupPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.SubtotalGroupManager] });
            });
        }

        /// <summary>
        /// Add report requirements that are used for policy.
        /// </summary>
        private static void AddReportRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXReportPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXReportPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ReportManager] });
            });
            options.AddPolicy(GXReportPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXReportPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ReportManager] });
            });
            options.AddPolicy(GXReportPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXReportPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ReportManager] });
            });
            options.AddPolicy(GXReportPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXReportPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ReportManager] });
            });
            options.AddPolicy(GXReportPolicies.Send, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXReportPolicies.Send, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ReportManager] });
            });
        }

        /// <summary>
        /// Add report group requirements that are used for policy.
        /// </summary>
        private static void AddReportGroupRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXReportGroupPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXReportGroupPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ReportGroupManager] });
            });
            options.AddPolicy(GXReportGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXReportGroupPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ReportGroupManager] });
            });
            options.AddPolicy(GXReportGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXReportGroupPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ReportGroupManager] });
            });
            options.AddPolicy(GXReportGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXReportGroupPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.ReportGroupManager] });
            });
        }

        /// <summary>
        /// Add notification requirements that are used for policy.
        /// </summary>
        private static void AddNotificationRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXNotificationPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXNotificationPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.NotificationManager] });
            });
            options.AddPolicy(GXNotificationPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXNotificationPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.NotificationManager] });
            });
            options.AddPolicy(GXNotificationPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXNotificationPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.NotificationManager] });
            });
            options.AddPolicy(GXNotificationPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXNotificationPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.NotificationManager] });
            });
        }

        /// <summary>
        /// Add notification group requirements that are used for policy.
        /// </summary>
        private static void AddNotificationGroupRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXNotificationGroupPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXNotificationGroupPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.NotificationGroupManager] });
            });
            options.AddPolicy(GXNotificationGroupPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXNotificationGroupPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.NotificationGroupManager] });
            });
            options.AddPolicy(GXNotificationGroupPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXNotificationGroupPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.NotificationGroupManager] });
            });
            options.AddPolicy(GXNotificationGroupPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXNotificationGroupPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.NotificationGroupManager] });
            });
        }

        /// <summary>
        /// Add appearance requirements that are used for policy.
        /// </summary>
        private static void AddAppearanceRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXAppearancePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAppearancePolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.AppearanceManager] });
            });
            options.AddPolicy(GXAppearancePolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAppearancePolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.AppearanceManager] });
            });
            options.AddPolicy(GXAppearancePolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAppearancePolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.AppearanceManager] });
            });
        }

        /// <summary>
        /// Add Resource requirements that are used for policy.
        /// </summary>
        private static void AddIpAddressRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXIpAddressPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXIpAddressPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
            });
            options.AddPolicy(GXIpAddressPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXIpAddressPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
            });
            options.AddPolicy(GXIpAddressPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXIpAddressPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
            });
            options.AddPolicy(GXIpAddressPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXIpAddressPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.User] });
            });
        }

        /// <summary>
        /// Add LocalizedResource requirements that are used for policy.
        /// </summary>
        private static void AddLocalizedResourceRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXLocalizedResourcePolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXLocalizedResourcePolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.LocalizedResourceManager, GXRoles.LocalizedResource] });
            });
            options.AddPolicy(GXLocalizedResourcePolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXLocalizedResourcePolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.LocalizedResourceManager] });
            });
            options.AddPolicy(GXLocalizedResourcePolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXLocalizedResourcePolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.LocalizedResourceManager] });
            });
            options.AddPolicy(GXLocalizedResourcePolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXLocalizedResourcePolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.LocalizedResourceManager] });
            });
        }

        /// <summary>
        /// Add announcement requirements that are used for policy.
        /// </summary>
        private static void AddAnnouncementRequirements(AuthorizationOptions options, string issuer)
        {
            options.AddPolicy(GXAnnouncementPolicies.View, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAnnouncementPolicies.View, issuer)
                { Roles = [GXRoles.Admin, GXRoles.AnnouncementManager] });
            });
            options.AddPolicy(GXAnnouncementPolicies.Add, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAnnouncementPolicies.Add, issuer)
                { Roles = [GXRoles.Admin, GXRoles.AnnouncementManager] });
            });
            options.AddPolicy(GXAnnouncementPolicies.Edit, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAnnouncementPolicies.Edit, issuer)
                { Roles = [GXRoles.Admin, GXRoles.AnnouncementManager] });
            });
            options.AddPolicy(GXAnnouncementPolicies.Delete, policy =>
            {
                policy.RequireAuthenticatedUser();
                //Cookie and Bearer can both access this resource.
                policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, IdentityConstants.BearerScheme);
                policy.Requirements.Add(new ScopeRequirement(GXAnnouncementPolicies.Delete, issuer)
                { Roles = [GXRoles.Admin, GXRoles.AnnouncementManager] });
            });
        }
    }
}
