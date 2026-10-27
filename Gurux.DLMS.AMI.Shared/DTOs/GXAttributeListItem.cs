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
// This file is a part of Gurux Attribute Framework.
//
// Gurux Attribute Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License 
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Attribute Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2. 
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>An item in the list of possible values for an attribute.</summary>
    [Description("An item in the list of possible values for an attribute.")]
    public class GXAttributeListItem : IUnique<Guid>
    {
        /// <summary>
        /// Attribute value identifier.
        /// </summary>
        [Description("Attribute value identifier.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// Parent attribute template.
        /// </summary>
        [Description("Parent attribute template.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [JsonIgnore]
        public GXAttributeTemplate? Template { get; set; }

        /// <summary>Display label corresponding to the numeric attribute value.</summary>
        [Description("Display label corresponding to the numeric attribute value.")]
        [DataMember]
        public string? UIValue { get; set; }

        /// <summary>Numeric attribute value represented by the display label.</summary>
        [Description("Numeric attribute value represented by the display label.")]
        [DataMember]
        public int Value { get; set; }

        /// <summary>
        /// Mask size.
        /// </summary>
        [Description("Mask size.")]
        [DataMember]
        public int MaskSize { get; set; }

        /// <summary>
        /// Shift.
        /// </summary>
        [Description("Shift.")]
        [DataMember]
        public int Shift { get; set; }

    }
}
