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
using Gurux.Service.Orm.Common;
using System.Runtime.Serialization;
using System.ComponentModel;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.User;

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
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        [ExcludeOpenApi(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device))]
        [ExcludeOpenApi(typeof(GXObject), nameof(GXObject.Device))]
        [ExcludeOpenApi(typeof(GXDeviceAction), nameof(GXDeviceAction.Device))]
        [ExcludeOpenApi(typeof(GXDevice), nameof(GXDevice.Traces),
            nameof(GXDevice.Objects), nameof(GXDevice.Actions)
            , nameof(GXDevice.Keys)
            , nameof(GXDevice.Errors), nameof(GXDevice.Tasks))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id))]
        [IncludeOpenApi(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeOpenApi(typeof(GXGateway), nameof(GXGateway.Id))]
        [IncludeOpenApi(typeof(GXDeviceAction), nameof(GXDeviceAction.Id))]
        [ExcludeOpenApi(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device), nameof(GXDeviceParameter.Module))]
        [ExcludeOpenApi(typeof(GXKeyManagement), nameof(GXKeyManagement.Device))]
        [ExcludeOpenApi(typeof(GXKeyManagementKey), nameof(GXKeyManagementKey.KeyManagement))]
        public GXDevice? Item
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
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [ExcludeOpenApi(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device))]
        [ExcludeOpenApi(typeof(GXDevice), nameof(GXDevice.Creator),
            nameof(GXDevice.Objects),
            nameof(GXDevice.Keys),
            nameof(GXDevice.Actions), nameof(GXDevice.Traces),
            nameof(GXDevice.Errors), nameof(GXDevice.Tasks))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id))]
        [IncludeOpenApi(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeOpenApi(typeof(GXGateway), nameof(GXGateway.Id))]
        [IncludeOpenApi(typeof(GXDeviceAction), nameof(GXDeviceAction.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [ExcludeOpenApi(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device), nameof(GXDeviceParameter.Module))]
        [IncludeOpenApi(typeof(GXKeyManagement), nameof(GXKeyManagement.Id), nameof(GXKeyManagement.Name))]
        [ExcludeOpenApi(typeof(GXKeyManagement), nameof(GXKeyManagement.Device))]
        [ExcludeOpenApi(typeof(GXKeyManagementKey), nameof(GXKeyManagementKey.KeyManagement))]
        public GXDevice[]? Devices
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

        /// <summary>
        /// Device groups where devices are added.
        /// </summary>
        [IncludeOpenApi(typeof(GXDeviceGroup),
            nameof(GXDeviceGroup.Id),
            nameof(GXDeviceGroup.Name))]
        public IEnumerable<GXDeviceGroup>? Groups
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
        public Guid[]? Ids
        {
            get;
            set;
        }

        /// <summary>
        /// Device groups IDs where devices are added.
        /// </summary>
        public Guid[]? Groups
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
        /// Amount of the devices to retrieve.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter devices.
        /// </summary>
        [ExcludeOpenApi(typeof(GXDevice), nameof(GXDevice.DeviceGroups),
                nameof(GXDevice.Creator), nameof(GXDevice.Parameters),
            nameof(GXDevice.Objects), nameof(GXDevice.Actions), nameof(GXDevice.Errors),
            nameof(GXDevice.Tasks), nameof(GXDevice.Traces)
            , nameof(GXDevice.ProfilePicture)
            , nameof(GXDevice.Settings)
            , nameof(GXDevice.Keys)
            , nameof(GXDevice.MediaSettings))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [ExcludeOpenApi(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id))]
        [IncludeOpenApi(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeOpenApi(typeof(GXGateway), nameof(GXGateway.Id))]
        [IncludeOpenApi(typeof(GXDeviceAction), nameof(GXDeviceAction.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [ExcludeOpenApi(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device), nameof(GXDeviceParameter.Module))]
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
        public string[]? Select
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
        /// Included Ids.
        /// </summary>
        /// <remarks>
        /// Included Ids can be used to get only part of large data.
        /// </remarks>
        public Guid[]? Included
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
        [ExcludeOpenApi(typeof(GXDevice), nameof(GXDevice.DeviceGroups)
            , nameof(GXDevice.Parameters), nameof(GXDevice.Objects)
            , nameof(GXDevice.Actions), nameof(GXDevice.Errors),
            nameof(GXDevice.Tasks), nameof(GXDevice.Keys), nameof(GXDevice.Traces))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [ExcludeOpenApi(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id))]
        [IncludeOpenApi(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeOpenApi(typeof(GXGateway), nameof(GXGateway.Id))]
        [IncludeOpenApi(typeof(GXDeviceAction), nameof(GXDeviceAction.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [ExcludeOpenApi(typeof(GXDeviceParameter), nameof(GXDeviceParameter.Device), nameof(GXDeviceParameter.Module))]
        public GXDevice[]? Devices
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the devices.
        /// </summary>
        /// <remarks>
        /// With large databases reading the amount of the data can take a very long time.
        /// In those cases the count is set to -1.
        /// </remarks>
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
    /// Remove device response.
    /// </summary>
    [DataContract]
    public class RemoveDeviceResponse
    {
    }

    /// <summary>
    /// Update device state.
    /// </summary>
    [DataContract]
    public class UpdateDeviceStatus
    {
        /// <summary>
        /// Device ID.
        /// </summary>
        [DataMember]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Device status.
        /// </summary>
        [DataMember]
        public DTOs.Enums.DeviceStatus Status
        {
            get;
            set;
        }
    }
}
