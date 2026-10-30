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
using Gurux.DLMS.AMI.Shared.DTOs;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested attribute template information.</summary>
    [Description("Response containing the requested attribute template information.")]
    public class GetAttributeTemplateResponse
    {
        /// <summary>
        /// AttributeTemplate information.
        /// </summary>
        [Description("AttributeTemplate information.")]
        [ExcludeOpenApi(typeof(GXAttributeTemplate),
                nameof(GXAttributeTemplate.ObjectTemplate),
                nameof(GXAttributeTemplate.ListItems))]
        public GXAttributeTemplate? Item { get; set; }
    }


    /// <summary>Requests the addition or update of COSEM attribute templates.</summary>
    [Description("Requests the addition or update of COSEM attribute templates.")]
    [DataContract]
    public class UpdateAttributeTemplate : IGXRequest<UpdateAttributeTemplateResponse>
    {
        /// <summary>
        /// Added COSEM attribute templates.
        /// </summary>
        [Description("Added COSEM attribute templates.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXObjectTemplate), nameof(GXObjectTemplate.Id))]
        [ExcludeOpenApi(typeof(GXAttributeTemplate),
                nameof(GXAttributeTemplate.ListItems))]
        [ExcludeOpenApi(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.CreationTime), nameof(GXAttributeTemplate.Updated))]
        public IEnumerable<GXAttributeTemplate>? AttributeTemplates { get; set; }
    }

    /// <summary>Response to updating the specified attribute templates.</summary>
    [Description("Response to updating the specified attribute templates.")]
    [DataContract]
    public class UpdateAttributeTemplateResponse
    {
        /// <summary>Identifiers of the added or updated attribute template records.</summary>
        [Description("Identifiers of the added or updated attribute template records.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests a list of attribute templates.</summary>
    [Description("Requests a list of attribute templates.")]
    [DataContract]
    public class ListAttributeTemplates : IGXRequest<ListAttributeTemplatesResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DataMember]
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of items to retrieve. Use 0 to return all matching items without paging.</summary>
        [DataMember]
        [DefaultValue(0)]
        [Description("Maximum number of items to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter attribute templates.
        /// </summary>
        [Description("Optional criteria used to filter attribute templates.")]
        [ExcludeOpenApi(typeof(GXAttributeTemplate),
               nameof(GXAttributeTemplate.ObjectTemplate),
               nameof(GXAttributeTemplate.ListItems))]
        [ExcludeOpenApi(typeof(GXAttributeTemplate),
            nameof(GXAttributeTemplate.ExpirationTime), nameof(GXAttributeTemplate.ConcurrencyStamp), nameof(GXAttributeTemplate.AccessLevel),
            nameof(GXAttributeTemplate.DataType), nameof(GXAttributeTemplate.UIDataType), nameof(GXAttributeTemplate.Weight),
            nameof(GXAttributeTemplate.Scaler), nameof(GXAttributeTemplate.Unit))]
        public GXAttributeTemplate? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }

        /// <summary>
        /// Admin user can access attribute templates from all users.
        /// </summary>
        /// <remarks>
        /// If true, attribute templates from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access attribute templates from all users.")]
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

    /// <summary>Response containing the requested attribute templates.</summary>
    [Description("Response containing the requested attribute templates.")]
    [DataContract]
    public class ListAttributeTemplatesResponse
    {

        /// <summary>
        /// List of COSEM attribute templates.
        /// </summary>
        [Description("List of COSEM attribute templates.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXAttributeTemplate),
                nameof(GXAttributeTemplate.ObjectTemplate),
                nameof(GXAttributeTemplate.ListItems))]
        public IEnumerable<GXAttributeTemplate>? AttributeTemplates { get; set; }
        /// <summary>
        /// Total number of matching attribute templates before paging.
        /// </summary>
        [Description("Total number of matching attribute templates before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests deletion of the selected attribute templates.</summary>
    [Description("Requests deletion of the selected attribute templates.")]
    public class RemoveAttributeTemplate : IGXRequest<RemoveAttributeTemplateResponse>
    {
        /// <summary>Identifiers of the attribute template records to delete.</summary>
        [Description("Identifiers of the attribute template records to delete.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.</summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [Description("Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.")]
        [DataMember]
        public bool Delete { get; set; }
    }

    /// <summary>Response to deleting the selected attribute templates.</summary>
    [Description("Response to deleting the selected attribute templates.")]
    [DataContract]
    public class RemoveAttributeTemplateResponse
    {
    }

    /// <summary>Requests changes to the attribute template data type.</summary>
    [Description("Requests changes to the attribute template data type.")]
    [DataContract]
    public class UpdateAttributeTemplateDataType : IGXRequest<UpdateAttributeTemplateDataTypeResponse>
    {
        /// <summary>
        /// Updated attribute templates.
        /// </summary>
        [Description("Updated attribute templates.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXAttributeTemplate),
            nameof(GXAttributeTemplate.Id),
            nameof(GXAttributeTemplate.DataType),
            nameof(GXAttributeTemplate.UIDataType))]
        public IEnumerable<GXAttributeTemplate>? Items { get; set; }
    }

    /// <summary>Response to updating the attribute template data type.</summary>
    [Description("Response to updating the attribute template data type.")]
    [DataContract]
    public class UpdateAttributeTemplateDataTypeResponse
    {
    }
}
