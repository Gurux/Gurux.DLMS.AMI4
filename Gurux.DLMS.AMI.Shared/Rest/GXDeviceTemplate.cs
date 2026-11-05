// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm.Common;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested device template information.</summary>
    [Description("Response containing the requested device template information.")]
    public class GetDeviceTemplateResponse
    {
        /// <summary>
        /// Device template information.
        /// </summary>
        [Description("Device template information.")]
        [IncludeOpenApi(typeof(GXDeviceTemplateGroup), nameof(GXDeviceTemplateGroup.Id), nameof(GXDeviceTemplateGroup.Name))]
        [IncludeOpenApi(typeof(GXObjectTemplate), nameof(GXObjectTemplate.Id), nameof(GXObjectTemplate.Name))]
        [ExcludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Keys))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [ExcludeOpenApi(typeof(GXDeviceModel), nameof(GXDeviceModel.Manufacturer))]
        [ExcludeOpenApi(typeof(GXDeviceVersion), nameof(GXDeviceVersion.Model))]
        [ExcludeOpenApi(typeof(GXDeviceSettings), nameof(GXDeviceSettings.Version)
            , nameof(GXDeviceSettings.Template))]
        [ExcludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.Models), nameof(GXManufacturer.ManufacturerGroups))]
        public GXDeviceTemplate? Item { get; set; }
    }

    /// <summary>Requests changes to the specified device templates. A new record is added when its identifier is zero.</summary>
    [DataContract]
    [Description("Requests changes to the specified device templates. A new record is added when its identifier is zero.")]
    public class UpdateDeviceTemplate : IGXRequest<UpdateDeviceTemplateResponse>
    {
        /// <summary>
        /// Inserted or updated device templates.
        /// </summary>
        [Description("Inserted or updated device templates.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXDeviceTemplateGroup), nameof(GXDeviceTemplateGroup.Id))]
        [ExcludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Objects)
            , nameof(GXDeviceTemplate.Keys))]
        [IncludeOpenApi(typeof(GXKeyManagement), nameof(GXKeyManagement.Id), nameof(GXKeyManagement.Name))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXDeviceModel), nameof(GXDeviceModel.Manufacturer))]
        [ExcludeOpenApi(typeof(GXDeviceVersion), nameof(GXDeviceVersion.Model))]
        [ExcludeOpenApi(typeof(GXDeviceSettings), nameof(GXDeviceSettings.Version), nameof(GXDeviceSettings.Template))]
        [ExcludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.ManufacturerGroups))]
        [ExcludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.CreationTime), nameof(GXDeviceTemplate.Updated))]
        [ExcludeOpenApi(typeof(GXDeviceSettings), nameof(GXDeviceSettings.CreationTime), nameof(GXDeviceSettings.Updated))]
        [ExcludeOpenApi(typeof(GXDeviceVersion), nameof(GXDeviceVersion.CreationTime), nameof(GXDeviceVersion.Updated))]
        [ExcludeOpenApi(typeof(GXDeviceModel), nameof(GXDeviceModel.CreationTime), nameof(GXDeviceModel.Updated))]
        [ExcludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.CreationTime), nameof(GXManufacturer.Updated))]
        public IEnumerable<GXDeviceTemplate>? Templates { get; set; }
    }

    /// <summary>Response to updating the specified device templates.</summary>
    [DataContract]
    [Description("Response to updating the specified device templates.")]
    public class UpdateDeviceTemplateResponse
    {
        /// <summary>Identifiers of the added or updated device template records.</summary>
        [Description("Identifiers of the added or updated device template records.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests a list of device templates.</summary>
    [Description("Requests a list of device templates.")]
    [DataContract]
    public class ListDeviceTemplates : IGXRequest<ListDeviceTemplatesResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of device templates to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of device templates to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter device templates.
        /// </summary>
        [Description("Optional criteria used to filter device templates.")]
        [IncludeOpenApi(typeof(GXDeviceTemplateGroup), nameof(GXDeviceTemplateGroup.Id), nameof(GXDeviceTemplateGroup.Name))]
        [ExcludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Objects), nameof(GXDeviceTemplate.Keys))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXDeviceModel), nameof(GXDeviceModel.Manufacturer))]
        [ExcludeOpenApi(typeof(GXDeviceVersion), nameof(GXDeviceVersion.Model))]
        [ExcludeOpenApi(typeof(GXDeviceSettings), nameof(GXDeviceSettings.Version), nameof(GXDeviceSettings.Template))]
        [ExcludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.ManufacturerGroups))]
        [ExcludeOpenApi(typeof(GXDeviceTemplate),
            nameof(GXDeviceTemplate.WaitTime), nameof(GXDeviceTemplate.ResendCount), nameof(GXDeviceTemplate.Settings),
            nameof(GXDeviceTemplate.Manufacturer), nameof(GXDeviceTemplate.ConcurrencyStamp), nameof(GXDeviceTemplate.DeviceTemplateGroups))]
        public GXDeviceTemplate? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public stored date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }


        /// <summary>
        /// Admin user can access templates from all users.
        /// </summary>
        /// <remarks>
        /// If true, templates from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access templates from all users.")]
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

        /// <summary>
        /// Get all device templates that are used in device groups.
        /// </summary>
        [Description("Get all device templates that are used in device groups.")]
        public IEnumerable<Guid>? DeviceGroups { get; set; }
    }

    /// <summary>Response containing the requested device templates.</summary>
    [DataContract]
    [Description("Response containing the requested device templates.")]
    public class ListDeviceTemplatesResponse
    {
        /// <summary>
        /// List of device templates.
        /// </summary>
        [DataMember]
        [Description("List of device templates.")]
        [IncludeOpenApi(typeof(GXDeviceTemplateGroup), nameof(GXDeviceTemplateGroup.Id), nameof(GXDeviceTemplateGroup.Name))]
        [ExcludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Objects), nameof(GXDeviceTemplate.Keys))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [IncludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.Id),
            nameof(GXManufacturer.Name), nameof(GXManufacturer.Models))]
        [IncludeOpenApi(typeof(GXDeviceModel), nameof(GXDeviceModel.Id),
            nameof(GXDeviceModel.Name), nameof(GXDeviceModel.Versions))]
        [IncludeOpenApi(typeof(GXDeviceVersion), nameof(GXDeviceVersion.Id), nameof(GXDeviceVersion.Name))]
        public IEnumerable<GXDeviceTemplate> Templates { get; set; } = default!;

        /// <summary>
        /// Total number of matching device templates before paging.
        /// </summary>
        [DataMember]
        [Description("Total number of matching device templates before paging.")]
        public long Count { get; set; }
    }

    /// <summary>Requests deletion of the selected device templates.</summary>
    [Description("Requests deletion of the selected device templates.")]
    [DataContract]
    public class RemoveDeviceTemplate : IGXRequest<RemoveDeviceTemplateResponse>
    {
        /// <summary>Identifiers of the device template records to delete.</summary>
        [Description("Identifiers of the device template records to delete.")]
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

    /// <summary>Response to deleting the selected device templates.</summary>
    [DataContract]
    [Description("Response to deleting the selected device templates.")]
    public class RemoveDeviceTemplateResponse
    {
    }
}
