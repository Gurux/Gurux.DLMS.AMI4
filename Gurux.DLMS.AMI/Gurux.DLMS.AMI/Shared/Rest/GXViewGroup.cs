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
using Gurux.DLMS.AMI.Shared.DTOs.View;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.Service.Orm.Common;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get view group.
    /// </summary>
    public class GetViewGroupResponse
    {
        /// <summary>
        /// View group information.
        /// </summary>
        [IncludeOpenApi(typeof(GXView), nameof(GXView.Id),
                nameof(GXView.Name))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id),
                nameof(GXUserGroup.Name))]
        [ExcludeOpenApi(typeof(GXViewGroup))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        public GXViewGroup? Item { get; set; }
    }

    /// <summary>
    /// Get view group list.
    /// </summary>
    [DataContract]
    public class ListViewGroups : IGXRequest<ListViewGroupsResponse>
    {
        /// <summary>
        /// Start index.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Amount of the view groups to retrieve.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Filter can be used to filter view groups.
        /// </summary>
        [ExcludeOpenApi(typeof(GXViewGroup), nameof(GXViewGroup.Views),
            nameof(GXViewGroup.UserGroups))]
        [IncludeOpenApi(typeof(GXViewGroup), nameof(GXViewGroup.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public GXViewGroup? Filter { get; set; }

        /// <summary>
        /// Admin user can access groups from all users.
        /// </summary>
        /// <remarks>
        /// If true, groups from all users are retreaved, not just current user. 
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
        /// Order by name.
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
    /// Get view groups response.
    /// </summary>
    [DataContract]
    public class ListViewGroupsResponse
    {
        /// <summary>
        /// List of view groups.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXViewGroup), nameof(GXViewGroup.Views),
            nameof(GXViewGroup.UserGroups))]
        [ExcludeOpenApi(typeof(GXViewGroup))]
        public IEnumerable<GXViewGroup>? ViewGroups { get; set; }

        /// <summary>
        /// Total count of the view groups.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Add new view group.
    /// </summary>
    [DataContract]
    public class AddViewGroup : IGXRequest<AddViewGroupResponse>
    {
        /// <summary>
        /// New view group(s).
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXView), nameof(GXView.Id))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [ExcludeOpenApi(typeof(GXViewGroup), nameof(GXViewGroup.CreationTime), nameof(GXViewGroup.Updated))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        public IEnumerable<GXViewGroup> ViewGroups { get; set; } = default!;
    }

    /// <summary>
    /// Add new view group response.
    /// </summary>
    [DataContract]
    public class AddViewGroupResponse
    {
        /// <summary>
        /// New view group IDs.
        /// </summary>
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Remove view group.
    /// </summary>
    [DataContract]
    public class RemoveViewGroup : IGXRequest<RemoveViewGroupResponse>
    {
        /// <summary>
        /// View group Ids to remove.
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
    /// Remove view group response.
    /// </summary>
    [DataContract]
    public class RemoveViewGroupResponse
    {
    }
}
