// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>The definition of a COSEM attribute shared by objects using the same template.</summary>
    [Description("The definition of a COSEM attribute shared by objects using the same template.")]
    public class GXAttributeTemplate : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXAttributeTemplate.</summary>
        [Description("Initializes a new instance of GXAttributeTemplate.")]
        public GXAttributeTemplate()
        {
        }

        /// <summary>Initializes a new instance of GXAttributeTemplate.</summary>
        /// <param name="name">Display name of the attribute template, or null when not assigned.</param>
        [Description("Initializes a new instance of GXAttributeTemplate.")]
        public GXAttributeTemplate(string? name)
        {
            Name = name;
            ListItems = [];
        }

        /// <summary>Attribute template identifier.</summary>
        [DataMember]
        [Description("Attribute template identifier.")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// Object template.
        /// </summary>
        [Description("Object template.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [IsRequired]
        [Filter(FilterType.Exact)]
        public GXObjectTemplate? ObjectTemplate { get; set; }

        /// <summary>
        /// Attribute index.
        /// </summary>
        [Description("Attribute index.")]
        [DataMember]
        [Filter(FilterType.Exact, 0)]
        public int Index { get; set; }

        /// <summary>
        /// Object attribute name.
        /// </summary>
        [Description("Object attribute name.")]
        [DataMember]
        [StringLength(64)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [DefaultValue(null)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// Attribute enumerated values.
        /// </summary>
        [Description("Attribute enumerated values.")]
        [DataMember]
        [ForeignKey]
        public List<GXAttributeListItem>? ListItems { get; set; }

        /// <summary>Controls how often the value is read from the meter. Null reads the value on every request; DateTime.MaxValue reads it only once.</summary>
        [Description("Controls how often the value is read from the meter. Null reads the value on every request; DateTime.MaxValue reads it only once.")]
        [DataMember]
        [DefaultValue(null)]
        public DateTimeOffset? ExpirationTime { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when the value was last updated.</summary>
        [Description("Time when the value was last updated.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }


        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [Description("Version token used by the database to detect conflicting updates.")]
        [DataMember]
        [DefaultValue(null)]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>Time when the record was marked as removed.</summary>
        [Description("Time when the record was marked as removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>
        /// Access level.
        /// </summary>
        [DataMember]
        [Description("Access level.")]
        [DefaultValue(0)]
        public int AccessLevel { get; set; }

        /// <summary>
        /// Data type.
        /// </summary>
        [DataMember]
        [Description("Data type.")]
        [DefaultValue(0)]
        public int DataType { get; set; }

        /// <summary>
        /// UI Data type.
        /// </summary>
        [DataMember]
        [Description("UI Data type.")]
        [DefaultValue(0)]
        public int UIDataType { get; set; }

        /// <summary>
        /// Attribute weight.
        /// </summary>
        /// <remarks>
        /// Attribute weight can be used to ask to execute tasks in given order.
        /// </remarks>
        [Description("Attribute weight.")]
        [DataMember]
        [DefaultValue(0)]
        public int Weight { get; set; }

        /// <summary>
        /// Default value.
        /// </summary>
        [DataMember]
        [Description("Default value.")]
        [Filter(FilterType.Contains)]
        public string? DefaultValue { get; set; }

        /// <summary>
        /// Optional scaler is used to multiple the value with the given scaler.
        /// </summary>
        [DataMember]
        [Description("Optional scaler is used to multiple the value with the given scaler.")]
        public double? Scaler { get; set; }


        /// <summary>
        /// Optional unit is used to describe the unit.
        /// </summary>
        [DataMember]
        [Description("Optional unit is used to describe the unit.")]
        public string? Unit { get; set; }

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
    }
}
