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
    /// Get trigger group list.
    /// </summary>
    [DataContract]
    public class ListTriggerGroups : IGXRequest<ListTriggerGroupsResponse>
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
        /// Amount of the trigger groups to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter trigger groups.
        /// </summary>
        public GXTriggerGroup? Filter
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
    /// Get trigger groups response.
    /// </summary>
    [DataContract]
    public class ListTriggerGroupsResponse
    {
        /// <summary>
        /// List of trigger groups.
        /// </summary>
        [DataMember]
        public GXTriggerGroup[] TriggerGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the trigger groups.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new trigger group.
    /// </summary>
    [DataContract]
    public class AddTriggerGroup : IGXRequest<AddTriggerGroupResponse>
    {
        /// <summary>
        /// New trigger group(s).
        /// </summary>
        [DataMember]
        public GXTriggerGroup[] TriggerGroups
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new trigger group response.
    /// </summary>
    [DataContract]
    public class AddTriggerGroupResponse
    {
        /// <summary>
        /// New trigger groups.
        /// </summary>
        public GXTriggerGroup[] TriggerGroups
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove trigger group.
    /// </summary>
    [DataContract]
    public class RemoveTriggerGroup : IGXRequest<RemoveTriggerGroupResponse>
    {
        /// <summary>
        /// Trigger group Ids to remove.
        /// </summary>
        [DataMember]
        public Guid[] Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove trigger group response.
    /// </summary>
    [DataContract]
    public class RemoveTriggerGroupResponse
    {
    }
}
