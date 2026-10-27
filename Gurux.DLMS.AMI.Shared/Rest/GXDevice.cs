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

using Gurux.DLMS.AMI.Shared.DTOs.Log;
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
    /// <summary>Response containing the requested device information.</summary>
    [Description("Response containing the requested device information.")]
    public class GetDeviceResponse
    {
        /// <summary>
        /// Device information.
        /// </summary>        
        [Description("Device information.")]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id), nameof(GXUserGroup.Name))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]
        [ExcludeOpenApi(typeof(GXDeviceProperty), nameof(GXDeviceProperty.Device))]
        [ExcludeOpenApi(typeof(GXObject), nameof(GXObject.Device))]
        [ExcludeOpenApi(typeof(GXLog), nameof(GXLog.Creator))]
        [ExcludeOpenApi(typeof(GXDevice), nameof(GXDevice.Traces),
            nameof(GXDevice.Objects), nameof(GXDevice.Actions)
            , nameof(GXDevice.Keys)
            , nameof(GXDevice.Errors), nameof(GXDevice.Tasks))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id))]
        [IncludeOpenApi(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeOpenApi(typeof(GXGateway), nameof(GXGateway.Id))]
        [IncludeOpenApi(typeof(GXLog), nameof(GXLog.PublicId))]
        [ExcludeOpenApi(typeof(GXDeviceProperty), nameof(GXDeviceProperty.Device), nameof(GXDeviceProperty.Module))]
        [ExcludeOpenApi(typeof(GXKeyManagement), nameof(GXKeyManagement.Device))]
        [ExcludeOpenApi(typeof(GXKeyManagementKey), nameof(GXKeyManagementKey.KeyManagement))]
        public GXDevice? Item { get; set; }
    }

    /// <summary>Requests changes to the specified devices. A new record is added when its identifier is zero.</summary>
    [Description("Requests changes to the specified devices. A new record is added when its identifier is zero.")]
    [DataContract]
    public class UpdateDevice : IGXRequest<UpdateDeviceResponse>
    {
        /// <summary>Devices to add or update.</summary>
        [Description("Devices to add or update.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [ExcludeOpenApi(typeof(GXDeviceProperty), nameof(GXDeviceProperty.Device))]
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
        [IncludeOpenApi(typeof(GXLog), nameof(GXLog.PublicId))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [ExcludeOpenApi(typeof(GXDeviceProperty), nameof(GXDeviceProperty.Device), nameof(GXDeviceProperty.Module))]
        [IncludeOpenApi(typeof(GXKeyManagement), nameof(GXKeyManagement.Id), nameof(GXKeyManagement.Name))]
        [ExcludeOpenApi(typeof(GXKeyManagement), nameof(GXKeyManagement.Device))]
        [ExcludeOpenApi(typeof(GXKeyManagementKey), nameof(GXKeyManagementKey.KeyManagement))]
        [ExcludeOpenApi(typeof(GXDevice), nameof(GXDevice.CreationTime), nameof(GXDevice.Updated))]
        [ExcludeOpenApi(typeof(GXDeviceProperty), nameof(GXDeviceProperty.CreationTime), nameof(GXDeviceProperty.Updated))]
        [ExcludeOpenApi(typeof(GXAppearance), nameof(GXAppearance.CreationTime), nameof(GXAppearance.Updated))]
        public IEnumerable<GXDevice>? Devices { get; set; }

        /// <summary>Whether to defer creating device objects until they are read from the meter, reducing device creation time.</summary>
        [Description("Whether to defer creating device objects until they are read from the meter, reducing device creation time.")]
        public bool LateBinding { get; set; }

        /// <summary>Groups to which the devices will be added.</summary>
        [Description("Groups to which the devices will be added.")]
        [IncludeOpenApi(typeof(GXDeviceGroup),
            nameof(GXDeviceGroup.Id),
            nameof(GXDeviceGroup.Name))]
        public IEnumerable<GXDeviceGroup>? Groups { get; set; }
    }

    /// <summary>Response to updating the specified devices.</summary>
    [DataContract]
    [Description("Response to updating the specified devices.")]
    public class UpdateDeviceResponse
    {
        /// <summary>Identifiers of the added or updated device records.</summary>
        [Description("Identifiers of the added or updated device records.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>Identifiers of the groups to which the devices were added.</summary>
        [Description("Identifiers of the groups to which the devices were added.")]
        public IEnumerable<Guid>? Groups { get; set; }
    }

    /// <summary>Requests a list of devices.</summary>
    [Description("Requests a list of devices.")]
    [DataContract]
    public class ListDevices : IGXRequest<ListDevicesResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of devices to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of devices to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter devices.
        /// </summary>
        [Description("Optional criteria used to filter devices.")]
        [ExcludeOpenApi(typeof(GXDevice), nameof(GXDevice.DeviceGroups),
                nameof(GXDevice.Creator), nameof(GXDevice.Parameters),
            nameof(GXDevice.Objects), nameof(GXDevice.Actions), nameof(GXDevice.Errors),
            nameof(GXDevice.Tasks), nameof(GXDevice.Traces)
            , nameof(GXDevice.ProfilePicture)
            , nameof(GXDevice.Settings)
            , nameof(GXDevice.Keys)
            , nameof(GXDevice.MediaSettings))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [ExcludeOpenApi(typeof(GXDeviceProperty), nameof(GXDeviceProperty.Device))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id))]
        [IncludeOpenApi(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeOpenApi(typeof(GXGateway), nameof(GXGateway.Id))]
        [IncludeOpenApi(typeof(GXLog), nameof(GXLog.PublicId))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [ExcludeOpenApi(typeof(GXDeviceProperty), nameof(GXDeviceProperty.Device), nameof(GXDeviceProperty.Module))]
        [ExcludeOpenApi(typeof(GXDevice),
            nameof(GXDevice.Dc), nameof(GXDevice.Type), nameof(GXDevice.WaitTime),
            nameof(GXDevice.ResendCount), nameof(GXDevice.LateBinding), nameof(GXDevice.ConcurrencyStamp),
            nameof(GXDevice.ConnectionUpTime), nameof(GXDevice.KeepAliveAttribute), nameof(GXDevice.KeepAliveInterval),
            nameof(GXDevice.TraceLevel), nameof(GXDevice.Gateways), nameof(GXDevice.LastErrorMessage))]
        public GXDevice? Filter { get; set; }

        /// <summary>
        /// Admin user can access devices from all users.
        /// </summary>
        /// <remarks>
        /// If true, devices from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access devices from all users.")]
        public bool AllUsers { get; set; }

        /// <summary>Additional related information to include in the response. Reserved for future use.</summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        [Description("Additional related information to include in the response. Reserved for future use.")]
        public IEnumerable<string>? Select { get; set; }

        /// <summary>Name of the property to sort by. If omitted, the endpoint uses its default ordering.</summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        [Description("Name of the property to sort by. If omitted, the endpoint uses its default ordering.")]
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        [Description("Whether to sort results in descending order.")]
        public bool Descending { get; set; }

        /// <summary>Identifiers to include in the results. A nonempty collection restricts results to these identifiers.</summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        [Description("Identifiers to include in the results. A nonempty collection restricts results to these identifiers.")]
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        [Description("Identifiers to exclude from the results.")]
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>Response containing the requested devices.</summary>
    [DataContract]
    [Description("Response containing the requested devices.")]
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
        [ExcludeOpenApi(typeof(GXDeviceProperty), nameof(GXDeviceProperty.Device))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXDeviceGroup), nameof(GXDeviceGroup.Id))]
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id))]
        [IncludeOpenApi(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeOpenApi(typeof(GXGateway), nameof(GXGateway.Id))]
        [IncludeOpenApi(typeof(GXLog), nameof(GXLog.PublicId))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [ExcludeOpenApi(typeof(GXDeviceProperty), nameof(GXDeviceProperty.Device), nameof(GXDeviceProperty.Module))]
        public IEnumerable<GXDevice>? Devices { get; set; }

        /// <summary>
        /// Total number of matching devices before paging.
        /// </summary>
        [DataMember]
        [Description("Total number of matching devices before paging.")]
        public long Count { get; set; }
    }


    /// <summary>Requests deletion of the selected devices.</summary>
    [Description("Requests deletion of the selected devices.")]
    [DataContract]
    public class RemoveDevice : IGXRequest<RemoveDeviceResponse>
    {
        /// <summary>Identifiers of the device records to delete.</summary>
        [Description("Identifiers of the device records to delete.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.</summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [Description("Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.")]
        [DataMember]
        [Required]
        public bool Delete { get; set; }
    }

    /// <summary>Response to deleting the selected devices.</summary>
    [Description("Response to deleting the selected devices.")]
    [DataContract]
    public class RemoveDeviceResponse
    {
    }

    /// <summary>Requests changes to the device status.</summary>
    [Description("Requests changes to the device status.")]
    [DataContract]
    public class UpdateDeviceStatus
    {
        /// <summary>Device identifier.</summary>
        [Description("Device identifier.")]
        [DataMember]
        public Guid Id { get; set; }

        /// <summary>
        /// Device status.
        /// </summary>
        [Description("Device status.")]
        [DataMember]
        public DTOs.Enums.DeviceStatus Status { get; set; }
    }
}
