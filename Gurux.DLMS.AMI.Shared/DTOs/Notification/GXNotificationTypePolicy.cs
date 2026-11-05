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

using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Notification;

/// <summary>Assigns an alternative named authorization policy to a notification type.</summary>
[System.ComponentModel.Description("Assigns an alternative named authorization policy to a notification type.")]
[DataContract, Serializable]
[IndexCollection(true, nameof(NotificationTypeId), nameof(PolicyId))]
public class GXNotificationTypePolicy
{
    /// <summary>Gets or sets the notification type associated with the policy.</summary>
    [System.ComponentModel.Description("Gets or sets the notification type associated with the policy.")]
    [DataMember, IsRequired]
    [ForeignKey(typeof(GXNotificationType), OnDelete = ForeignKeyDelete.Cascade)]
    public Guid NotificationTypeId { get; set; }

    /// <summary>Gets or sets the identifier of the assigned authorization policy.</summary>
    [System.ComponentModel.Description("Gets or sets the identifier of the assigned authorization policy.")]
    [DataMember, IsRequired]
    [ForeignKey(typeof(GXPolicy))]
    public Guid PolicyId { get; set; }
}
