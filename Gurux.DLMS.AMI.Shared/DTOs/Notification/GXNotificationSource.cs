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
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Notification;

/// <summary>A selected event source belonging to a notification subscription.</summary>
[Description("A selected event source belonging to a notification subscription.")]
[DataContract, Serializable]
[IndexCollection(true, nameof(NotificationId), nameof(Source))]
public class GXNotificationSource : IUnique<Guid>
{
    /// <summary>Source row identifier used by the ORM.</summary>
    [Description("Source row identifier used by the ORM.")]
    [DataMember, IsRequired, Key]
    public Guid Id { get; set; }

    /// <summary>Parent notification identifier.</summary>
    [Description("Parent notification identifier.")]
    [DataMember, IsRequired]
    [ForeignKey(typeof(GXNotification), OnDelete = ForeignKeyDelete.Cascade)]
    public Guid NotificationId { get; set; }

    /// <summary>Exact event type or resource prefix, independent of the permission catalog.</summary>
    [Description("Exact event type or resource prefix, independent of the permission catalog.")]
    [DataMember, IsRequired, Required, StringLength(128)]
    public string Source { get; set; } = string.Empty;
}
