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
    /// Adds a new device action.
    /// </summary>
    [DataContract]
    public class AddDeviceAction : IGXRequest<AddDeviceActionResponse>
    {
        /// <summary>
        /// Added Device action items.
        /// </summary>
        [DataMember]
        public GXDeviceAction[] Actions
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add device action response.
    /// </summary>
    [DataContract]
    public class AddDeviceActionResponse
    {

    }

    /// <summary>
    /// Get list from the device actions.
    /// </summary>
    [DataContract]
    public class ListDeviceAction : IGXRequest<ListDeviceActionResponse>
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
        /// Maximum device action count to return.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter device actions.
        /// </summary>
        public GXDeviceAction? Filter
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
    /// List device action response.
    /// </summary>
    [DataContract]
    public class ListDeviceActionResponse
    {
        /// <summary>
        /// List of device actions.
        /// </summary>
        [DataMember]
        public GXDeviceAction[] Actions
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
    /// Clear device action. All actions are removed from the given device.
    /// </summary>
    [DataContract]
    public class ClearDeviceAction : IGXRequest<ClearDeviceActionResponse>
    {
        /// <summary>
        /// Device identifiers where device actions are removed.
        /// </summary>
        [DataMember]
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Clear device action response.
    /// </summary>
    [DataContract]
    public class ClearDeviceActionResponse
    {
    }
}
