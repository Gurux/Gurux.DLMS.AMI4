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
using Gurux.DLMS.AMI.Shared.DTOs.Device;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Requests a list of values.</summary>
    [Description("Requests a list of values.")]
    [DataContract]
    public class ListValues : IGXRequest<ListValuesResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of values to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of values to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter values.
        /// </summary>
        [Description("Optional criteria used to filter values.")]
        [ExcludeOpenApi(typeof(GXValue), nameof(GXValue.Attribute))]
        public GXValue? Filter { get; set; }

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

        /// <summary>Device identifiers for which values are retrieved.</summary>
        [Description("Device identifiers for which values are retrieved.")]
        public IEnumerable<Guid>? Devices { get; set; }

        /// <summary>Object identifiers for which values are retrieved.</summary>
        [Description("Object identifiers for which values are retrieved.")]
        public IEnumerable<Guid>? Objects { get; set; }

        /// <summary>Attribute identifiers for which values are retrieved.</summary>
        [Description("Attribute identifiers for which values are retrieved.")]
        public IEnumerable<Guid>? Attributes { get; set; }

        /// <summary>Attribute template identifiers for which values are retrieved.</summary>
        [Description("Attribute template identifiers for which values are retrieved.")]
        public IEnumerable<Guid>? AttributeTemplates { get; set; }
    }

    /// <summary>Response containing the requested values.</summary>
    [Description("Response containing the requested values.")]
    [DataContract]
    public class ListValuesResponse
    {
        /// <summary>
        /// List of value items.
        /// </summary>
        [Description("List of value items.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXValue), nameof(GXValue.Attribute))]
        public IEnumerable<GXValue>? Values { get; set; }

        /// <summary>
        /// Total number of matching value items before paging.
        /// </summary>
        [Description("Total number of matching value items before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests the addition of values.</summary>
    [Description("Requests the addition of values.")]
    [DataContract]
    public class AddValue : IGXRequest<AddValueResponse>
    {
        /// <summary>
        /// Values to add.
        /// </summary>
        [DataMember]
        [Description("Values to add.")]
        [ExcludeOpenApi(typeof(GXValue), nameof(GXValue.Attribute))]
        public IEnumerable<GXValue> Values { get; set; } = default!;
    }

    /// <summary>Response to adding values.</summary>
    [Description("Response to adding values.")]
    [DataContract]
    public class AddValueResponse
    {
        /// <summary>Identifiers of the newly added value records.</summary>
        [Description("Identifiers of the newly added value records.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Clear attribute, object or device value.
    /// </summary>
    [Description("Clear attribute, object or device value.")]
    [DataContract]
    public class ClearValue : IGXRequest<ClearValueResponse>
    {
        /// <summary>
        /// Devices to clear.
        /// </summary>
        [Description("Devices to clear.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id))]
        public IEnumerable<GXDevice>? Devices { get; set; }
        /// <summary>
        /// Objects to clear.
        /// </summary>
        [Description("Objects to clear.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXObject), nameof(GXObject.Id))]
        public IEnumerable<GXObject>? Objects { get; set; }
        /// <summary>
        /// Attributes to clear.
        /// </summary>
        [Description("Attributes to clear.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXAttribute), nameof(GXAttribute.Id))]
        public IEnumerable<GXAttribute>? Attributes { get; set; }
    }

    /// <summary>
    /// Clear Value response.
    /// </summary>
    [Description("Clear Value response.")]
    [DataContract]
    public class ClearValueResponse
    {

    }
}
