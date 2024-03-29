﻿//
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
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Attribute template table.
    /// </summary>
    public class GXAttributeTemplate : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXAttributeTemplate()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXAttributeTemplate(string? name)
        {
            Name = name;
            ListItems = new List<GXAttributeListItem>();
        }

        /// <summary>
        /// Attribute template Id.
        /// </summary>
        [DataMember]
        [Description("Attribute template Id.")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Object template.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [IsRequired]
        [Filter(FilterType.Exact)]
        public GXObjectTemplate? ObjectTemplate
        {
            get;
            set;
        }

        /// <summary>
        /// Attribute index.
        /// </summary>
        [DataMember]
        [Filter(FilterType.Exact, 0)]
        public int Index
        {
            get;
            set;
        }

        /// <summary>
        /// Object attribute name.
        /// </summary>
        [DataMember]
        [StringLength(64)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [DefaultValue(null)]
        [IsRequired]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Attribute enumerated values.
        /// </summary>
        [DataMember]
        [ForeignKey]
        public List<GXAttributeListItem>? ListItems
        {
            get;
            set;
        }

        /// <summary>
        /// Expiration time tells how often value needs to read from the meter. If it's null it will read every read. If it's DateTime.Max it's read only once.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        public DateTimeOffset? ExpirationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// When value is last updated.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// User has modified the attribute templates.
        /// </summary>
        [IgnoreDataMember]
        [Ignore]
        [JsonIgnore]
        public bool Modified
        {
            get;
            set;
        }

        /// <summary>
        /// Concurrency stamp.
        /// </summary>
        /// <remarks>
        /// Concurrency stamp is used to verify that several user's can't 
        /// modify the target at the same time.
        /// </remarks>
        [DataMember]
        [DefaultValue(null)]
        [StringLength(36)]
        public string? ConcurrencyStamp
        {
            get;
            set;
        }

        /// <summary>
        /// Remove time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed
        {
            get;
            set;
        }

        /// <summary>
        /// Access level.
        /// </summary>
        [DataMember]
        [Description("Access level.")]
        [DefaultValue(0)]
        public int AccessLevel
        {
            get;
            set;
        }

        /// <summary>
        /// Data type.
        /// </summary>
        [DataMember]
        [Description("Data type.")]
        [DefaultValue(0)]
        public int DataType
        {
            get;
            set;
        }

        /// <summary>
        /// UI Data type.
        /// </summary>
        [DataMember]
        [Description("UI Data type.")]
        [DefaultValue(0)]
        public int UIDataType
        {
            get;
            set;
        }

        /// <summary>
        /// Attribute weight.
        /// </summary>
        /// <remarks>
        /// Attribute weight can be used to ask to execute tasks in given order.
        /// </remarks>
        [DataMember]
        [DefaultValue(0)]
        public int Weight
        {
            get;
            set;
        }

        /// <summary>
        /// Default value.
        /// </summary>
        [DataMember]
        [Description("Default value")]
        [Filter(FilterType.Contains)]
        public string? DefaultValue
        {
            get;
            set;
        }

        /// <summary>
        /// Optional scaler is used to multiple the value with the given scaler.
        /// </summary>
        [DataMember]
        [Description("Optional scaler is used to multiple the value with the given scaler.")]
        public double? Scaler
        {
            get;
            set;
        }


        /// <summary>
        /// Optional unit is used to describe the unit.
        /// </summary>
        [DataMember]
        [Description("Optional unit is used to describe the unit.")]
        public string? Unit
        {
            get;
            set;
        }

        /// <summary>
        /// Update creation time before update.
        /// </summary>
        public override void BeforeAdd()
        {
            if (CreationTime == DateTime.MinValue)
            {
                CreationTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Update concurrency stamp.
        /// </summary>
        public override void BeforeUpdate()
        {
            Updated = DateTime.Now;
        }
    }
}
