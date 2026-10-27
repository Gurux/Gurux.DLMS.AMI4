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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>A COSEM object attribute with its current value, access rights, and read state.</summary>
    [Description("A COSEM object attribute with its current value, access rights, and read state.")]
    public class GXAttribute : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXAttribute.</summary>
        [Description("Initializes a new instance of GXAttribute.")]
        public GXAttribute()
        {
        }

        /// <summary>Initializes a new instance of GXAttribute.</summary>
        /// <param name="template">Attribute template.</param>
        [Description("Initializes a new instance of GXAttribute.")]
        public GXAttribute(GXAttributeTemplate? template)
        {
            Template = template;
            Parameters = [];
            Values = [];
            Tasks = [];
            Errors = [];
        }


        /// <summary>Initializes a new instance of GXAttribute.</summary>
        /// <param name="index">Attribute index.</param>
        /// <param name="value">Attribute value.</param>
        [Description("Initializes a new instance of GXAttribute.")]
        public GXAttribute(int index, string? value)
        {
            Template = new GXAttributeTemplate()
            {
                Index = index,
            };
            Value = value;
            Parameters = [];
            Values = [];
            Tasks = [];
            Errors = [];
        }

        /// <summary>Attribute identifier.</summary>
        [DataMember]
        [Description("Attribute identifier.")]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>COSEM object to which this attribute belongs.</summary>
        [Description("COSEM object to which this attribute belongs.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXObject? Object { get; set; }

        /// <summary>Template defining the attribute's metadata and access settings.</summary>
        [Description("Template defining the attribute's metadata and access settings.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Index(false)]
        [IsRequired]
        [Filter(FilterType.Exact)]
        public GXAttributeTemplate? Template { get; set; }

        /// <summary>
        /// Attribute module settings.
        /// </summary>
        [Description("Attribute module settings.")]
        [DataMember]
        public string? Settings { get; set; }

        /// <summary>
        /// Latest read value.
        /// </summary>
        [DataMember]
        [Description("Latest read value.")]
        [Filter(FilterType.Contains)]
        public string? Value { get; set; }

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
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

        /// <summary>Whether the edited attribute value is pending a write to the meter; not persisted or serialized.</summary>
        [Description("Whether the edited attribute value is pending a write to the meter; not persisted or serialized.")]
        [IgnoreDataMember]
        [Ignore]
        [JsonIgnore]
        public bool Modified { get; set; }

        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [Description("Version token used by the database to detect conflicting updates.")]
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>Time when the value was last read.</summary>
        /// <remarks>
        /// Profile generic uses this when buffer is read.
        /// </remarks>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        [Description("Time when the value was last read.")]
        public DateTimeOffset? Read { get; set; }

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
        /// Last exception. Successful read nulls this.
        /// </summary>
        [DataMember]
        [Description("Last exception. Successful read nulls this.")]
        [Filter(FilterType.Contains)]
        public string? Exception { get; set; }

        /// <summary>Controls how often the value is read from the meter. Null reads the value on every request; DateTime.MaxValue reads it only once.</summary>
        [Description("Controls how often the value is read from the meter. Null reads the value on every request; DateTime.MaxValue reads it only once.")]
        [DataMember]
        public DateTimeOffset? ExpirationTime { get; set; }

        /// <summary>Time when the record was marked as removed.</summary>
        [Description("Time when the record was marked as removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>
        /// Attribute parameters.
        /// </summary>
        [Description("Attribute parameters.")]
        [DataMember]
        [ForeignKey]
        [Filter(FilterType.Contains)]
        public List<GXAttributeProperty>? Parameters { get; set; }

        /// <summary>
        /// Attribute historical values.
        /// </summary>
        [Description("Attribute historical values.")]
        [DataMember]
        [ForeignKey]
        [Filter(FilterType.Contains)]
        public List<GXValue>? Values { get; set; }

        /// <summary>
        /// Executed tasks.
        /// </summary>
        [Description("Executed tasks.")]
        [DataMember]
        [ForeignKey(typeof(GXTask))]
        [Filter(FilterType.Contains)]
        public List<GXTask>? Tasks { get; set; }

        /// <summary>
        /// Attribute errors.
        /// </summary>
        [Description("Attribute errors.")]
        [DataMember, ForeignKey(typeof(GXAttributeError))]
        [Filter(FilterType.Contains)]
        [JsonIgnore]
        public List<GXAttributeError>? Errors { get; set; }

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
            StringBuilder sb = new();
            if (Template != null)
            {
                sb.Append("#" + Template.Index + ". " + Template.Name);
            }
            if (!string.IsNullOrEmpty(Exception))
            {
                sb.Append(" " + Exception);
            }
            else
            {
                sb.Append(" " + Value);
            }
            if (Read != null)
            {
                sb.Append(" " + Read);
            }
            if (sb.Length == 0)
            {
                return nameof(GXAttribute);
            }
            return sb.ToString();
        }
    }
}
