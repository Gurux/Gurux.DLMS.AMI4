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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested object information.</summary>
    [Description("Response containing the requested object information.")]
    public class GetObjectResponse
    {
        /// <summary>
        /// Object information.
        /// </summary>        
        [Description("Object information.")]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [IncludeOpenApi(typeof(GXAttribute), nameof(GXAttribute.Id), nameof(GXAttribute.Template))]
        [IncludeOpenApi(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.Id), nameof(GXAttributeTemplate.Name))]
        [ExcludeOpenApi(typeof(GXObject), nameof(GXObject.Tasks), nameof(GXObject.Errors))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id), nameof(GXDevice.Name))]
        [IncludeOpenApi(typeof(GXObjectTemplate), nameof(GXObjectTemplate.Id), nameof(GXObjectTemplate.Name))]
        [ExcludeOpenApi(typeof(GXObjectProperty),
            nameof(GXObjectProperty.Object), nameof(GXObjectProperty.Module))]
        public GXObject? Item { get; set; }
    }

    /// <summary>Requests the addition or update of COSEM objects.</summary>
    [Description("Requests the addition or update of COSEM objects.")]
    [DataContract]
    public class UpdateObject : IGXRequest<UpdateObjectResponse>
    {
        /// <summary>
        /// Added COSEM objects.
        /// </summary>
        [Description("Added COSEM objects.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXObject),
            nameof(GXObject.Template),
            nameof(GXObject.Device),
            nameof(GXObject.Tasks),
            nameof(GXObject.Errors))]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeOpenApi(typeof(GXAttribute), nameof(GXAttribute.Id))]
        [ExcludeOpenApi(typeof(GXObjectProperty),
            nameof(GXObjectProperty.Object),
            nameof(GXObjectProperty.Module))]
        [ExcludeOpenApi(typeof(GXObject), nameof(GXObject.CreationTime), nameof(GXObject.Updated))]
        [ExcludeOpenApi(typeof(GXObjectProperty), nameof(GXObjectProperty.CreationTime), nameof(GXObjectProperty.Updated))]
        public IEnumerable<GXObject>? Objects { get; set; }
    }

    /// <summary>Response to updating the specified objects.</summary>
    [Description("Response to updating the specified objects.")]
    [DataContract]
    public class UpdateObjectResponse
    {
        /// <summary>Identifiers of the added or updated object records.</summary>
        [Description("Identifiers of the added or updated object records.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }


    /// <summary>Requests a list of objects.</summary>
    [Description("Requests a list of objects.")]
    [DataContract]
    public class ListObjects : IGXRequest<ListObjectsResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DataMember]
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of COSEM objects to retrieve. Use 0 to return all matching items without paging.</summary>
        [DataMember]
        [DefaultValue(0)]
        [Description("Maximum number of COSEM objects to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter objects.
        /// </summary>
        [Description("Optional criteria used to filter objects.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXObject), nameof(GXObject.Tasks),
            nameof(GXObject.Template),
            nameof(GXObject.Errors), nameof(GXObject.Device),
            nameof(GXObject.Attributes), nameof(GXObject.Properties))]
        [ExcludeOpenApi(typeof(GXObject),
            nameof(GXObject.Latebind), nameof(GXObject.ConcurrencyStamp), nameof(GXObject.LastErrorMessage))]
        public GXObject? Filter { get; set; }

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
        /// Admin user can access COSEM objects from all users.
        /// </summary>
        /// <remarks>
        /// If true, COSEM objects from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access COSEM objects from all users.")]
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

        /// <summary>Device identifiers.</summary>
        [Description("Device identifiers.")]
        public IEnumerable<Guid>? Devices { get; set; }

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

    /// <summary>Response containing the requested objects.</summary>
    [Description("Response containing the requested objects.")]
    [DataContract]
    public class ListObjectsResponse
    {

        /// <summary>
        /// List of COSEM objects.
        /// </summary>
        [Description("List of COSEM objects.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXObject), nameof(GXObject.Tasks), nameof(GXObject.Errors), nameof(GXObject.Device), nameof(GXObject.Attributes), nameof(GXObject.Properties))]
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id))]
        [ExcludeOpenApi(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.ObjectTemplate))]
        [ExcludeOpenApi(typeof(GXAttributeListItem), nameof(GXAttributeListItem.Template))]
        public IEnumerable<GXObject>? Objects { get; set; }
        /// <summary>
        /// Total number of matching objects before paging.
        /// </summary>
        [Description("Total number of matching objects before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests deletion of the selected objects.</summary>
    [Description("Requests deletion of the selected objects.")]
    public class RemoveObject : IGXRequest<RemoveObjectResponse>
    {
        /// <summary>Identifiers of the object records to delete.</summary>
        [Description("Identifiers of the object records to delete.")]
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

    /// <summary>Response to deleting the selected objects.</summary>
    [Description("Response to deleting the selected objects.")]
    [DataContract]
    public class RemoveObjectResponse
    {
    }
}
