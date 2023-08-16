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
using Gurux.Common.Db;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Attribute value.
    /// </summary>
    public class GXAttributeListItem : IUnique<Guid>
    {
        /// <summary>
        /// Attribute value identifier.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Parent attribute template.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [JsonIgnore]
        public GXAttributeTemplate? Template
        {
            get;
            set;
        }

        /// <summary>
        /// UI value.
        /// </summary>
        [DataMember]
        public string? UIValue
        {
            get;
            set;
        }

        /// <summary>
        /// Value.
        /// </summary>
        [DataMember]
        public int Value
        {
            get;
            set;
        }

        /// <summary>
        /// Mask size.
        /// </summary>
        [DataMember]
        public int MaskSize
        {
            get;
            set;
        }

        /// <summary>
        /// Shift.
        /// </summary>
        [DataMember]
        public int Shift
        {
            get;
            set;
        }       

        /// <summary>
        /// User has modified the parameter.
        /// </summary>
        [IgnoreDataMember]
        [Ignore]
        [JsonIgnore]
        public bool Modified
        {
            get;
            set;
        }       
    }
}
