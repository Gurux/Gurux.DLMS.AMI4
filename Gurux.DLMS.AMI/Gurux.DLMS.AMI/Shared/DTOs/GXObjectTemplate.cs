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
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Object template.
    /// </summary>
    [Description("COSEM object template.")]
    public class GXObjectTemplate : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXObjectTemplate() 
        { 
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXObjectTemplate(string name)
        {
            Name = name;
            Attributes = new List<GXAttributeTemplate>();
        }

        /// <summary>
        /// Object template identifier.
        /// </summary>
        [Description("Object template identifier.")]
        //Filter uses default value.
        [DefaultValue(null)]
        [DataMember]
        public Guid Id
        {
            get;
            set;
        }


        /// <summary>
        /// Device template identifier.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Description("Device template identifier.")]
        [Index(false)]
        public GXDeviceTemplate? DeviceTemplate
        {
            get;
            set;
        }

        /// <summary>
        /// Object type.
        /// </summary>
        [DataMember]
        [Description("Object type.")]
        [DefaultValue(0)]
        public int ObjectType
        {
            get;
            set;
        }


        /// <summary>
        /// Object version.
        /// </summary>
        [DataMember]
        [Description("Object version.")]
        [DefaultValue(0)]
        public int Version
        {
            get;
            set;
        }


        /// <summary>
        /// Object description.
        /// </summary>
        [DataMember]
        [StringLength(128)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Logical name of the object.
        /// </summary>
        [DataMember]
        [StringLength(25)]
        [Filter(FilterType.Contains)]
        [DefaultValue(null)]
        [IsRequired]
        public string? LogicalName
        {
            get;
            set;
        }

        /// <summary>
        /// Short name of the object.
        /// </summary>
        [DataMember]
        [Filter(FilterType.Exact)]
        [DefaultValue(0)]
        public UInt16? ShortName
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
        /// When object is last updated.
        /// </summary>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// User has modified the item.
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
        /// Expiration time tells how often value needs to read from the meter. If it's null it will read every read. If it's DateTime.Max it's read only once.
        /// </summary>
        [DataMember]
        [Description("Expiration time.")]
        [DefaultValue(null)]
        public DateTimeOffset? ExpirationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Attribute templates.
        /// </summary>
        [DataMember]
        [Description("Attribute templates")]
        [ForeignKey(typeof(GXAttributeTemplate))]
        public List<GXAttributeTemplate>? Attributes
        {
            get;
            set;
        }

        /// <summary>
        /// Action access levels.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [StringLength(20)]
        public string? ActionAccessLevels
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

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }
            return nameof(GXObjectTemplate);
        }
    }
}
