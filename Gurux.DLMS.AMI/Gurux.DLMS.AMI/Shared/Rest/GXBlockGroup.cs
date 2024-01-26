﻿//
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
using Gurux.Common;
using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.User;

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
        [IncludeSwagger(typeof(GXBlock), nameof(GXBlock.Id),
                nameof(GXBlock.Name))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id),
                nameof(GXUserGroup.Name))]
        [ExcludeSwagger(typeof(GXBlockGroup), nameof(GXBlockGroup.Roles))]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public GXBlockGroup Item
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get block group list.
    /// </summary>
    [DataContract]
    public class ListBlockGroups : IGXRequest<ListBlockGroupsResponse>
    {
        /// <summary>
        /// Start index.
        /// </summary>
        public int Index
        {
            get;
            set;

        }

        /// <summary>
        /// Amount of the block groups to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter block groups.
        /// </summary>
        [ExcludeSwagger(typeof(GXBlockGroup), nameof(GXBlockGroup.Blocks),
            nameof(GXBlockGroup.UserGroups))]
        [ExcludeSwagger(typeof(GXBlockGroup), nameof(GXBlockGroup.Roles))]
        public GXBlockGroup? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access groups from all users.
        /// </summary>
        /// <remarks>
        /// If true, groups from all users are retreaved, not just current user. 
        /// </remarks>
        public bool AllUsers
        {
            get;
            set;
        }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public string[]? Select
        {
            get;
            set;
        }

        /// <summary>
        /// Order by name.
        /// </summary>
        /// <remarks>
        /// Default order by is used if this is not set.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy
        {
            get;
            set;
        }

        /// <summary>
        /// Are values shown as descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending
        {
            get;
            set;
        }

        /// <summary>
        /// Included Ids.
        /// </summary>
        /// <remarks>
        /// Included Ids can be used to get only part of large data.
        /// </remarks>
        public Guid[]? Included
        {
            get;
            set;
        }

        /// <summary>
        /// Excluded Ids.
        /// </summary>
        /// <remarks>
        /// Excluded Ids can be used to filter data.
        /// </remarks>
        public Guid[]? Exclude
        {
            get;
            set;
        }
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
        [ExcludeSwagger(typeof(GXBlockGroup), nameof(GXBlockGroup.Blocks),
            nameof(GXBlockGroup.UserGroups))]
        [ExcludeSwagger(typeof(GXBlockGroup), nameof(GXBlockGroup.Roles))]
        public GXBlockGroup[]? BlockGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the block groups.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
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
        [IncludeSwagger(typeof(GXBlock), nameof(GXBlock.Id))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [ExcludeSwagger(typeof(GXBlockGroup), nameof(GXBlockGroup.Roles), nameof(GXBlockGroup.CreationTime), nameof(GXBlockGroup.Updated))]
        public GXBlockGroup[] BlockGroups
        {
            get;
            set;
        }
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
        public Guid[]? Ids
        {
            get;
            set;
        }
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
        public Guid[] Ids
        {
            get;
            set;
        }

        /// <summary>
        /// Items are removed from the database.
        /// </summary>
        /// <remarks>
        /// If false, the Removed date is set for the items, but items are kept on the database.
        /// </remarks>
        [DataMember]
        [Required]
        public bool Delete
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove block group response.
    /// </summary>
    [DataContract]
    public class RemoveBlockGroupResponse
    {
    }
}
