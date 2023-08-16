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
using Gurux.Common.Db;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// COSEM attribute value.
    /// </summary>
    [DataContract]
    public class GXValue : IUnique<Guid>
    {
        /// <summary>
        /// Value identifier.
        /// </summary>
        [DataMember]
        [Description("Value identifier.")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Attribute .
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXAttribute), OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXAttribute? Attribute
        {
            get;
            set;
        }

        /// <summary>
        /// attribute value.
        /// </summary>
        [DataMember]
        [Description("Object value.")]
        [Filter(FilterType.Contains)]
        public string? Value
        {
            get;
            set;
        }

        /// <summary>
        /// Read time.
        /// </summary>
        [DataMember]
        [Description("Read time.")]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? Read
        {
            get;
            set;
        }

        /// <summary>
        /// The user has entered a new value.
        /// </summary>
        /// <remarks>
        /// If value is false then the value is read from the meter.
        /// </remarks>
        [DataMember]
        [Description("The user has entered a new value.")]
        [Filter(FilterType.Exact)]
        [DefaultValue(false)]
        [IsRequired]
        public bool? User
        {
            get;
            set;
        }
    }
}
