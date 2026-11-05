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

using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Enums;
using System.Runtime.Serialization;

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Requests a list of user stamps.</summary>
    [Description("Requests a list of user stamps.")]
    [DataContract]
    public class ListUserStamps
    {
        /// <summary>
        /// Optional criteria used to filter user stamps, for example by date.
        /// </summary>
        [Description("Optional criteria used to filter user stamps, for example by date.")]
        [IncludeOpenApi(typeof(GXUser),
                nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        [ExcludeOpenApi(typeof(GXUserStamp),
            nameof(GXUserStamp.ConcurrencyStamp), nameof(GXUserStamp.Errors), nameof(GXUserStamp.Warnings),
            nameof(GXUserStamp.Informational), nameof(GXUserStamp.Verboses))]
        public GXUserStamp? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public stored date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }

        /// <summary>
        /// Admin user can access user stamps from all users.
        /// </summary>
        /// <remarks>
        /// If true, user stamps from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access user stamps from all users.")]
        public bool AllUsers { get; set; }

        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of user stamps to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of user stamps to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

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

    /// <summary>Response containing the requested user stamps.</summary>
    [Description("Response containing the requested user stamps.")]
    [DataContract]
    public class ListUserStampsResponse
    {
        /// <summary>
        /// User stamp records returned by the request.
        /// </summary>
        [Description("User stamp records returned by the request.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXUser),
                nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        public IEnumerable<GXUserStamp>? Stamps { get; set; }

        /// <summary>
        /// Total number of matching user stamp records.
        /// </summary>
        [Description("Total number of matching user stamp records.")]
        public long Count { get; set; }
    }

    /// <summary>Requests changes to the specified user stamps.</summary>
    [Description("Requests changes to the specified user stamps.")]
    [DataContract]
    public class UpdateUserStamp
    {
        /// <summary>
        /// New user stamp.
        /// </summary>
        [Description("New user stamp.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXUserStamp),
                nameof(GXUserStamp.Creator))]
        [ExcludeOpenApi(typeof(GXUserStamp), nameof(GXUserStamp.CreationTime), nameof(GXUserStamp.Updated))]
        public IEnumerable<GXUserStamp>? Stamps { get; set; }
    }

    /// <summary>Response to updating the specified user stamps.</summary>
    [Description("Response to updating the specified user stamps.")]
    [DataContract]
    public class UpdateUserStampResponse
    {
        /// <summary>
        /// New user stamp.
        /// </summary>
        [Description("New user stamp.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXUserStamp),
                nameof(GXUserStamp.Creator))]
        public IEnumerable<GXUserStamp>? Stamps { get; set; }
    }

    /// <summary>Requests deletion of the selected user stamps.</summary>
    [Description("Requests deletion of the selected user stamps.")]
    [DataContract]
    public class DeleteUserStamps
    {
        /// <summary>Identifiers of the user stamps records to delete.</summary>
        [Description("Identifiers of the user stamps records to delete.")]
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>Response to deleting the selected user stamps.</summary>
    [Description("Response to deleting the selected user stamps.")]
    [DataContract]
    public class DeleteUserStampsResponse
    {
    }
}
