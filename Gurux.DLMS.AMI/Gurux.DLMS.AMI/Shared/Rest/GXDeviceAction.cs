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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get device action.
    /// </summary>
    public class GetDeviceActionResponse
    {
        /// <summary>
        /// Device information.
        /// </summary>        
        [IncludeSwagger(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXUserGroup.Name))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        [ExcludeSwagger(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device))]
        [ExcludeSwagger(typeof(GXObject), nameof(GXObject.Device))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeSwagger(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id))]
        [IncludeSwagger(typeof(GXObject), nameof(GXObject.Id))]
        [ExcludeSwagger(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device), nameof(GXDeviceParameter.Module))]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public GXDeviceAction Item
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            get;
            set;
        }
    }
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
        [IncludeSwagger(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXUserGroup.Name))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        [ExcludeSwagger(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device))]
        [ExcludeSwagger(typeof(GXObject), nameof(GXObject.Device))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeSwagger(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id))]
        [IncludeSwagger(typeof(GXObject), nameof(GXObject.Id))]
        [ExcludeSwagger(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device), nameof(GXDeviceParameter.Module))]
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
        [IncludeSwagger(typeof(GXDeviceAction), nameof(GXDeviceAction.Device))]
        [IncludeSwagger(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXUserGroup.Name))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        [ExcludeSwagger(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device))]
        [ExcludeSwagger(typeof(GXObject), nameof(GXObject.Device))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeSwagger(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id))]
        [IncludeSwagger(typeof(GXObject), nameof(GXObject.Id))]
        [ExcludeSwagger(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device), nameof(GXDeviceParameter.Module))]
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
    /// List device action response.
    /// </summary>
    [DataContract]
    public class ListDeviceActionResponse
    {
        /// <summary>
        /// List of device actions.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXUserGroup.Name))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        [ExcludeSwagger(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device))]
        [ExcludeSwagger(typeof(GXObject), nameof(GXObject.Device))]
        [ExcludeSwagger(typeof(GXDevice), nameof(GXDevice.Traces),
            nameof(GXDevice.Objects), nameof(GXDevice.Actions)
            , nameof(GXDevice.Keys)
            , nameof(GXDevice.Errors), nameof(GXDevice.Tasks))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeSwagger(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id))]
        [IncludeSwagger(typeof(GXObject), nameof(GXObject.Id))]
        [ExcludeSwagger(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device), nameof(GXDeviceParameter.Module))]
        public GXDeviceAction[]? Actions
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
    /// Clear device action response.
    /// </summary>
    [DataContract]
    public class ClearDeviceActionResponse
    {
    }
}
