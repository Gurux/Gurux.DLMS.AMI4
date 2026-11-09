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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Maps routed AMI editor models to the canonical command-policy catalog.</summary>
internal static class GXEditorPolicies
{
    private static readonly IReadOnlyDictionary<string, (string Add, string Edit, string Delete)> Policies =
        new Dictionary<string, (string, string, string)>(StringComparer.Ordinal)
        {
            ["GXAgent"] = (GXAgentPolicies.Add, GXAgentPolicies.Edit, GXAgentPolicies.Delete),
            ["GXAgentGroup"] = (GXAgentGroupPolicies.Add, GXAgentGroupPolicies.Edit, GXAgentGroupPolicies.Delete),
            ["GXGateway"] = (GXGatewayPolicies.Add, GXGatewayPolicies.Edit, GXGatewayPolicies.Delete),
            ["GXGatewayGroup"] = (GXGatewayGroupPolicies.Add, GXGatewayGroupPolicies.Edit, GXGatewayGroupPolicies.Delete),
            ["GXManufacturer"] = (GXManufacturerPolicies.Add, GXManufacturerPolicies.Edit, GXManufacturerPolicies.Delete),
            ["GXWorkflow"] = (GXWorkflowPolicies.Add, GXWorkflowPolicies.Edit, GXWorkflowPolicies.Delete),
            ["GXWorkflowGroup"] = (GXWorkflowGroupPolicies.Add, GXWorkflowGroupPolicies.Edit, GXWorkflowGroupPolicies.Delete),
            ["GXTrigger"] = (GXTriggerPolicies.Add, GXTriggerPolicies.Edit, GXTriggerPolicies.Delete),
            ["GXTriggerGroup"] = (GXTriggerGroupPolicies.Add, GXTriggerGroupPolicies.Edit, GXTriggerGroupPolicies.Delete),
            ["GXScript"] = (GXScriptPolicies.Add, GXScriptPolicies.Edit, GXScriptPolicies.Delete),
            ["GXScriptGroup"] = (GXScriptGroupPolicies.Add, GXScriptGroupPolicies.Edit, GXScriptGroupPolicies.Delete),
            ["GXDevice"] = (GXDevicePolicies.Add, GXDevicePolicies.Edit, GXDevicePolicies.Delete),
            ["GXDeviceGroup"] = (GXDeviceGroupPolicies.Add, GXDeviceGroupPolicies.Edit, GXDeviceGroupPolicies.Delete),
            ["GXDeviceTemplate"] = (GXDeviceTemplatePolicies.Add, GXDeviceTemplatePolicies.Edit, GXDeviceTemplatePolicies.Delete),
            ["GXDeviceTemplateGroup"] = (GXDeviceTemplateGroupPolicies.Add, GXDeviceTemplateGroupPolicies.Edit, GXDeviceTemplateGroupPolicies.Delete),
            ["GXView"] = (GXViewPolicies.Add, GXViewPolicies.Edit, GXViewPolicies.Delete),
            ["GXViewGroup"] = (GXViewGroupPolicies.Add, GXViewGroupPolicies.Edit, GXViewGroupPolicies.Delete),
            ["GXSchedule"] = (GXSchedulePolicies.Add, GXSchedulePolicies.Edit, GXSchedulePolicies.Delete),
            ["GXScheduleGroup"] = (GXScheduleGroupPolicies.Add, GXScheduleGroupPolicies.Edit, GXScheduleGroupPolicies.Delete),
            ["GXMenu"] = (GXMenuPolicies.Add, GXMenuPolicies.Edit, GXMenuPolicies.Delete),
            ["GXMenuGroup"] = (GXMenuGroupPolicies.Add, GXMenuGroupPolicies.Edit, GXMenuGroupPolicies.Delete),
            ["GXUser"] = (GXUserPolicies.Add, GXUserPolicies.Edit, GXUserPolicies.Delete),
            ["GXUserGroup"] = (GXUserGroupPolicies.Add, GXUserGroupPolicies.Edit, GXUserGroupPolicies.Delete),
            ["GXNotification"] = (GXNotificationPolicies.Add, GXNotificationPolicies.Edit, GXNotificationPolicies.Delete),
            ["GXAppearance"] = (GXAppearancePolicies.Add, GXAppearancePolicies.Edit, GXAppearancePolicies.Delete),
            ["GXComponentView"] = (GXComponentViewPolicies.Add, GXComponentViewPolicies.Edit, GXComponentViewPolicies.Delete),
            ["GXComponentViewGroup"] = (GXComponentViewGroupPolicies.Add, GXComponentViewGroupPolicies.Edit, GXComponentViewGroupPolicies.Delete),
            ["GXBlock"] = (GXBlockPolicies.Add, GXBlockPolicies.Edit, GXBlockPolicies.Delete),
            ["GXBlockGroup"] = (GXBlockGroupPolicies.Add, GXBlockGroupPolicies.Edit, GXBlockGroupPolicies.Delete),
            ["GXKeyManagement"] = (GXKeyManagementPolicies.Add, GXKeyManagementPolicies.Edit, GXKeyManagementPolicies.Delete),
            ["GXKeyManagementGroup"] = (GXKeyManagementGroupPolicies.Add, GXKeyManagementGroupPolicies.Edit, GXKeyManagementGroupPolicies.Delete),
            ["GXModule"] = (GXModulePolicies.Add, GXModulePolicies.Edit, GXModulePolicies.Delete),
            ["GXModuleGroup"] = (GXModuleGroupPolicies.Add, GXModuleGroupPolicies.Edit, GXModuleGroupPolicies.Delete),
            ["GXRole"] = (GXRolePolicies.Add, GXRolePolicies.Edit, GXRolePolicies.Delete),
            // The languages editor writes Localization/Update, not Language/Update.
            ["GXLanguage"] = (GXLocalizationPolicies.Add, GXLocalizationPolicies.Edit, GXLocalizationPolicies.Delete)
        };

    internal static string? For(object model, CrudAction action)
    {
        var type = model.GetType();
        // The configuration editors keep their edit context on a typed collection.
        if (model is IEnumerable<GXRole>) {
            type = typeof(GXRole);
        }
        else if (model is IEnumerable<GXLanguage>) {
            type = typeof(GXLanguage);
        }
        // Custom editor models retain their existing unprotected command behavior.
        if (type.Assembly != typeof(GXUser).Assembly ||
            type.Namespace?.StartsWith("Gurux.DLMS.AMI.Shared.DTOs", StringComparison.Ordinal) != true ||
            !Policies.TryGetValue(type.Name, out var policies)) {
            return null;
        }
        return action switch
        {
            CrudAction.Create => policies.Add,
            CrudAction.Update => policies.Edit,
            CrudAction.Delete => policies.Delete,
            _ => null
        };
    }
}
