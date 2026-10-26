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
    public class GXAttribute : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXAttribute()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="template">Attribute template.</param>
        public GXAttribute(GXAttributeTemplate? template)
        {
            Template = template;
            Parameters = new();
            Values = new();
            Tasks = new();
            Errors = new List<GXAttributeError>();
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="index">Attribute index.</param>
        /// <param name="value">Attribute value.</param>
        public GXAttribute(int index, string? value)
        {
            Template = new GXAttributeTemplate()
            {
                Index = index,
            };
            Value = value;
            Parameters = new();
            Values = new();
            Tasks = new();
            Errors = new List<GXAttributeError>();
        }

        /// <summary>
        /// Attribute Id.
        /// </summary>
        [DataMember]
        [Description("Attribute Id.")]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>COSEM object to which this attribute belongs.</summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXObject? Object { get; set; }

        /// <summary>Template defining the attribute's metadata and access settings.</summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Index(false)]
        [IsRequired]
        [Filter(FilterType.Exact)]
        public GXAttributeTemplate? Template { get; set; }

        /// <summary>
        /// Attribute module settings.
        /// </summary>
        [DataMember]
        public string? Settings { get; set; }

        /// <summary>
        /// Latest read value.
        /// </summary>
        [DataMember]
        [Description("Latest read value")]
        [Filter(FilterType.Contains)]
        public string? Value { get; set; }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// When value is last updated.
        /// </summary>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

        /// <summary>Whether the edited attribute value is pending a write to the meter; not persisted or serialized.</summary>
        [IgnoreDataMember]
        [Ignore]
        [JsonIgnore]
        public bool Modified { get; set; }

        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>
        /// When value is read last time.
        /// </summary>
        /// <remarks>
        /// Profile generic uses this when buffer is read.
        /// </remarks>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        [Description("When value is read last time.")]
        public DateTimeOffset? Read { get; set; }

        /// <summary>
        /// When the object's attribute were last written.
        /// </summary>
        [DataMember]
        [Description("When the object's attribute were last written.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? LastWrite { get; set; }

        /// <summary>
        /// When the object's actions were last invoked.
        /// </summary>
        [DataMember]
        [Description("When the object's actions were last invoked.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? LastAction { get; set; }

        /// <summary>
        /// When the last error was occurred.
        /// </summary>
        [DataMember]
        [Description("When the last error was occurred.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? LastError { get; set; }

        /// <summary>
        /// Last exception. Successful read nulls this.
        /// </summary>
        [DataMember]
        [Description("Last exception.")]
        [Filter(FilterType.Contains)]
        public string? Exception { get; set; }

        /// <summary>
        /// Expiration time tells how often value needs to read from the meter. If it's null it will read every read. If it's DateTime.Max it's read only once.
        /// </summary>
        [DataMember]
        public DateTimeOffset? ExpirationTime { get; set; }

        /// <summary>
        /// Remove time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>
        /// Attribute parameters.
        /// </summary>
        [DataMember]
        [ForeignKey]
        [Filter(FilterType.Contains)]
        public List<GXAttributeProperty>? Parameters { get; set; }

        /// <summary>
        /// Attribute historical values.
        /// </summary>
        [DataMember]
        [ForeignKey]
        [Filter(FilterType.Contains)]
        public List<GXValue>? Values { get; set; }

        /// <summary>
        /// Executed tasks.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXTask))]
        [Filter(FilterType.Contains)]
        public List<GXTask>? Tasks { get; set; }

        /// <summary>
        /// Attribute errors.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXAttributeError))]
        [Filter(FilterType.Contains)]
        [JsonIgnore]
        public List<GXAttributeError>? Errors { get; set; }

        /// <summary>
        /// Initializes the creation timestamp before insertion if it has not been set.
        /// </summary>
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
        public override void BeforeUpdate()
        {
            Updated = DateTime.Now;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
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
