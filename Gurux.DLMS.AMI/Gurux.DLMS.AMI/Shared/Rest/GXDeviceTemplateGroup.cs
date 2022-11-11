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
// This file is a part of Gurux DeviceTemplate Framework.
//
// Gurux DeviceTemplate Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux DeviceTemplate Framework is distributed in the hope that it will be useful,
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
    /// Get device template group list.
    /// </summary>
    [DataContract]
    public class ListDeviceTemplateGroups : IGXRequest<ListDeviceTemplateGroupsResponse>
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
        /// Amount of the device template  groups to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter device template groups.
        /// </summary>
        public GXDeviceTemplateGroup? Filter
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
    /// Get device template  groups response.
    /// </summary>
    [DataContract]
    public class ListDeviceTemplateGroupsResponse
    {
        /// <summary>
        /// List of device template groups.
        /// </summary>
        [DataMember]
        public GXDeviceTemplateGroup[] DeviceTemplateGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the device template groups.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new device template group.
    /// </summary>
    [DataContract]
    public class AddDeviceTemplateGroup : IGXRequest<AddDeviceTemplateGroupResponse>
    {
        /// <summary>
        /// New device template group(s).
        /// </summary>
        [DataMember]
        public GXDeviceTemplateGroup[] DeviceTemplateGroups
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add new device template group response.
    /// </summary>
    [DataContract]
    public class AddDeviceTemplateGroupResponse
    {
        /// <summary>
        /// New device groups.
        /// </summary>
        public GXDeviceTemplateGroup[] DeviceTemplateGroups
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove device template group.
    /// </summary>
    [DataContract]
    public class RemoveDeviceTemplateGroup : IGXRequest<RemoveDeviceTemplateGroupResponse>
    {
        /// <summary>
        /// Device template group Id(s) to remove.
        /// </summary>
        [DataMember]
        public Guid[] Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove device template group response.
    /// </summary>
    [DataContract]
    public class RemoveDeviceTemplateGroupResponse
    {
    }
}
