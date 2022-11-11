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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Add or Update role information.
    /// </summary>
    [DataContract]
    public class UpdateRole : IGXRequest<UpdateRoleResponse>
    {
        /// <summary>
        /// Inserted or updated roles.
        /// </summary>
        [DataMember]
        public GXRole[] Roles
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update role response.
    /// </summary>
    [DataContract]
    public class UpdateRoleResponse
    {
        /// <summary>
        /// New role identifier(s).
        /// </summary>
        [DataMember]
        public string[] Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get available roles.
    /// </summary>
    [DataContract]
    public class ListRoles : IGXRequest<ListRolesResponse>
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
        /// Amount of the roles to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter roles.
        /// </summary>
        public GXRole? Filter
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Returns roles.
    /// </summary>
    [DataContract]
    public class ListRolesResponse
    {
        /// <summary>
        /// List of roles.
        /// </summary>
        [DataMember]
        public GXRole[] Roles
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the roles.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete role.
    /// </summary>
    [DataContract]
    public class RoleDelete : IGXRequest<RoleDeleteResponse>
    {
        /// <summary>
        /// Removed roles.
        /// </summary>
        [DataMember]
        public string[] Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete roles response.
    /// </summary>
    [DataContract]
    public class RoleDeleteResponse
    {
    }
}
