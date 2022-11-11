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
using System.ComponentModel;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get user list.
    /// </summary>
    [DataContract]
    public class ListUsers : IGXRequest<ListUsersResponse>
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
        /// Amount of the users to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter users.
        /// </summary>
        public GXUser? Filter
        {
            get;
            set;
        }


        /// <summary>
        /// Admin user can access all users.
        /// </summary>
        /// <remarks>
        /// If true, all users are retreaved, not just current user. 
        /// </remarks>
        public bool AllUsers
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get users response.
    /// </summary>
    [DataContract]
    [Description("Get users response.")]
    public class ListUsersResponse
    {
        /// <summary>
        /// List of users.
        /// </summary>
        [DataMember]
        [Description("List of users.")]
        public GXUser[] Users
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the users.
        /// </summary>
        [DataMember]
        [Description("Total count of the users.")]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add or update users.
    /// </summary>
    [DataContract]
    public class AddUser : IGXRequest<AddUserResponse>
    {
        /// <summary>
        /// Users to add or update.
        /// </summary>
        [DataMember]
        public GXUser[] Users
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add or update users response.
    /// </summary>
    [DataContract]
    public class AddUserResponse
    {
        /// <summary>
        /// New users.
        /// </summary>
        public GXUser[] Users
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove user.
    /// </summary>
    [DataContract]
    [Description("Remove user.")]
    public class RemoveUser : IGXRequest<RemoveUserResponse>
    {
        /// <summary>
        /// User Ids to remove.
        /// </summary>
        [Description("User Ids to remove.")]
        [DataMember]
        public string[] Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove user response.
    /// </summary>
    [DataContract]
    [Description("Remove user response.")]
    public class RemoveUserResponse
    {
    }
}
