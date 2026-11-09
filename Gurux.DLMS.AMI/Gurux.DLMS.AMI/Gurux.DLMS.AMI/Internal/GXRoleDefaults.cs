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

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;

namespace Gurux.DLMS.AMI.Server.Internal
{
    /// <summary>Canonical metadata and explicit permission grants for system roles.</summary>
    internal static class GXRoleDefaults
    {
        private static readonly IReadOnlyList<string> EmptyPermissions = Array.Empty<string>();

        private static readonly IReadOnlyList<string> UserPermissions = Array.AsReadOnly(new[]
        {
            // Manage the four resources available to ordinary users.
            GXDevicePolicies.View, GXDevicePolicies.Add, GXDevicePolicies.Edit, GXDevicePolicies.Delete,
            GXObjectPolicies.View, GXObjectPolicies.Add, GXObjectPolicies.Edit, GXObjectPolicies.Delete, GXObjectPolicies.Clear,
            GXAttributePolicies.View, GXAttributePolicies.Add, GXAttributePolicies.Edit, GXAttributePolicies.Delete, GXAttributePolicies.Clear,
            GXAgentPolicies.View, GXAgentPolicies.Add, GXAgentPolicies.Edit, GXAgentPolicies.Delete,

            // Issue and manage personal agent tokens without assigning the runtime Agent role.
            GXTokenPolicies.Add, GXTokenPolicies.View, GXTokenPolicies.Delete, GXUserPolicies.View,

            // Device passwords and security keys are saved through key management.
            GXKeyManagementPolicies.View, GXKeyManagementPolicies.Add, GXKeyManagementPolicies.Edit,
            GXKeyManagementGroupPolicies.View, GXKeyManagementLogPolicies.Add,

            // Read supporting records used by those resource screens.
            GXDeviceGroupPolicies.View,
            GXManufacturerPolicies.View,
            GXDeviceTemplatePolicies.View, GXDeviceTemplateGroupPolicies.View,
            GXObjectTemplatePolicies.View, GXAttributeTemplatePolicies.View,
            GXDevicePropertiesPolicies.View, GXAgentPropertiesPolicies.View,
            GXAgentGroupPolicies.View,
            GXDeviceLogPolicies.View, GXAgentLogPolicies.View,
            GXDeviceTracePolicies.View, GXDeviceActionPolicies.View, GXValuePolicies.View,

            // Read personal data requested automatically by the application shell.
            GXUserPropertiesPolicies.View,

            // Display the UI without granting customization or regeneration rights.
            GXMenuPolicies.View, GXMenuGroupPolicies.View,
            GXViewPolicies.View, GXViewGroupPolicies.View,
            GXBlockPolicies.View, GXBlockGroupPolicies.View,
            GXAppearancePolicies.View,
            GXComponentViewPolicies.View, GXComponentViewGroupPolicies.View,
            GXLocalizedResourcePolicies.View, GXLanguagePolicies.View
        }.Distinct(StringComparer.Ordinal).ToArray());

        private static readonly IReadOnlyList<string> AgentPermissions = Array.AsReadOnly(new[]
        {
            GXAgentPolicies.View, GXAgentPolicies.Add, GXAgentPolicies.Edit,
            GXAgentGroupPolicies.View,
            GXTaskPolicies.View, GXTaskPolicies.Edit,
            GXObjectPolicies.Edit,
            GXDevicePolicies.View, GXDevicePolicies.Add, GXDevicePolicies.Edit,
            GXDeviceLogPolicies.Add,
            GXAgentLogPolicies.Add,
            GXDeviceTracePolicies.Add,
            GXDeviceActionPolicies.Add,
            GXValuePolicies.Add,
            GXAttributePolicies.Edit,
            GXScriptPolicies.View,
            GXScriptLogPolicies.Add,
            GXKeyManagementPolicies.View,
            GXGatewayPolicies.View, GXGatewayPolicies.Add, GXGatewayPolicies.Edit,
            GXGatewayGroupPolicies.View,
            GXGatewayLogPolicies.Add
        }.Distinct(StringComparer.Ordinal).ToArray());

        /// <summary>Create a fresh system role with its canonical metadata.</summary>
        internal static GXRole Create(string name)
        {
            string canonicalName = CanonicalName(name);
            return new GXRole
            {
                Name = canonicalName,
                NormalizedName = canonicalName.ToUpperInvariant(),
                SystemDefined = true,
                Default = canonicalName == GXRoles.Authenticated,
                Description = null
            };
        }

        /// <summary>Get the explicit grants; administrator access is supplied by policy role checks.</summary>
        internal static IReadOnlyList<string> PermissionNames(string roleName)
        {
            return CanonicalName(roleName) switch
            {
                GXRoles.Authenticated => UserPermissions.Concat(AgentPermissions).Distinct(StringComparer.Ordinal).ToArray(),
                GXRoles.Agent => AgentPermissions,
                _ => EmptyPermissions
            };
        }

        private static string CanonicalName(string name)
        {
            foreach (string systemRole in GXRoles.SystemRoles)
            {
                if (string.Equals(name, systemRole, StringComparison.OrdinalIgnoreCase))
                {
                    return systemRole;
                }
            }
            throw new ArgumentException("Unknown system role.", nameof(name));
        }
    }
}
