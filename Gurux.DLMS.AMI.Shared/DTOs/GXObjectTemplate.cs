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
using Gurux.Service.Orm.Common.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>A reusable COSEM object definition containing attribute templates.</summary>
    [Description("A reusable COSEM object definition containing attribute templates.")]
    public class GXObjectTemplate : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXObjectTemplate.</summary>
        [Description("Initializes a new instance of GXObjectTemplate.")]
        public GXObjectTemplate()
        {
        }

        /// <summary>Initializes a new instance of GXObjectTemplate.</summary>
        /// <param name="name">Display name of the object template, or null when not assigned.</param>
        [Description("Initializes a new instance of GXObjectTemplate.")]
        public GXObjectTemplate(string? name)
        {
            Name = name;
            Attributes = [];
        }

        /// <summary>
        /// Object template identifier.
        /// </summary>
        [Description("Object template identifier.")]
        [DataMember]
        //Filter uses default value.
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }


        /// <summary>Device template containing this object definition.</summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Description("Device template containing this object definition.")]
        [Index(false)]
        [Filter(FilterType.Exact)]
        public GXDeviceTemplate? DeviceTemplate { get; set; }

        /// <summary>
        /// Object type.
        /// </summary>
        [DataMember]
        [Description("Object type.")]
        [IsRequired]
        [Filter(FilterType.Exact)]
        public int? ObjectType { get; set; }


        /// <summary>
        /// Object version.
        /// </summary>
        [DataMember]
        [Description("Object version.")]
        [DefaultValue(0)]
        [IsRequired]
        public int? Version { get; set; }


        /// <summary>Display name of the COSEM object template.</summary>
        [Description("Display name of the COSEM object template.")]
        [DataMember]
        [StringLength(256)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// Logical name of the object.
        /// </summary>
        [Description("Logical name of the object.")]
        [DataMember]
        [StringLength(25)]
        [Filter(FilterType.Contains)]
        [DefaultValue(null)]
        [IsRequired]
        public string? LogicalName { get; set; }

        /// <summary>
        /// Short name of the object.
        /// </summary>
        [Description("Short name of the object.")]
        [DataMember]
        [Filter(FilterType.Exact)]
        [DefaultValue(0)]
        public UInt16? ShortName { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when the object was last updated.</summary>
        [Description("Time when the object was last updated.")]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }



        /// <summary>Time when the record was marked as removed.</summary>
        [Description("Time when the record was marked as removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>Controls how often the value is read from the meter. Null reads the value on every request; DateTime.MaxValue reads it only once.</summary>
        [DataMember]
        [Description("Controls how often the value is read from the meter. Null reads the value on every request; DateTime.MaxValue reads it only once.")]
        [DefaultValue(null)]
        public DateTimeOffset? ExpirationTime { get; set; }

        /// <summary>
        /// Attribute templates.
        /// </summary>
        [DataMember]
        [Description("Attribute templates.")]
        [ForeignKey(typeof(GXAttributeTemplate))]
        public List<GXAttributeTemplate>? Attributes { get; set; }

        /// <summary>
        /// Action access levels.
        /// </summary>
        [Description("Action access levels.")]
        [DataMember]
        [DefaultValue(null)]
        [StringLength(20)]
        public string? ActionAccessLevels { get; set; }

        /// <summary>
        /// Initializes the creation timestamp before insertion if it has not been set.
        /// </summary>
        [Description("Initializes the creation timestamp before insertion if it has not been set.")]
        public override void BeforeAdd()
        {
            if (CreationTime == null)
            {
                CreationTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Updates the last-modified timestamp before persisting changes.
        /// </summary>
        [Description("Updates the last-modified timestamp before persisting changes.")]
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
