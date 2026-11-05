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

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Gurux.Service.Orm.Common;
using Gurux.DLMS.AMI.Shared.DTOs.Module;

namespace Gurux.DLMS.AMI.Shared.DTOs.Notification;

/// <summary>A registered event source with a globally unique name and a server-wide delivery setting, owned by the framework or a module.</summary>
[Description("A registered event source with a globally unique name and a server-wide delivery setting, owned by the framework or a module.")]
[DataContract, Serializable]
public class GXNotificationSource : IUnique<int>
{
    /// <summary>Source row identifier used by the ORM.</summary>
    [Description("Source row identifier used by the ORM.")]
    [DataMember, IsRequired, Key, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Gets or sets whether the server permits notification delivery for this source.</summary>
    /// <remarks>Does not grant recipient permissions or enable user subscriptions or delivery channels. Persisted sources must have an explicit value; null can represent an unspecified list filter.</remarks>
    [Description("Gets or sets whether the server permits notification delivery for this source.")]
    [DataMember, IsRequired]
    [DefaultValue(true)]
    public bool? Enabled { get; set; }

    /// <summary>Owning module identifier; null for framework sources.</summary>
    [Description("Owning module identifier; null for framework sources.")]
    [DataMember, StringLength(64)]
    [ForeignKey(typeof(GXModule), OnDelete = Service.Orm.Common.Enums.ForeignKeyDelete.None)]
    public string? ModuleId { get; set; }

    /// <summary>Gets or sets the globally unique event name or resource prefix, independent of the permission catalog.</summary>
    [Description("Gets or sets the globally unique event name or resource prefix, independent of the permission catalog.")]
    [DataMember, IsRequired, Required, StringLength(128), Index(Unique = true)]
    public string Source { get; set; } = string.Empty;
}
