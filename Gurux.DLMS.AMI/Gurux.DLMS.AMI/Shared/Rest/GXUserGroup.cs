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

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get user group list.
    /// </summary>
    [DataContract]
    public class ListUserGroups : IGXRequest<ListUserGroupsResponse>
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
        /// Amount of the user groups to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter user groups.
        /// </summary>
        public GXUserGroup? Filter
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

    }

    /// <summary>
    /// Get user groups response.
    /// </summary>
    [DataContract]
    public class ListUserGroupsResponse
    {
        /// <summary>
        /// List of user groups.
        /// </summary>
        [DataMember]
        public GXUserGroup[] UserGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the user groups.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new user group.
    /// </summary>
    [DataContract]
    public class AddUserGroup : IGXRequest<AddUserGroupResponse>
    {
        /// <summary>
        /// New user group(s).
        /// </summary>
        [DataMember]
        public GXUserGroup[] UserGroups
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new user group response.
    /// </summary>
    [DataContract]
    public class AddUserGroupResponse
    {
        /// <summary>
        /// New user groups.
        /// </summary>
        public GXUserGroup[] UserGroups
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove user group.
    /// </summary>
    [DataContract]
    public class RemoveUserGroup : IGXRequest<RemoveUserGroupResponse>
    {
        /// <summary>
        /// User group Ids to remove.
        /// </summary>
        [DataMember]
        public Guid[] Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove user group response.
    /// </summary>
    [DataContract]
    public class RemoveUserGroupResponse
    {
    }
}
