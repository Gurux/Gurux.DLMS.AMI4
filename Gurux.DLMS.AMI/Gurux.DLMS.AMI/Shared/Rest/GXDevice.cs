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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get device.
    /// </summary>
    public class GetDeviceResponse
    {
        /// <summary>
        /// Device information.
        /// </summary>        
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXUserGroup.Name))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        [ExcludeSwagger(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device))]
        [ExcludeSwagger(typeof(GXObject), nameof(GXObject.Device))]
        [ExcludeSwagger(typeof(GXDeviceAction), nameof(GXDeviceAction.Device))]
        [ExcludeSwagger(typeof(GXDevice), nameof(GXDevice.Traces),
            nameof(GXDevice.Objects), nameof(GXDevice.Actions)
            , nameof(GXDevice.Keys)
            , nameof(GXDevice.Errors), nameof(GXDevice.Tasks))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeSwagger(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id))]
        [IncludeSwagger(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeSwagger(typeof(GXDeviceAction), nameof(GXDeviceAction.Id))]
        [ExcludeSwagger(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device), nameof(GXDeviceParameter.Module))]
        [ExcludeSwagger(typeof(GXKeyManagement), nameof(GXKeyManagement.Device))]
        [ExcludeSwagger(typeof(GXKeyManagementKey), nameof(GXKeyManagementKey.KeyManagement))]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public GXDevice Item
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add or Update device information. Device is added if ID is zero.
    /// </summary>
    [DataContract]
    public class UpdateDevice : IGXRequest<UpdateDeviceResponse>
    {
        /// <summary>
        /// Inserted or updated devices.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [ExcludeSwagger(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device))]
        [ExcludeSwagger(typeof(GXDevice), nameof(GXDevice.Creator),
            nameof(GXDevice.Objects),
            nameof(GXDevice.Keys),
            nameof(GXDevice.Actions), nameof(GXDevice.Traces),
            nameof(GXDevice.Errors), nameof(GXDevice.Tasks))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeSwagger(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id))]
        [IncludeSwagger(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeSwagger(typeof(GXDeviceAction), nameof(GXDeviceAction.Id))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [ExcludeSwagger(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device), nameof(GXDeviceParameter.Module))]
        [IncludeSwagger(typeof(GXKeyManagement), nameof(GXKeyManagement.Id), nameof(GXKeyManagement.Name))]
        [ExcludeSwagger(typeof(GXKeyManagement), nameof(GXKeyManagement.Device))]
        [ExcludeSwagger(typeof(GXKeyManagementKey), nameof(GXKeyManagementKey.KeyManagement))]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public GXDevice[] Devices
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            get;
            set;
        }

        /// <summary>
        /// Device objects are created when they are read from the meter. This improves device creation speed.
        /// </summary>
        public bool LateBinding
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update device response.
    /// </summary>
    [DataContract]
    [Description("Update device response.")]
    public class UpdateDeviceResponse
    {
        /// <summary>
        /// New device identifier(s).
        /// </summary>
        [DataMember]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Guid[] Ids
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get available devices.
    /// </summary>
    [DataContract]
    public class ListDevices : IGXRequest<ListDevicesResponse>
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
        /// Amount of the devices to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter devices.
        /// </summary>
        [ExcludeSwagger(typeof(GXDevice), nameof(GXDevice.DeviceGroups),
                nameof(GXDevice.Creator), nameof(GXDevice.Parameters),
            nameof(GXDevice.Objects), nameof(GXDevice.Actions), nameof(GXDevice.Errors),
            nameof(GXDevice.Tasks), nameof(GXDevice.Traces)
            , nameof(GXDevice.ProfilePicture)
            , nameof(GXDevice.Settings)
            , nameof(GXDevice.Keys)
            , nameof(GXDevice.MediaSettings))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [ExcludeSwagger(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeSwagger(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id))]
        [IncludeSwagger(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeSwagger(typeof(GXDeviceAction), nameof(GXDeviceAction.Id))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [ExcludeSwagger(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device), nameof(GXDeviceParameter.Module))]
        public GXDevice? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access devices from all users.
        /// </summary>
        /// <remarks>
        /// If true, devices from all users are retreaved, not just current user. 
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
    /// Returns devices.
    /// </summary>
    [DataContract]
    [Description("Returns devices.")]
    public class ListDevicesResponse
    {
        /// <summary>
        /// List of devices.
        /// </summary>
        [DataMember]
        [Description("List of devices.")]
        [ExcludeSwagger(typeof(GXDevice), nameof(GXDevice.DeviceGroups)
            , nameof(GXDevice.Parameters), nameof(GXDevice.Objects)
            , nameof(GXDevice.Actions), nameof(GXDevice.Errors),
            nameof(GXDevice.Tasks), nameof(GXDevice.Keys), nameof(GXDevice.Traces))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [ExcludeSwagger(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeSwagger(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeSwagger(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id))]
        [IncludeSwagger(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeSwagger(typeof(GXDeviceAction), nameof(GXDeviceAction.Id))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [ExcludeSwagger(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device), nameof(GXDeviceParameter.Module))]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public GXDevice[] Devices
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the devices.
        /// </summary>
        [DataMember]
        [Description("Total count of the devices.")]
        public int Count
        {
            get;
            set;
        }
    }


    /// <summary>
    /// Remove Device.
    /// </summary>
    [DataContract]
    public class RemoveDevice : IGXRequest<RemoveDeviceResponse>
    {
        /// <summary>
        /// Removed devices.
        /// </summary>
        [DataMember]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Guid[] Ids
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
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
    /// Remove device response.
    /// </summary>
    [DataContract]
    public class RemoveDeviceResponse
    {
    }
}
