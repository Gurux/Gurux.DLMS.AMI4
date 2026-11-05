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
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.Service.Orm.Common;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested role information.</summary>
    [Description("Response containing the requested role information.")]
    public class GetRoleResponse
    {
        /// <summary>
        /// Role information.
        /// </summary>
        [Description("Role information.")]
        [IncludeOpenApi(typeof(GXModule),
                nameof(GXModule.Id), nameof(GXModule.Name))]
        public GXRole? Item { get; set; }
    }

    /// <summary>Requests changes to the specified roles.</summary>
    [Description("Requests changes to the specified roles.")]
    [DataContract]
    public class UpdateRole : IGXRequest<UpdateRoleResponse>
    {
        /// <summary>
        /// Inserted or updated roles.
        /// </summary>
        [Description("Inserted or updated roles.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        public IEnumerable<GXRole> Roles { get; set; } = default!;
    }

    /// <summary>Response to updating the specified roles.</summary>
    [Description("Response to updating the specified roles.")]
    [DataContract]
    public class UpdateRoleResponse
    {
        /// <summary>Identifiers of the added or updated role records.</summary>
        [Description("Identifiers of the added or updated role records.")]
        [DataMember]
        public IEnumerable<string>? Ids { get; set; }
    }

    /// <summary>Requests a list of roles.</summary>
    [Description("Requests a list of roles.")]
    [DataContract]
    public class ListRoles : IGXRequest<ListRolesResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of roles to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of roles to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter roles.
        /// </summary>
        [Description("Optional criteria used to filter roles.")]
        [IncludeOpenApi(typeof(GXModule),
            nameof(GXModule.Id),
            nameof(GXModule.Name))]
        [ExcludeOpenApi(typeof(GXRole),
            nameof(GXRole.NormalizedName), nameof(GXRole.ConcurrencyStamp),
            nameof(GXRole.Description))]
        public GXRole? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public stored date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }

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

    /// <summary>Response containing the requested roles.</summary>
    [Description("Response containing the requested roles.")]
    [DataContract]
    public class ListRolesResponse
    {
        /// <summary>
        /// List of roles.
        /// </summary>
        [Description("List of roles.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXModule),
            nameof(GXModule.Id),
            nameof(GXModule.Name))]
        [IncludeOpenApi(typeof(GXPermission),
            nameof(GXPermission.Id),
            nameof(GXPermission.Name))]
        public IEnumerable<GXRole>? Roles { get; set; }

        /// <summary>
        /// Total number of matching roles before paging.
        /// </summary>
        [Description("Total number of matching roles before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests deletion of the selected role records.</summary>
    [Description("Requests deletion of the selected role records.")]
    [DataContract]
    public class RoleDelete : IGXRequest<RoleDeleteResponse>
    {
        /// <summary>
        /// Removed roles.
        /// </summary>
        [Description("Removed roles.")]
        [DataMember]
        public IEnumerable<string> Ids { get; set; } = default!;
    }

    /// <summary>Response to deleting role.</summary>
    [Description("Response to deleting role.")]
    [DataContract]
    public class RoleDeleteResponse
    {
    }

    /// <summary>
    /// Regenerate role.
    /// </summary>
    [Description("Regenerate role.")]
    [DataContract]
    public class RegenerateRole : IGXRequest<RegenerateRoleResponse>
    {
        /// <summary>Existing identifiers to reset to built-in defaults. Null or empty adds only missing defaults and preserves existing records.</summary>
        [Description("Existing identifiers to reset to built-in defaults. Null or empty adds only missing defaults and preserves existing records.")]
        [DataMember]
        public IEnumerable<string>? Ids { get; set; }
    }

    /// <summary>
    /// Regenerate roles response.
    /// </summary>
    [Description("Regenerate roles response.")]
    [DataContract]
    public class RegenerateRoleResponse
    {
    }
}
