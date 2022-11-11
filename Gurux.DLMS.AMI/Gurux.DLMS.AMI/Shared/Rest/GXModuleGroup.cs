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
    /// Get module group list.
    /// </summary>
    [DataContract]
    public class ListModuleGroups : IGXRequest<ListModuleGroupsResponse>
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
        /// Amount of the module groups to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter module groups.
        /// </summary>
        public GXModuleGroup? Filter
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
    /// Get module groups response.
    /// </summary>
    [DataContract]
    public class ListModuleGroupsResponse
    {
        /// <summary>
        /// List of module groups.
        /// </summary>
        [DataMember]
        public GXModuleGroup[] ModuleGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the module groups.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new module group.
    /// </summary>
    [DataContract]
    public class AddModuleGroup : IGXRequest<AddModuleGroupResponse>
    {
        /// <summary>
        /// New module group(s).
        /// </summary>
        [DataMember]
        public GXModuleGroup[] ModuleGroups
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new module group response.
    /// </summary>
    [DataContract]
    public class AddModuleGroupResponse
    {
        /// <summary>
        /// New module groups.
        /// </summary>
        public GXModuleGroup[] ModuleGroups
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove module group.
    /// </summary>
    [DataContract]
    public class RemoveModuleGroup : IGXRequest<RemoveModuleGroupResponse>
    {
        /// <summary>
        /// Module group Ids to remove.
        /// </summary>
        [DataMember]
        public Guid[] Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove module group response.
    /// </summary>
    [DataContract]
    public class RemoveModuleGroupResponse
    {
    }
}
