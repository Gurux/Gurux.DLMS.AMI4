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
    /// <summary>
    /// Get historical value from the DB.
    /// </summary>
    [DataContract]
    public class ListValues : IGXRequest<ListValuesResponse>
    {
        /// <summary>
        /// Start index.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Amount of the values to retrieve.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Filter can be used to filter values.
        /// </summary>
        [ExcludeOpenApi(typeof(GXValue), nameof(GXValue.Attribute))]
        public GXValue? Filter { get; set; }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public IEnumerable<string>? Select { get; set; }

        /// <summary>
        /// Order by name.
        /// </summary>
        /// <remarks>
        /// Default order by is used if this is not set.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Are values shown as descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending { get; set; }

        /// <summary>
        /// Included Ids.
        /// </summary>
        /// <remarks>
        /// Included Ids can be used to get only part of large data.
        /// </remarks>
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Excluded Ids.
        /// </summary>
        /// <remarks>
        /// Excluded Ids can be used to filter data.
        /// </remarks>
        public IEnumerable<Guid>? Exclude { get; set; }

        /// <summary>
        /// Device Ids for which values are retrieved.
        /// </summary>
        public IEnumerable<Guid>? Devices { get; set; }

        /// <summary>
        /// Object Ids for which values are retrieved.
        /// </summary>
        public IEnumerable<Guid>? Objects { get; set; }

        /// <summary>
        /// Attribute Ids for which values are retrieved.
        /// </summary>
        public IEnumerable<Guid>? Attributes { get; set; }

        /// <summary>
        /// Attribute template Ids for which values are retrieved.
        /// </summary>
        public IEnumerable<Guid>? AttributeTemplates { get; set; }
    }

    /// <summary>
    /// Get values reply.
    /// </summary>
    [DataContract]
    public class ListValuesResponse
    {
        /// <summary>
        /// List of value items.
        /// </summary>
        [DataMember]
        [ExcludeOpenApi(typeof(GXValue), nameof(GXValue.Attribute))]
        public IEnumerable<GXValue>? Values { get; set; }

        /// <summary>
        /// Total count of the value items.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Add attribute value.
    /// </summary>
    [DataContract]
    public class AddValue : IGXRequest<AddValueResponse>
    {
        /// <summary>
        /// Values to add.
        /// </summary>
        [DataMember]
        [Description("Values to add")]
        [ExcludeOpenApi(typeof(GXValue), nameof(GXValue.Attribute))]
        public IEnumerable<GXValue> Values { get; set; } = default!;
    }

    /// <summary>
    /// Value add response.
    /// </summary>
    [Description("Value add response.")]
    [DataContract]
    public class AddValueResponse
    {
        /// <summary>
        /// New value identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Clear attribute, object or device value.
    /// </summary>
    [DataContract]
    public class ClearValue : IGXRequest<ClearValueResponse>
    {
        /// <summary>
        /// Devices to clear.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXDevice), nameof(GXDevice.Id))]
        public IEnumerable<GXDevice>? Devices { get; set; }
        /// <summary>
        /// Objects to clear.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXObject), nameof(GXObject.Id))]
        public IEnumerable<GXObject>? Objects { get; set; }
        /// <summary>
        /// Attributes to clear.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXAttribute), nameof(GXAttribute.Id))]
        public IEnumerable<GXAttribute>? Attributes { get; set; }
    }

    /// <summary>
    /// Clear Value response.
    /// </summary>
    [DataContract]
    public class ClearValueResponse
    {

    }
}
