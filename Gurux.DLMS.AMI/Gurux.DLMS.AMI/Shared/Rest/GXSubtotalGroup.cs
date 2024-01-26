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
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.Subtotal;
using Gurux.DLMS.AMI.Shared.DTOs.User;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get subtotal group.
    /// </summary>
    public class GetSubtotalGroupResponse
    {
        /// <summary>
        /// Subtotal group information.
        /// </summary>
        [IncludeSwagger(typeof(GXSubtotal), nameof(GXSubtotal.Id),
                nameof(GXSubtotal.Name))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id),
                nameof(GXUserGroup.Name))]
        public GXSubtotalGroup? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get subtotal group list.
    /// </summary>
    [DataContract]
    public class ListSubtotalGroups : IGXRequest<ListSubtotalGroupsResponse>
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
        /// Amount of the subtotal groups to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter subtotal groups.
        /// </summary>
        [ExcludeSwagger(typeof(GXSubtotalGroup), nameof(GXSubtotalGroup.Subtotals),
            nameof(GXSubtotalGroup.UserGroups))]
        public GXSubtotalGroup? Filter
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
    /// Get subtotal groups response.
    /// </summary>
    [DataContract]
    public class ListSubtotalGroupsResponse
    {
        /// <summary>
        /// List of subtotal groups.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXSubtotalGroup), nameof(GXSubtotalGroup.Subtotals),
            nameof(GXSubtotalGroup.UserGroups))]
        [ExcludeSwagger(typeof(GXSubtotalGroup))]
        public GXSubtotalGroup[]? SubtotalGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the subtotal groups.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new subtotal group.
    /// </summary>
    [DataContract]
    public class AddSubtotalGroup : IGXRequest<AddSubtotalGroupResponse>
    {
        /// <summary>
        /// New subtotal group(s).
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXSubtotal), nameof(GXSubtotal.Id))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [ExcludeSwagger(typeof(GXSubtotalGroup),
            nameof(GXSubtotalGroup.CreationTime),
            nameof(GXSubtotalGroup.Updated))]
        public GXSubtotalGroup[]? SubtotalGroups
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new subtotal group response.
    /// </summary>
    [DataContract]
    public class AddSubtotalGroupResponse
    {
        /// <summary>
        /// New subtotal group IDs.
        /// </summary>
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove subtotal group.
    /// </summary>
    [DataContract]
    public class RemoveSubtotalGroup : IGXRequest<RemoveSubtotalGroupResponse>
    {
        /// <summary>
        /// Subtotal group Ids to remove.
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
    /// Remove subtotal group response.
    /// </summary>
    [DataContract]
    public class RemoveSubtotalGroupResponse
    {
    }
}
