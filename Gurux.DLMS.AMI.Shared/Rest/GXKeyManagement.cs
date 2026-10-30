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
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.DTOs;

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested key management information.</summary>
    [Description("Response containing the requested key management information.")]
    public class GetKeyManagementResponse
    {
        /// <summary>
        /// Key management information.
        /// </summary>
        [Description("Key management information.")]
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
        [IncludeOpenApi(typeof(GXDeviceTemplate),
        nameof(GXDeviceTemplate.Id), nameof(GXDeviceTemplate.Name))]

        [ExcludeOpenApi(typeof(GXDeviceModel), nameof(GXDeviceModel.Manufacturer))]
        [ExcludeOpenApi(typeof(GXDeviceVersion), nameof(GXDeviceVersion.Model))]
        [ExcludeOpenApi(typeof(GXDeviceSettings), nameof(GXDeviceSettings.Version),
            nameof(GXDeviceSettings.Template))]
        [ExcludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.ManufacturerGroups))]
        [ExcludeOpenApi(typeof(GXObjectTemplate), nameof(GXObjectTemplate.DeviceTemplate))]
        [ExcludeOpenApi(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.ObjectTemplate))]
        [ExcludeOpenApi(typeof(GXAttributeListItem), nameof(GXAttributeListItem.Template))]
        [ExcludeOpenApi(typeof(GXKeyManagementProperty), nameof(GXKeyManagementProperty.Module))]
        public GXKeyManagement? Item { get; set; }
    }

    /// <summary>Requests a list of key management configurations.</summary>
    [Description("Requests a list of key management configurations.")]
    [DataContract]
    public class ListKeyManagements : IGXRequest<ListKeyManagementsResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of key management configurations to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of key management configurations to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>Optional criteria used to filter key management configurations.</summary>
        [Description("Optional criteria used to filter key management configurations.")]
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
        [ExcludeOpenApi(typeof(GXKeyManagement),
            nameof(GXKeyManagement.ProviderId), nameof(GXKeyManagement.ProviderConnectionId), nameof(GXKeyManagement.TraceLevel),
            nameof(GXKeyManagement.ConcurrencyStamp))]
        public GXKeyManagement? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }


        /// <summary>Admin user can access key management configurations from all users.</summary>
        /// <remarks>
        /// If true, key managements from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access key management configurations from all users.")]
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

    /// <summary>Response containing the requested key management configurations.</summary>
    [Description("Response containing the requested key management configurations.")]
    [DataContract]
    public class ListKeyManagementsResponse
    {
        /// <summary>
        /// List of key management items.
        /// </summary>
        [Description("List of key management items.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXKeyManagement),
        nameof(GXKeyManagement.Id), nameof(GXKeyManagement.Name),
            nameof(GXKeyManagement.SystemTitle)
            , nameof(GXKeyManagement.CreationTime)
            )]
        public IEnumerable<GXKeyManagement>? KeyManagements { get; set; }

        /// <summary>Total number of matching key management configurations before paging.</summary>
        [Description("Total number of matching key management configurations before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests changes to the specified key management configurations.</summary>
    [Description("Requests changes to the specified key management configurations.")]
    [DataContract]
    public class UpdateKeyManagement : IGXRequest<UpdateKeyManagementResponse>
    {
        /// <summary>Key management configurations to update.</summary>
        [Description("Key management configurations to update.")]
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
        [ExcludeOpenApi(typeof(GXKeyManagement),
        nameof(GXKeyManagement.Logs))]
        [ExcludeOpenApi(typeof(GXKeyManagementProperty),
        nameof(GXKeyManagementProperty.Module))]
        [ExcludeOpenApi(typeof(GXKeyManagementKey),
        nameof(GXKeyManagementKey.KeyManagement))]
        [IncludeOpenApi(typeof(GXKeyManagementGroup),
        nameof(GXKeyManagementGroup.Id), nameof(GXKeyManagementGroup.Name))]
        [ExcludeOpenApi(typeof(GXKeyManagementKey), nameof(GXKeyManagementKey.CreationTime), nameof(GXKeyManagementKey.Updated))]
        [ExcludeOpenApi(typeof(GXKeyManagementProperty), nameof(GXKeyManagementProperty.CreationTime), nameof(GXKeyManagementProperty.Updated))]
        [ExcludeOpenApi(typeof(GXKeyManagement), nameof(GXKeyManagement.CreationTime), nameof(GXKeyManagement.Updated))]
        public IEnumerable<GXKeyManagement> KeyManagements { get; set; } = default!;
    }

    /// <summary>Response to updating the specified key management configurations.</summary>
    [Description("Response to updating the specified key management configurations.")]
    [DataContract]
    public class UpdateKeyManagementResponse
    {
        /// <summary>Identifiers of the added or updated key management records.</summary>
        [Description("Identifiers of the added or updated key management records.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests deletion of the selected key management configurations.</summary>
    [Description("Requests deletion of the selected key management configurations.")]
    [DataContract]
    public class RemoveKeyManagement : IGXRequest<RemoveKeyManagementResponse>
    {
        /// <summary>Identifiers of the key management records to delete.</summary>
        [Description("Identifiers of the key management records to delete.")]
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

    /// <summary>Response to deleting the selected key management configurations.</summary>
    [Description("Response to deleting the selected key management configurations.")]
    [DataContract]
    public class RemoveKeyManagementResponse
    {
    }

    /// <summary>Requests changes to the keys.</summary>
    [Description("Requests changes to the keys.")]
    [DataContract]
    public class UpdateKeys : IGXRequest<UpdateKeysResponse>
    {
        /// <summary>Initializes a new instance of UpdateKeys.</summary>
        [Description("Initializes a new instance of UpdateKeys.")]
        public UpdateKeys()
        {
            Keys = [];
        }

        /// <summary>
        /// Keys to update.
        /// </summary>
        [Description("Keys to update.")]
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
        [ExcludeOpenApi(typeof(GXKeyManagementKey), nameof(GXKeyManagementKey.CreationTime), nameof(GXKeyManagementKey.Updated))]
        public List<GXKeyManagement> Keys { get; set; }
    }

    /// <summary>Response to updating the keys.</summary>
    [Description("Response to updating the keys.")]
    [DataContract]
    public class UpdateKeysResponse
    {

    }
}
