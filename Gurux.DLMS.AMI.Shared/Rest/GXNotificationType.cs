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

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested notification type information.</summary>
    [Description("Response containing the requested notification type information.")]
    public class GetNotificationTypeResponse
    {
        /// <summary>
        /// Notification type information.
        /// </summary>        
        [Description("Notification type information.")]
        public GXNotificationType? Item { get; set; }
    }

    /// <summary>Requests a list of notification types.</summary>
    [Description("Requests a list of notification types.")]
    [DataContract]
    public class ListNotificationType : IGXRequest<ListNotificationTypeResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of notification types to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of notification types to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter notification types.
        /// </summary>
        [Description("Optional criteria used to filter notification types.")]
        [ExcludeOpenApi(typeof(GXNotificationType), nameof(GXNotificationType.ConcurrencyStamp))]
        public GXNotificationType? Filter { get; set; }

        /// <summary>Name of the property to sort by. If omitted, the endpoint uses its default ordering.</summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        [Description("Name of the property to sort by. If omitted, the endpoint uses its default ordering.")]
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        [Description("Whether to sort results in descending order.")]
        public bool Descending { get; set; }

        /// <summary>Identifiers to include in the results. A nonempty collection restricts results to these identifiers.</summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        [Description("Identifiers to include in the results. A nonempty collection restricts results to these identifiers.")]
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        [Description("Identifiers to exclude from the results.")]
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>Response containing the requested notification types.</summary>
    [Description("Response containing the requested notification types.")]
    [DataContract]
    public class ListNotificationTypeResponse
    {
        /// <summary>
        /// List of notification types.
        /// </summary>
        [Description("List of notification types.")]
        [DataMember]
        public IEnumerable<GXNotificationType>? NotificationTypes { get; set; }

        /// <summary>
        /// Total number of matching notification types before paging.
        /// </summary>
        [Description("Total number of matching notification types before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests changes to the specified notification types.</summary>
    [Description("Requests changes to the specified notification types.")]
    [DataContract]
    public class UpdateNotificationType : IGXRequest<UpdateNotificationTypeResponse>
    {
        /// <summary>
        /// Notification types to update.
        /// </summary>
        [Description("Notification types to update.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXNotificationType), nameof(GXNotificationType.CreationTime), nameof(GXNotificationType.Updated))]
        public IEnumerable<GXNotificationType> NotificationTypes { get; set; } = default!;
    }

    /// <summary>Response to updating the specified notification types.</summary>
    [Description("Response to updating the specified notification types.")]
    [DataContract]
    public class UpdateNotificationTypeResponse
    {
        /// <summary>Identifiers of the added or updated notification type records.</summary>
        [Description("Identifiers of the added or updated notification type records.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests deletion of the selected notification types.</summary>
    [Description("Requests deletion of the selected notification types.")]
    [DataContract]
    public class RemoveNotificationType : IGXRequest<RemoveNotificationTypeResponse>
    {
        /// <summary>Identifiers of the notification type records to delete.</summary>
        [Description("Identifiers of the notification type records to delete.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.</summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [Description("Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.")]
        [DataMember]
        [Required]
        public bool Delete { get; set; }
    }

    /// <summary>Response to deleting the selected notification types.</summary>
    [Description("Response to deleting the selected notification types.")]
    [DataContract]
    public class RemoveNotificationTypeResponse
    {
    }
}
