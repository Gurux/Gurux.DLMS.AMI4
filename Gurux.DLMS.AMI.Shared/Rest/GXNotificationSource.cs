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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested notification source information.</summary>
    [Description("Response containing the requested notification source information.")]
    public class GetNotificationSourceResponse
    {
        /// <summary>
        /// The requested notification source.
        /// </summary>        
        [Description("The requested notification source.")]
        public GXNotificationSource? Item { get; set; }
    }

    /// <summary>Requests a list of notification sources.</summary>
    [Description("Requests a list of notification sources.")]
    [DataContract]
    public class ListNotificationSources : IGXRequest<ListNotificationSourcesResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of notification sources to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of notification sources to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter notification sources.
        /// </summary>
        [Description("Optional criteria used to filter notification sources.")]
        public GXNotificationSource? Filter { get; set; }

        /// <summary>Additional related information to include in the response. Reserved for future use.</summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        [Description("Additional related information to include in the response. Reserved for future use.")]
        public IEnumerable<string>? Select { get; set; }

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
        public IEnumerable<int>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        [Description("Identifiers to exclude from the results.")]
        public IEnumerable<int>? Exclude { get; set; }
    }

    /// <summary>Response containing the requested notification sources.</summary>
    [Description("Response containing the requested notification sources.")]
    [DataContract]
    public class ListNotificationSourcesResponse
    {
        /// <summary>
        /// List of notification source items.
        /// </summary>
        [Description("List of notification source items.")]
        [DataMember]
        public IEnumerable<GXNotificationSource>? NotificationSources { get; set; }

        /// <summary>
        /// Total number of matching notification sources before paging.
        /// </summary>
        [Description("Total number of matching notification sources before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests enabling or disabling server-wide delivery for selected registered notification sources.</summary>
    [Description("Requests enabling or disabling server-wide delivery for selected registered notification sources.")]
    [DataContract]
    public class SetNotificationSourcesEnabled : IGXRequest<SetNotificationSourcesEnabledResponse>
    {
        /// <summary>Gets or sets the identifiers of the registered notification sources to update.</summary>
        [Description("Identifiers of the registered notification sources to update.")]
        [DataMember(IsRequired = true)]
        [Required]
        public IEnumerable<int> Ids { get; set; } = [];

        /// <summary>Gets or sets whether server-wide delivery is permitted for the selected sources.</summary>
        /// <remarks>Changes only the delivery setting; source names and producer ownership are preserved.</remarks>
        [Description("Whether server-wide delivery is permitted for the selected sources.")]
        [DataMember(IsRequired = true)]
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public bool Enabled { get; set; }
    }

    /// <summary>Response indicating completion of the notification source delivery settings update.</summary>
    [Description("Response indicating completion of the notification source delivery settings update.")]
    [DataContract]
    public class SetNotificationSourcesEnabledResponse
    {
    }
}
