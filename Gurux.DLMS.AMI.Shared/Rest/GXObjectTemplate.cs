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

using Gurux.Service.Orm.Common;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested object template information.</summary>
    [Description("Response containing the requested object template information.")]
    public class GetObjectTemplateResponse
    {
        /// <summary>
        /// Object information.
        /// </summary>        
        [Description("Object information.")]
        [IncludeOpenApi(typeof(GXUser),
        nameof(GXUser.Id), nameof(GXUser.UserName))]
        [ExcludeOpenApi(typeof(GXDeviceModel),
            nameof(GXDeviceModel.Manufacturer))]
        [ExcludeOpenApi(typeof(GXDeviceVersion),
            nameof(GXDeviceVersion.Model))]
        [ExcludeOpenApi(typeof(GXDeviceSettings),
            nameof(GXDeviceSettings.Version))]
        [IncludeOpenApi(typeof(GXDeviceTemplate),
            nameof(GXDeviceTemplate.Id))]
        [ExcludeOpenApi(typeof(GXAttributeTemplate),
            nameof(GXAttributeTemplate.ObjectTemplate))]
        [ExcludeOpenApi(typeof(GXAttributeListItem),
            nameof(GXAttributeListItem.Template))]
        public GXObjectTemplate? Item { get; set; }
    }


    /// <summary>Requests the addition or update of COSEM object templates.</summary>
    [Description("Requests the addition or update of COSEM object templates.")]
    [DataContract]
    public class UpdateObjectTemplate : IGXRequest<UpdateObjectTemplateResponse>
    {
        /// <summary>
        /// Added COSEM object templates.
        /// </summary>
        [Description("Added COSEM object templates.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXDeviceTemplate),
            nameof(GXDeviceTemplate.Id))]
        [ExcludeOpenApi(typeof(GXAttributeTemplate),
            nameof(GXAttributeTemplate.ObjectTemplate))]
        [ExcludeOpenApi(typeof(GXAttributeListItem),
            nameof(GXAttributeListItem.Template))]
        [ExcludeOpenApi(typeof(GXObjectTemplate), nameof(GXObjectTemplate.CreationTime), nameof(GXObjectTemplate.Updated))]
        [ExcludeOpenApi(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.CreationTime), nameof(GXAttributeTemplate.Updated))]
        public IEnumerable<GXObjectTemplate>? ObjectTemplates { get; set; }
    }

    /// <summary>Response to updating the specified object templates.</summary>
    [Description("Response to updating the specified object templates.")]
    [DataContract]
    public class UpdateObjectTemplateResponse
    {
        /// <summary>Identifiers of the added or updated object template records.</summary>
        [Description("Identifiers of the added or updated object template records.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }


    /// <summary>Requests a list of object templates.</summary>
    [Description("Requests a list of object templates.")]
    [DataContract]
    public class ListObjectTemplates : IGXRequest<ListObjectTemplatesResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of COSEM object templates to retrieve. Use 0 to return all matching items without paging.</summary>
        [DataMember]
        [DefaultValue(0)]
        [Description("Maximum number of COSEM object templates to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter object templates.
        /// </summary>
        [Description("Optional criteria used to filter object templates.")]
        [ExcludeOpenApi(typeof(GXObjectTemplate),
                    nameof(GXObjectTemplate.Attributes),
            nameof(GXObjectTemplate.DeviceTemplate))]
        [ExcludeOpenApi(typeof(GXObjectTemplate),
            nameof(GXObjectTemplate.Version), nameof(GXObjectTemplate.ExpirationTime), nameof(GXObjectTemplate.ActionAccessLevels))]
        public GXObjectTemplate? Filter { get; set; }

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
        /// Admin user can access COSEM object templates from all users.
        /// </summary>
        /// <remarks>
        /// If true, COSEM object templates from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access COSEM object templates from all users.")]
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

        /// <summary>Device template identifiers.</summary>
        [Description("Device template identifiers.")]
        public IEnumerable<Guid>? DeviceTemplates { get; set; }

        /// <summary>
        /// Filtered object types.
        /// </summary>
        /// <remarks>
        /// </remarks>
        [Description("Filtered object types.")]
        public IEnumerable<int>? ObjectTypes { get; set; }

        /// <summary>
        /// Ignored object types.
        /// </summary>
        [Description("Ignored object types.")]
        public IEnumerable<int>? IgnoredObjectTypes { get; set; }
    }

    /// <summary>Response containing the requested object templates.</summary>
    [Description("Response containing the requested object templates.")]
    [DataContract]
    public class ListObjectTemplatesResponse
    {

        /// <summary>
        /// List of COSEM object templates.
        /// </summary>
        [Description("List of COSEM object templates.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id))]
        [ExcludeOpenApi(typeof(GXObjectTemplate), nameof(GXObjectTemplate.Attributes))]
        public IEnumerable<GXObjectTemplate>? ObjectTemplates { get; set; }
        /// <summary>
        /// Total number of matching object templates before paging.
        /// </summary>
        [Description("Total number of matching object templates before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests deletion of the selected object templates.</summary>
    [Description("Requests deletion of the selected object templates.")]
    public class RemoveObjectTemplate : IGXRequest<RemoveObjectTemplateResponse>
    {
        /// <summary>Identifiers of the object template records to delete.</summary>
        [Description("Identifiers of the object template records to delete.")]
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

    /// <summary>Response to deleting the selected object templates.</summary>
    [Description("Response to deleting the selected object templates.")]
    [DataContract]
    public class RemoveObjectTemplateResponse
    {
    }
}
