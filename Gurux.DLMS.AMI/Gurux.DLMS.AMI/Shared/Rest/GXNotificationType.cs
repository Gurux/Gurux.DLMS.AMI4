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
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.Service.Orm.Common;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get notification type.
    /// </summary>
    public class GetNotificationTypeResponse
    {
        /// <summary>
        /// Notification type information.
        /// </summary>        
        public GXNotificationType? Item { get; set; }
    }

    /// <summary>
    /// Get list from notification types.
    /// </summary>
    [DataContract]
    public class ListNotificationType : IGXRequest<ListNotificationTypeResponse>
    {
        /// <summary>
        /// Start index.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Amount of the notification types to retrieve.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Filter can be used to filter notification types.
        /// </summary>
        public GXNotificationType? Filter { get; set; }

        /// <summary>
        /// Order by.
        /// </summary>
        /// <remarks>
        /// Default order by is used if this is not set.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Are values shown as descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending { get; set; }

        /// <summary>
        /// Included Ids.
        /// </summary>
        /// <remarks>
        /// Included Ids can be used to get only part of large data.
        /// </remarks>
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Excluded Ids.
        /// </summary>
        /// <remarks>
        /// Excluded Ids can be used to filter data.
        /// </remarks>
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>
    /// Notification type items reply.
    /// </summary>
    [DataContract]
    public class ListNotificationTypeResponse
    {
        /// <summary>
        /// List of notification types.
        /// </summary>
        [DataMember]
        public IEnumerable<GXNotificationType>? NotificationTypes { get; set; }

        /// <summary>
        /// Total count of the notification types.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Update notification types.
    /// </summary>
    [DataContract]
    public class UpdateNotificationType : IGXRequest<UpdateNotificationTypeResponse>
    {
        /// <summary>
        /// Notification types to update.
        /// </summary>
        [DataMember]
        public IEnumerable<GXNotificationType> NotificationTypes { get; set; } = default!;
    }

    /// <summary>
    /// Update notification types reply.
    /// </summary>
    [DataContract]
    public class UpdateNotificationTypeResponse
    {
        /// <summary>
        /// New notification type identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Remove notification types.
    /// </summary>
    [DataContract]
    public class RemoveNotificationType : IGXRequest<RemoveNotificationTypeResponse>
    {
        /// <summary>
        /// Removed IP address identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>
        /// Items are removed from the database.
        /// </summary>
        /// <remarks>
        /// If false, the Removed date is set for the items, but items are kept on the database.
        /// </remarks>
        [DataMember]
        [Required]
        public bool Delete { get; set; }
    }

    /// <summary>
    /// Reply from Remove notification types.
    /// </summary>
    [DataContract]
    public class RemoveNotificationTypeResponse
    {
    }
}
