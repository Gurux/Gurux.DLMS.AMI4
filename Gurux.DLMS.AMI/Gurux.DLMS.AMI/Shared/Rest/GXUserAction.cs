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
// This file is a part of Gurux User Framework.
//
// Gurux User Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux User Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using System.Runtime.Serialization;
using System.ComponentModel;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Adds a new User action.
    /// </summary>
    [DataContract]
    public class AddUserAction
    {
        /// <summary>
        /// Added User actions.
        /// </summary>
        [DataMember]
        public GXUserAction[] Actions
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add User actions response.
    /// </summary>
    [DataContract]
    public class AddUserActionResponse
    {

    }

    /// <summary>
    /// Get list from the User activities.
    /// </summary>
    [DataContract]
    public class ListUserAction
    {
        /// <summary>
        /// Start index.
        /// </summary>
        [DataMember]
        public int Index
        {
            get;
            set;
        }

        /// <summary>
        /// Maximum User action count to return.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter user actions.
        /// </summary>
        public GXUserAction? Filter
        {
            get;
            set;
        }


        /// <summary>
        /// Admin user can access actions from all users.
        /// </summary>
        /// <remarks>
        /// If true, actions from all users are retreaved, not just current user. 
        /// </remarks>
        public bool AllUsers
        {
            get;
            set;
        }
    }

    /// <summary>
    /// List User action response.
    /// </summary>
    [DataContract]
    public class ListUserActionResponse
    {
        /// <summary>
        /// List of User actions.
        /// </summary>
        [DataMember]
        public GXUserAction[] Actions
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the action items.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Clear User action. All activities are removed from the given User.
    /// </summary>
    [DataContract]
    public class ClearUserAction
    {
        /// <summary>
        /// List of user Ids whoes activities are cleared.
        /// </summary>
        /// <remarks>
        /// Activities for all users are cleared if it's null or empty.
        /// </remarks>
        [DataMember]
        public string[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Clear User action response.
    /// </summary>
    [DataContract]
    public class ClearUserActionResponse
    {
    }
}
