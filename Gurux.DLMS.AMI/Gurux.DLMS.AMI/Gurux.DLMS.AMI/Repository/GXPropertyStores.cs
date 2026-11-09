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

using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Typed owner mappings for the common property persistence service.</summary>
internal static class GXPropertyStores
{
    internal static readonly GXPropertyStore<GXWorkflowProperty, GXWorkflow, Guid> Workflow = new(p => p.Workflow);
    internal static readonly GXPropertyStore<GXDeviceProperty, GXDevice, Guid> Device = new(p => p.Device);
    internal static readonly GXPropertyStore<GXDeviceGroupProperty, GXDeviceGroup, Guid> DeviceGroup = new(p => p.DeviceGroup);
    internal static readonly GXPropertyStore<GXObjectProperty, GXObject, Guid> Object = new(p => p.Object);
    internal static readonly GXPropertyStore<GXAttributeProperty, GXAttribute, Guid> Attribute = new(p => p.Attribute);
    internal static readonly GXPropertyStore<GXAgentProperty, GXAgent, Guid> Agent = new(p => p.Agent);
    internal static readonly GXPropertyStore<GXUserProperty, GXUser, string?> User = new(p => p.User);
    internal static readonly GXPropertyStore<GXKeyManagementProperty, GXKeyManagement, Guid> KeyManagement = new(p => p.KeyManagement);
}
