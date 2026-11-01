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
