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

namespace Gurux.DLMS.AMI.Module.Enums;

/// <summary>
/// Host location where a module view is displayed.
/// </summary>
public enum AmiViewArea
{
    /// <summary>The module's configuration editor.</summary>
    Configuration,
    /// <summary>The module's standalone extension view.</summary>
    Extension,
    /// <summary>The editor for settings of a scheduled module execution.</summary>
    ScheduleSettings,
    /// <summary>The details view for a schedule.</summary>
    Schedule,
    /// <summary>The details view for a user.</summary>
    User,
    /// <summary>The host configuration page.</summary>
    Config,
    /// <summary>The details view for a device.</summary>
    Device,
    /// <summary>The settings view for a device.</summary>
    DeviceSettings,
    /// <summary>The details view for a device group.</summary>
    DeviceGroup,
    /// <summary>The settings view for a device group.</summary>
    DeviceGroupSettings,
    /// <summary>The details view for a user group.</summary>
    UserGroup,
    /// <summary>The settings view for a user.</summary>
    UserSettings,
    /// <summary>The settings view for a user group.</summary>
    UserGroupSettings,
    /// <summary>The details view for a COSEM object.</summary>
    Object,
    /// <summary>The settings view for a COSEM object.</summary>
    ObjectSettings,
    /// <summary>The details view for an object attribute.</summary>
    Attribute,
    /// <summary>The settings view for an object attribute.</summary>
    AttributeSettings,
    /// <summary>The details view for a parameter.</summary>
    Parameter,
    /// <summary>The settings view for a parameter.</summary>
    ParameterSettings,
    /// <summary>The log listing for a selected log type; a view key can identify the type.</summary>
    Log,
    /// <summary>Additional views in the host's logs navigation.</summary>
    Logs,
    /// <summary>Additional views in the notification editor, scoped to its delivery module.</summary>
    Notification,
    /// <summary>The details view for an agent. Appended to preserve existing serialized area values.</summary>
    Agent,
    /// <summary>Application-wide appended views, such as module notices.</summary>
    Application,
    /// <summary>The details view for a device template.</summary>
    DeviceTemplate,
    /// <summary>The details view for a device template group.</summary>
    DeviceTemplateGroup,
    /// <summary>The details view for an agent group.</summary>
    AgentGroup,
    /// <summary>The details view for a gateway.</summary>
    Gateway,
    /// <summary>The details view for a gateway group.</summary>
    GatewayGroup,
    /// <summary>The details view for a manufacturer.</summary>
    Manufacturer,
    /// <summary>The details view for a workflow.</summary>
    Workflow,
    /// <summary>The details view for a script.</summary>
    Script
}
