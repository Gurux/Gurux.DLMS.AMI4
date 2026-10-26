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
using Gurux.Service.Orm.Common;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get block group.
    /// </summary>
    public class GetBlockGroupResponse
    {
        /// <summary>
        /// Block group information.
        /// </summary>
        [IncludeOpenApi(typeof(GXBlock), nameof(GXBlock.Id),
                nameof(GXBlock.Name))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id),
                nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        public GXBlockGroup? Item { get; set; }
    }

    /// <summary>
    /// Get block group list.
    /// </summary>
    [DataContract]
    public class ListBlockGroups : IGXRequest<ListBlockGroupsResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of block groups to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter block groups.
        /// </summary>
        [ExcludeOpenApi(typeof(GXBlockGroup), nameof(GXBlockGroup.Blocks),
            nameof(GXBlockGroup.UserGroups))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        public GXBlockGroup? Filter { get; set; }

        /// <summary>
        /// Admin user can access groups from all users.
        /// </summary>
        /// <remarks>
        /// If true, groups from all users are retrieved, not just current user.
        /// </remarks>
        public bool AllUsers { get; set; }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public IEnumerable<string>? Select { get; set; }

        /// <summary>
        /// Name of the property to sort by.
        /// </summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending { get; set; }

        /// <summary>
        /// Identifiers to include in the results.
        /// </summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>
    /// Get block groups response.
    /// </summary>
    [DataContract]
    public class ListBlockGroupsResponse
    {
        /// <summary>
        /// List of block groups.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXBlockGroup), nameof(GXBlockGroup.Blocks),
            nameof(GXBlockGroup.UserGroups))]
        public IEnumerable<GXBlockGroup>? BlockGroups { get; set; }

        /// <summary>
        /// Total number of matching block groups before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Add new block group.
    /// </summary>
    [DataContract]
    public class AddBlockGroup : IGXRequest<AddBlockGroupResponse>
    {
        /// <summary>
        /// New block group(s).
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXBlock), nameof(GXBlock.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [ExcludeOpenApi(typeof(GXBlockGroup), nameof(GXBlockGroup.CreationTime), nameof(GXBlockGroup.Updated))]
        public IEnumerable<GXBlockGroup>? BlockGroups { get; set; }
    }

    /// <summary>
    /// Add new block group response.
    /// </summary>
    [DataContract]
    public class AddBlockGroupResponse
    {
        /// <summary>
        /// New block group IDs.
        /// </summary>
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Remove block group.
    /// </summary>
    [DataContract]
    public class RemoveBlockGroup : IGXRequest<RemoveBlockGroupResponse>
    {
        /// <summary>
        /// Block group Ids to remove.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>
        /// Whether to permanently delete the items from the database.
        /// </summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [DataMember]
        [Required]
        public bool Delete { get; set; }
    }

    /// <summary>
    /// Remove block group response.
    /// </summary>
    [DataContract]
    public class RemoveBlockGroupResponse
    {
    }
}
