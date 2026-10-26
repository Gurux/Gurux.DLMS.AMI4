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
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get key management.
    /// </summary>
    public class GetKeyManagementResponse
    {
        /// <summary>
        /// Key management information.
        /// </summary>
        [ExcludeOpenApi(typeof(GXKeyManagement),
        nameof(GXKeyManagement.Logs))]
        [ExcludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.DeviceTemplateGroups),
            nameof(GXDeviceTemplate.Keys))]
        [IncludeOpenApi(typeof(GXDevice),
        nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeOpenApi(typeof(GXUser),
        nameof(GXUser.Id), nameof(GXUser.UserName))]
        [ExcludeOpenApi(typeof(GXLog),
        nameof(GXLog.Creator))]
        [ExcludeOpenApi(typeof(GXKeyManagementKey),
        nameof(GXKeyManagementKey.KeyManagement))]
        [IncludeOpenApi(typeof(GXKeyManagementGroup),
        nameof(GXKeyManagementGroup.Id), nameof(GXKeyManagementGroup.Name))]
        [ExcludeOpenApi(typeof(GXDeviceGroupProperty),
        nameof(GXDeviceGroupProperty.DeviceGroup))]
        [ExcludeOpenApi(typeof(GXModule), nameof(GXModule.Assemblies))]
        [IncludeOpenApi(typeof(GXDeviceGroup),
        nameof(GXDeviceGroup.Id), nameof(GXDeviceGroup.Name))]

        [ExcludeOpenApi(typeof(GXDeviceModel), nameof(GXDeviceModel.Manufacturer))]
        [ExcludeOpenApi(typeof(GXDeviceVersion), nameof(GXDeviceVersion.Model))]
        [ExcludeOpenApi(typeof(GXDeviceSettings), nameof(GXDeviceSettings.Version),
            nameof(GXDeviceSettings.Template))]
        [ExcludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.ManufacturerGroups))]
        [ExcludeOpenApi(typeof(GXObjectTemplate), nameof(GXObjectTemplate.DeviceTemplate))]
        [ExcludeOpenApi(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.ObjectTemplate))]
        [ExcludeOpenApi(typeof(GXAttributeListItem), nameof(GXAttributeListItem.Template))]
        public GXKeyManagement? Item { get; set; }
    }

    /// <summary>
    /// Get list from key managements.
    /// </summary>
    [DataContract]
    public class ListKeyManagements : IGXRequest<ListKeyManagementsResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of key managements to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter key managements.
        /// </summary>
        [IncludeOpenApi(typeof(GXKeyManagement),
        nameof(GXKeyManagement.Id), nameof(GXKeyManagement.Name),
            nameof(GXKeyManagement.SystemTitle)
            , nameof(GXKeyManagement.CreationTime)
            )]
        [IncludeOpenApi(typeof(GXDevice),
        nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeOpenApi(typeof(GXUser),
        nameof(GXUser.Id), nameof(GXUser.UserName))]
        [ExcludeOpenApi(typeof(GXLog),
        nameof(GXLog.Creator))]
        [ExcludeOpenApi(typeof(GXKeyManagementKey),
        nameof(GXKeyManagementKey.KeyManagement))]
        [IncludeOpenApi(typeof(GXKeyManagementGroup),
        nameof(GXKeyManagementGroup.Id), nameof(GXKeyManagementGroup.Name))]
        public GXKeyManagement? Filter { get; set; }


        /// <summary>
        /// Admin user can access key managements from all users.
        /// </summary>
        /// <remarks>
        /// If true, key managements from all users are retrieved, not just current user.
        /// </remarks>
        public bool AllUsers { get; set; }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public IEnumerable<string>? Select { get; set; }

        /// <summary>
        /// Name of the property to sort by.
        /// </summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending { get; set; }

        /// <summary>
        /// Identifiers to include in the results.
        /// </summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>
    /// KeyManagement items reply.
    /// </summary>
    [DataContract]
    public class ListKeyManagementsResponse
    {
        /// <summary>
        /// List of key management items.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXKeyManagement),
        nameof(GXKeyManagement.Id), nameof(GXKeyManagement.Name),
            nameof(GXKeyManagement.SystemTitle)
            , nameof(GXKeyManagement.CreationTime)
            )]
        public IEnumerable<GXKeyManagement>? KeyManagements { get; set; }

        /// <summary>
        /// Total number of matching key managementrs before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Update key managements.
    /// </summary>
    [DataContract]
    public class UpdateKeyManagement : IGXRequest<UpdateKeyManagementResponse>
    {
        /// <summary>
        /// KeyManagements to update.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXDevice),
        nameof(GXDevice.Id))]
        [IncludeOpenApi(typeof(GXDeviceGroup),
        nameof(GXDeviceGroup.Id))]
        [IncludeOpenApi(typeof(GXDeviceTemplate),
        nameof(GXDeviceTemplate.Id))]
        [IncludeOpenApi(typeof(GXUser),
        nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXLog),
        nameof(GXLog.Creator))]
        [ExcludeOpenApi(typeof(GXKeyManagementKey),
        nameof(GXKeyManagementKey.KeyManagement))]
        [IncludeOpenApi(typeof(GXKeyManagementGroup),
        nameof(GXKeyManagementGroup.Id), nameof(GXKeyManagementGroup.Name))]
        public IEnumerable<GXKeyManagement> KeyManagements { get; set; } = default!;
    }

    /// <summary>
    /// Update key managements reply.
    /// </summary>
    [DataContract]
    public class UpdateKeyManagementResponse
    {
        /// <summary>
        /// Key management identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Delete key managements.
    /// </summary>
    [DataContract]
    public class RemoveKeyManagement : IGXRequest<RemoveKeyManagementResponse>
    {
        /// <summary>
        /// Key management identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>
        /// Whether to permanently delete the items from the database.
        /// </summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [DataMember]
        [Required]
        public bool Delete { get; set; }
    }

    /// <summary>
    /// Reply from Delete key management.
    /// </summary>
    [DataContract]
    public class RemoveKeyManagementResponse
    {
    }

    /// <summary>
    /// Update new keys for the meter.
    /// </summary>
    [DataContract]
    public class UpdateKeys : IGXRequest<UpdateKeysResponse>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UpdateKeys()
        {
            Keys = new List<GXKeyManagement>();
        }

        /// <summary>
        /// Keys to update.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXKeyManagement),
        nameof(GXKeyManagement.Id), nameof(GXKeyManagement.Keys))]
        [ExcludeOpenApi(typeof(GXKeyManagementKey),
            nameof(GXKeyManagementKey.KeyManagement))]
        /*
        [IncludeSwagger(typeof(GXDevice),
        nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeSwagger(typeof(GXUser),
        nameof(GXUser.Id), nameof(GXUser.UserName))]
        [ExcludeSwagger(typeof(GXLog),
        nameof(GXLog.Creator))]
        [IncludeSwagger(typeof(GXKeyManagementGroup),
        nameof(GXKeyManagementGroup.Id), nameof(GXKeyManagementGroup.Name))]
        */
        public List<GXKeyManagement> Keys { get; set; }
    }

    /// <summary>
    /// Update keys reply.
    /// </summary>
    [DataContract]
    public class UpdateKeysResponse
    {

    }
}
