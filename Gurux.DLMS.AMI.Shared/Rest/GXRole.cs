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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.Service.Orm.Common;
using System.Runtime.Serialization;

using System.ComponentModel;

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
            nameof(GXRole.NormalizedName), nameof(GXRole.ConcurrencyStamp), nameof(GXRole.NotifyOnContentChange),
            nameof(GXRole.Description))]
        public GXRole? Filter { get; set; }

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
}
