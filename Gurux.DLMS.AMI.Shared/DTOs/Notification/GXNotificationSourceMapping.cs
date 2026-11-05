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
using System.Runtime.Serialization;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Notification;

/// <summary>Maps a notification subscription to a shared event source.</summary>
[Description("Maps a notification subscription to a shared event source.")]
[DataContract, Serializable]
[IndexCollection(true, nameof(NotificationId), nameof(SourceId))]
public class GXNotificationSourceMapping : IUnique<Guid>
{
    /// <summary>Mapping identifier used by the ORM.</summary>
    [Description("Mapping identifier used by the ORM.")]
    [DataMember, IsRequired, System.ComponentModel.DataAnnotations.Key]
    public Guid Id { get; set; }

    /// <summary>Notification subscription identifier.</summary>
    [Description("Notification subscription identifier.")]
    [DataMember, IsRequired]
    [ForeignKey(typeof(GXNotification), OnDelete = ForeignKeyDelete.Cascade)]
    public Guid NotificationId { get; set; }

    /// <summary>Shared source identifier.</summary>
    [Description("Shared source identifier.")]
    [DataMember, IsRequired]
    [ForeignKey(typeof(GXNotificationSource), OnDelete = ForeignKeyDelete.Cascade)]
    public int SourceId { get; set; }
}
