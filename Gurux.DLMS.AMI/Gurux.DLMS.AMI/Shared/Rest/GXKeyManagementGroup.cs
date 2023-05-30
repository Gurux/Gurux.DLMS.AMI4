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
using Gurux.Common;
using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get key management group.
    /// </summary>
    public class GetKeyManagementGroupResponse
    {
        /// <summary>
        /// Key management group information.
        /// </summary>
        [IncludeSwagger(typeof(GXKeyManagement), nameof(GXKeyManagement.Id), nameof(GXKeyManagement.SystemTitle))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id),
                nameof(GXUserGroup.Name))]
        public GXKeyManagementGroup? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get key management group list.
    /// </summary>
    [DataContract]
    public class ListKeyManagementGroups : IGXRequest<ListKeyManagementGroupsResponse>
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
        /// Amount of the key management groups to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter key management groups.
        /// </summary>
        [ExcludeSwagger(typeof(GXKeyManagementGroup),
           nameof(GXKeyManagementGroup.KeyManagements),
           nameof(GXKeyManagementGroup.UserGroups))]
        public GXKeyManagementGroup? Filter
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
        public TargetType Select
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
    /// Get key management groups response.
    /// </summary>
    [DataContract]
    public class ListKeyManagementGroupsResponse
    {
        /// <summary>
        /// List of key management groups.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXKeyManagementGroup),
           nameof(GXKeyManagementGroup.KeyManagements),
           nameof(GXKeyManagementGroup.UserGroups))]
        public GXKeyManagementGroup[]? KeyManagementGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the key management groups.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new key management group.
    /// </summary>
    [DataContract]
    public class AddKeyManagementGroup : IGXRequest<AddKeyManagementGroupResponse>
    {
        /// <summary>
        /// New key management group(s).
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXKeyManagement), nameof(GXKeyManagement.Id))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        public GXKeyManagementGroup[]? KeyManagementGroups
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new key management group response.
    /// </summary>
    [DataContract]
    public class AddKeyManagementGroupResponse
    {
        /// <summary>
        /// New key management group IDs.
        /// </summary>
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove key management group.
    /// </summary>
    [DataContract]
    public class RemoveKeyManagementGroup : IGXRequest<RemoveKeyManagementGroupResponse>
    {
        /// <summary>
        /// KeyManagement group Ids to remove.
        /// </summary>
        [DataMember]
        public Guid[]? Ids
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
    /// Remove key management group response.
    /// </summary>
    [DataContract]
    public class RemoveKeyManagementGroupResponse
    {
    }
}
