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
