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

using Gurux.Service.Orm.Common;
using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested manufacturer group information.</summary>
    [Description("Response containing the requested manufacturer group information.")]
    public class GetManufacturerGroupResponse
    {
        /// <summary>
        /// Manufacturer group information.
        /// </summary>
        [Description("Manufacturer group information.")]
        [IncludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.Id),
                nameof(GXManufacturer.Name))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id),
                nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        public GXManufacturerGroup? Item { get; set; }
    }

    /// <summary>Requests a list of manufacturer groups.</summary>
    [Description("Requests a list of manufacturer groups.")]
    [DataContract]
    public class ListManufacturerGroups : IGXRequest<ListManufacturerGroupsResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of manufacturer groups to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of manufacturer groups to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter manufacturer groups.
        /// </summary>
        [Description("Optional criteria used to filter manufacturer groups.")]
        [ExcludeOpenApi(typeof(GXManufacturerGroup), nameof(GXManufacturerGroup.Manufacturers),
            nameof(GXManufacturerGroup.UserGroups))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXManufacturerGroup), nameof(GXManufacturerGroup.Description), nameof(GXManufacturerGroup.ConcurrencyStamp))]
        public GXManufacturerGroup? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public stored date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }

        /// <summary>Whether administrators can access groups owned by other users.</summary>
        /// <remarks>
        /// If true, groups from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Whether administrators can access groups owned by other users.")]
        public bool AllUsers { get; set; }

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

    /// <summary>Response containing the requested manufacturer groups.</summary>
    [Description("Response containing the requested manufacturer groups.")]
    [DataContract]
    public class ListManufacturerGroupsResponse
    {
        /// <summary>
        /// List of manufacturer groups.
        /// </summary>
        [Description("List of manufacturer groups.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXManufacturerGroup), nameof(GXManufacturerGroup.Manufacturers),
            nameof(GXManufacturerGroup.UserGroups))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        public IEnumerable<GXManufacturerGroup>? ManufacturerGroups { get; set; }

        /// <summary>
        /// Total number of matching manufacturer groups before paging.
        /// </summary>
        [Description("Total number of matching manufacturer groups before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests the addition of manufacturer groups.</summary>
    [Description("Requests the addition of manufacturer groups.")]
    [DataContract]
    public class AddManufacturerGroup : IGXRequest<AddManufacturerGroupResponse>
    {
        /// <summary>New manufacturer groups.</summary>
        [Description("New manufacturer groups.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.Id))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXManufacturerGroup),
            nameof(GXManufacturerGroup.CreationTime),
            nameof(GXManufacturerGroup.Updated))]
        public IEnumerable<GXManufacturerGroup> ManufacturerGroups { get; set; } = default!;
    }

    /// <summary>Response to adding manufacturer groups.</summary>
    [Description("Response to adding manufacturer groups.")]
    [DataContract]
    public class AddManufacturerGroupResponse
    {
        /// <summary>Identifiers of the newly added manufacturer group records.</summary>
        [Description("Identifiers of the newly added manufacturer group records.")]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests deletion of the selected manufacturer groups.</summary>
    [Description("Requests deletion of the selected manufacturer groups.")]
    [DataContract]
    public class RemoveManufacturerGroup : IGXRequest<RemoveManufacturerGroupResponse>
    {
        /// <summary>Identifiers of the manufacturer group records to delete.</summary>
        [Description("Identifiers of the manufacturer group records to delete.")]
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

    /// <summary>Response to deleting the selected manufacturer groups.</summary>
    [Description("Response to deleting the selected manufacturer groups.")]
    [DataContract]
    public class RemoveManufacturerGroupResponse
    {
    }
}
