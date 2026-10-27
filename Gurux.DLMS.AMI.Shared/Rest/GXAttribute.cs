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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.Service.Orm.Common;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested attribute information.</summary>
    [Description("Response containing the requested attribute information.")]
    public class GetAttributeResponse
    {
        /// <summary>
        /// Attribute information.
        /// </summary>
        [Description("Attribute information.")]
        [IncludeOpenApi(typeof(GXObject),
                nameof(GXObject.Id))]
        [IncludeOpenApi(typeof(GXModule),
                nameof(GXModule.Id))]
        [ExcludeOpenApi(typeof(GXAttributeTemplate),
                nameof(GXAttributeTemplate.ObjectTemplate))]
        [ExcludeOpenApi(typeof(GXAttributeProperty),
                nameof(GXAttributeProperty.Attribute))]
        [ExcludeOpenApi(typeof(GXAttribute),
            nameof(GXAttribute.Tasks),
            nameof(GXAttribute.Values),
            nameof(GXAttribute.Errors))]
        [ExcludeOpenApi(typeof(GXAttributeListItem),
            nameof(GXAttributeListItem.Template))]
        public GXAttribute? Item { get; set; }
    }


    /// <summary>Requests the addition or update of COSEM attributes.</summary>
    [Description("Requests the addition or update of COSEM attributes.")]
    [DataContract]
    public class UpdateAttribute
    {
        /// <summary>
        /// Added COSEM attributes.
        /// </summary>
        [Description("Added COSEM attributes.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXObject), nameof(GXObject.Id))]
        [IncludeOpenApi(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.Id))]
        [IncludeOpenApi(typeof(GXModule), nameof(GXModule.Id))]
        [ExcludeOpenApi(typeof(GXAttributeProperty), nameof(GXAttributeProperty.Attribute))]
        [ExcludeOpenApi(typeof(GXAttribute),
            nameof(GXAttribute.CreationTime),
            nameof(GXAttribute.Updated),
            nameof(GXAttribute.Tasks),
            nameof(GXAttribute.Values),
            nameof(GXAttribute.Errors))]
        [ExcludeOpenApi(typeof(GXAttributeProperty), nameof(GXAttributeProperty.CreationTime), nameof(GXAttributeProperty.Updated))]
        public IEnumerable<GXAttribute>? Attributes { get; set; }
    }

    /// <summary>Response to updating the specified attributes.</summary>
    [Description("Response to updating the specified attributes.")]
    [DataContract]
    public class UpdateAttributeResponse
    {
        /// <summary>Identifiers of the added or updated attribute records.</summary>
        [Description("Identifiers of the added or updated attribute records.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests a list of attributes.</summary>
    [Description("Requests a list of attributes.")]
    [DataContract]
    public class ListAttributes : IGXRequest<ListAttributesResponse>
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
        /// Optional criteria used to filter attributes.
        /// </summary>
        [Description("Optional criteria used to filter attributes.")]
        [ExcludeOpenApi(typeof(GXAttribute),
            nameof(GXAttribute.Object),
            nameof(GXAttribute.Template),
            nameof(GXAttribute.Tasks),
            nameof(GXAttribute.Errors),
            nameof(GXAttribute.Parameters),
            nameof(GXAttribute.Values))]
        [ExcludeOpenApi(typeof(GXAttribute),
            nameof(GXAttribute.Settings), nameof(GXAttribute.Modified), nameof(GXAttribute.ConcurrencyStamp),
            nameof(GXAttribute.ExpirationTime))]
        public GXAttribute? Filter { get; set; }

        /// <summary>
        /// Admin user can access attributes from all users.
        /// </summary>
        /// <remarks>
        /// If true, attributes from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access attributes from all users.")]
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

    /// <summary>Response containing the requested attributes.</summary>
    [Description("Response containing the requested attributes.")]
    [DataContract]
    public class ListAttributesResponse
    {

        /// <summary>
        /// List of COSEM attributes.
        /// </summary>
        [Description("List of COSEM attributes.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXAttribute),
            nameof(GXAttribute.Object),
            nameof(GXAttribute.Tasks),
            nameof(GXAttribute.Errors),
            nameof(GXAttribute.Parameters),
            nameof(GXAttribute.Values))]
        [IncludeOpenApi(typeof(GXAttributeTemplate),
            nameof(GXAttributeTemplate.Id),
            nameof(GXAttributeTemplate.Name))]
        public IEnumerable<GXAttribute>? Attributes { get; set; }
        /// <summary>
        /// Total number of matching attributes before paging.
        /// </summary>
        [Description("Total number of matching attributes before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests deletion of the selected attributes.</summary>
    [Description("Requests deletion of the selected attributes.")]
    public class RemoveAttribute : IGXRequest<RemoveAttributeResponse>
    {
        /// <summary>Identifiers of the attribute records to delete.</summary>
        [Description("Identifiers of the attribute records to delete.")]
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

    /// <summary>Response to deleting the selected attributes.</summary>
    [Description("Response to deleting the selected attributes.")]
    [DataContract]
    public class RemoveAttributeResponse
    {
    }

    /// <summary>Requests changes to the data type.</summary>
    [Description("Requests changes to the data type.")]
    [DataContract]
    public class UpdateDatatype : IGXRequest<UpdateAttributeResponse>
    {
        /// <summary>
        /// Updated attributes.
        /// </summary>
        [Description("Updated attributes.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXAttribute),
            nameof(GXAttribute.Object),
            nameof(GXAttribute.Tasks),
            nameof(GXAttribute.Errors),
            nameof(GXAttribute.Values),
            nameof(GXAttribute.Parameters))]
        [IncludeOpenApi(typeof(GXAttributeTemplate),
                nameof(GXAttributeTemplate.Id))]
        [ExcludeOpenApi(typeof(GXAttribute), nameof(GXAttribute.CreationTime), nameof(GXAttribute.Updated))]
        public IEnumerable<GXAttribute> Items { get; set; } = default!;
    }

    /// <summary>Response to updating the data type.</summary>
    [Description("Response to updating the data type.")]
    [DataContract]
    public class UpdateDatatypeResponse
    {
    }
}
