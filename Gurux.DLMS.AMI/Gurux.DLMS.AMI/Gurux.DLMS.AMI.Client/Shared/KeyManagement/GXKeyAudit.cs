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

namespace Gurux.DLMS.AMI.Client.Shared.KeyManagement;

/// <summary>
/// Persistent identity/lifecycle evidence without any secret bytes.
/// </summary>
[DataContract]
public sealed class GXKeyAudit : IUnique<Guid>
{
    /// <summary>Gets or sets the unique identifier of this audit record.</summary>
    [Key, DataMember] public Guid Id { get; set; }
    /// <summary>Gets or sets the key identity affected by the operation.</summary>
    [DataMember, Index(false)] public Guid KeyId { get; set; }
    /// <summary>Gets or sets the acting user identifier, or null when no user is recorded.</summary>
    [DataMember, StringLength(512)] public string? UserId { get; set; }
    /// <summary>Gets or sets the recorded operation name, or null when unspecified.</summary>
    [DataMember, StringLength(64)] public string? Operation { get; set; }
    /// <summary>Gets or sets the timestamp at which the operation was recorded.</summary>
    [DataMember] public DateTimeOffset Time { get; set; }
}

