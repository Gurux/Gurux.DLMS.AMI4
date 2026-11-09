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

using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Gurux.Service.Orm.Common;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;

namespace Gurux.DLMS.AMI.Client.Shared.KeyManagement;

/// <summary>
/// Confirmed installed key identity. Distinct from cryptographic activation.
/// </summary>
[DataContract]
public sealed class GXKeyDeployment : IUnique<Guid>
{
    /// <summary>Gets or sets the unique identifier of this deployment confirmation.</summary>
    [Key, DataMember] public Guid Id { get; set; }
    /// <summary>Gets or sets the device on which the key was confirmed as installed.</summary>
    [DataMember, Index(false)] public Guid DeviceId { get; set; }
    /// <summary>Gets or sets the installed key identity.</summary>
    [DataMember, Index(false)] public Guid KeyId { get; set; }
    /// <summary>Gets or sets the DLMS role occupied by the installed key.</summary>
    [DataMember] public KeyManagementType KeyType { get; set; }
    /// <summary>Gets or sets the time at which installation was confirmed.</summary>
    [DataMember] public DateTimeOffset ConfirmedAt { get; set; }
}

