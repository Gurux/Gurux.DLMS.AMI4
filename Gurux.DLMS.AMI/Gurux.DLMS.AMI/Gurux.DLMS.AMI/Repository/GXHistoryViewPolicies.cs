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
namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Explicit resource permissions used when authorizing owned deletion history for missing targets.</summary>
internal static class GXHistoryViewPolicies
{
    internal static readonly IReadOnlyDictionary<string, string> Values = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["GXAgentGroup"] = GXAgentGroupPolicies.View,
        ["GXAgent"] = GXAgentPolicies.View,
        ["GXAttribute"] = GXAttributePolicies.View,
        ["GXAttributeTemplate"] = GXAttributeTemplatePolicies.View,
        ["GXBlockGroup"] = GXBlockGroupPolicies.View,
        ["GXBlock"] = GXBlockPolicies.View,
        ["GXComponentViewGroup"] = GXComponentViewGroupPolicies.View,
        ["GXComponentView"] = GXComponentViewPolicies.View,
        ["GXDeviceGroup"] = GXDeviceGroupPolicies.View,
        ["GXDevice"] = GXDevicePolicies.View,
        ["GXDeviceTemplateGroup"] = GXDeviceTemplateGroupPolicies.View,
        ["GXDeviceTemplate"] = GXDeviceTemplatePolicies.View,
        ["GXFavorite"] = GXFavoritePolicies.View,
        ["GXGatewayGroup"] = GXGatewayGroupPolicies.View,
        ["GXGateway"] = GXGatewayPolicies.View,
        ["GXKeyManagementGroup"] = GXKeyManagementGroupPolicies.View,
        ["GXKeyManagement"] = GXKeyManagementPolicies.View,
        ["GXLocalizedResource"] = GXLocalizedResourcePolicies.View,
        ["GXLogType"] = GXLogTypePolicies.View,
        ["GXManufacturerGroup"] = GXManufacturerGroupPolicies.View,
        ["GXManufacturer"] = GXManufacturerPolicies.View,
        ["GXMenuGroup"] = GXMenuGroupPolicies.View,
        ["GXMenu"] = GXMenuPolicies.View,
        ["GXModuleGroup"] = GXModuleGroupPolicies.View,
        ["GXNotification"] = GXNotificationPolicies.View,
        ["GXNotificationType"] = GXNotificationTypePolicies.View,
        ["GXObject"] = GXObjectPolicies.View,
        ["GXObjectTemplate"] = GXObjectTemplatePolicies.View,
        ["GXPolicy"] = GXPolicyPolicies.View,
        ["GXRole"] = GXRolePolicies.View,
        ["GXScheduleGroup"] = GXScheduleGroupPolicies.View,
        ["GXSchedule"] = GXSchedulePolicies.View,
        ["GXScriptGroup"] = GXScriptGroupPolicies.View,
        ["GXScript"] = GXScriptPolicies.View,
        ["GXTriggerGroup"] = GXTriggerGroupPolicies.View,
        ["GXTrigger"] = GXTriggerPolicies.View,
        ["GXUserGroup"] = GXUserGroupPolicies.View,
        ["GXUser"] = GXUserPolicies.View,
        ["GXViewGroup"] = GXViewGroupPolicies.View,
        ["GXView"] = GXViewPolicies.View,
        ["GXWorkflowGroup"] = GXWorkflowGroupPolicies.View,
        ["GXWorkflow"] = GXWorkflowPolicies.View,
        ["GXModule"] = GXModulePolicies.View,
        ["GXLanguage"] = GXLanguagePolicies.View,
        ["GXConfiguration"] = GXConfigurationPolicies.View,
        ["GXAppearance"] = GXAppearancePolicies.View,
        ["GXAgentProperty"] = GXAgentPropertiesPolicies.View,
        ["GXUserProperty"] = GXUserPolicies.View,
    };
}
