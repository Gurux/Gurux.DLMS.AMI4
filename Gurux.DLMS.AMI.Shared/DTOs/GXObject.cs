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
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>A COSEM object belonging to a device, with its template, attributes, and module parameters.</summary>
    [Description("A COSEM object belonging to a device, with its template, attributes, and module parameters.")]
    [IndexCollection(true, nameof(Device), nameof(Template))]
    public class GXObject : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXObject.</summary>
        [Description("Initializes a new instance of GXObject.")]
        public GXObject()
        {
        }

        /// <summary>Initializes a new instance of GXObject.</summary>
        /// <param name="template">Template associated with the object, or null when not assigned.</param>
        [Description("Initializes a new instance of GXObject.")]
        public GXObject(GXObjectTemplate? template)
        {
            Template = template;
            Attributes = [];
            Properties = [];
            Tasks = [];
            Errors = [];
        }

        /// <summary>Initializes a new instance of GXObject.</summary>
        /// <param name="objectType">COSEM interface class identifier for the new object template.</param>
        /// <param name="logicalName">Logical name in OBIS notation, or null when not assigned.</param>
        [Description("Initializes a new instance of GXObject.")]
        public GXObject(int objectType, string? logicalName)
        {
            Template = new GXObjectTemplate()
            {
                LogicalName = logicalName,
                ObjectType = objectType,
            };
            Attributes = [];
            Properties = [];
            Tasks = [];
            Errors = [];
        }

        /// <summary>Object identifier.</summary>
        [Description("Object identifier.")]
        [DataMember]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>Device that owns this COSEM object.</summary>
        [Description("Device that owns this COSEM object.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [Filter(FilterType.Exact)]
        public GXDevice? Device { get; set; }


        /// <summary>
        /// Object Template.
        /// </summary>
        [Description("Object Template.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Index(false)]
        [Filter(FilterType.Exact)]
        public GXObjectTemplate? Template { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when the object was last updated.</summary>
        [DataMember]
        [Description("Time when the object was last updated.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }


        /// <summary>
        /// Object is created only when needed.
        /// </summary>
        /// <remarks>
        /// If true, the object is not created for the database.
        /// It's created when needed for the first time.
        /// This makes faster to add new devices for the database.
        /// </remarks>
        [Description("Object is created only when needed.")]
        [Ignore]
        public bool Latebind { get; set; }

        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [Description("Version token used by the database to detect conflicting updates.")]
        [DataMember]
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
        /// Object properties.
        /// </summary>
        [Description("Object properties.")]
        [DataMember]
        [ForeignKey]
        [Filter(FilterType.Contains)]
        public List<GXObjectProperty>? Properties { get; set; }

        /// <summary>Time when the object attributes were last read.</summary>
        [DataMember]
        [Description("Time when the object attributes were last read.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? LastRead { get; set; }

        /// <summary>Time when the object attributes were last written.</summary>
        [DataMember]
        [Description("Time when the object attributes were last written.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? LastWrite { get; set; }

        /// <summary>Time when the object actions were last invoked.</summary>
        [DataMember]
        [Description("Time when the object actions were last invoked.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? LastAction { get; set; }

        /// <summary>Time when the most recent error occurred.</summary>
        [DataMember]
        [Description("Time when the most recent error occurred.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? LastError { get; set; }

        /// <summary>
        /// Last error message.
        /// </summary>
        [DataMember]
        [Description("Last error message.")]
        [IsRequired]
        public string? LastErrorMessage { get; set; }

        /// <summary>
        /// Attributes.
        /// </summary>
        [Description("Attributes.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Contains)]
        public List<GXAttribute>? Attributes { get; set; }

        /// <summary>
        /// Executed tasks.
        /// </summary>
        [Description("Executed tasks.")]
        [DataMember]
        [ForeignKey(typeof(GXTask))]
        [Filter(FilterType.Contains)]
        public List<GXTask>? Tasks { get; set; }

        /// <summary>
        /// Object errors.
        /// </summary>
        [Description("Object errors.")]
        [DataMember, ForeignKey(typeof(GXObjectError))]
        [Filter(FilterType.Contains)]
        public List<GXObjectError>? Errors { get; set; }

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

        /// <inheritdoc />
        public override string ToString()
        {
            if (Template != null)
            {
                if (Template.ShortName != 0)
                {
                    return Template.ShortName + " " + Template.Name;
                }
                return Template.LogicalName + " " + Template.Name;
            }
            return nameof(GXObject);
        }
    }
}
