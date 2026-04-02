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
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.ContentType
{
    /// <summary>
    /// Gurux DLMS AMI content type field.
    /// </summary>
    public class GXContentTypeField : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXContentTypeField()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constuctor is called when a new content type is created. It will create all needed lists.
        /// </remarks>
        /// <param name="name">Content type field name.</param>
        public GXContentTypeField(string? name)
        {
            Name = name;
            Active = true;
            Required = false;
        }

        /// <summary>
        /// Content type field ID.
        /// </summary>
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Is the content type field active.
        /// </summary>
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Active { get; set; }

        /// <summary>
        /// Is the content type field hidden for the user.
        /// </summary>
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Hidden { get; set; }

        /// <summary>
        /// The content value must be unique.
        /// </summary>
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Unique { get; set; }


        /// <summary>
        /// Responsive determines the order
        /// in which the contents are hidden if they do not fit the screen.
        /// </summary>
        [DefaultValue(0)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public int? Visibility { get; set; }

        /// <summary>
        /// Content type field name.
        /// </summary>
        [StringLength(128)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Used icon.
        /// </summary>
        /// <remarks>
        /// Icon can be SVG or png image.
        /// </remarks>
        [Description("Icon.")]
        public string? Icon
        {
            get;
            set;
        }

        /// <summary>
        /// Is this required value.
        /// </summary>
        [Filter(FilterType.Equals)]
        public bool? Required
        {
            get;
            set;
        }


        /// <summary>
        /// Content type field order index.
        /// </summary>
        public int Order
        {
            get;
            set;
        }

        /// <summary>
        /// Content type field description.
        /// </summary>
        public string? Description
        {
            get;
            set;
        }

        /// <summary>
        /// The field type.
        /// </summary>
        /// <seealso cref="Source"/>
        public FieldType? FieldType
        {
            get;
            set;
        }

        /// <summary>
        /// Source content.
        /// </summary>
        /// <remarks>
        /// Source is set only when FieldType expects that data is search from the contents.
        /// E.g. FieldType is selection list.
        /// </remarks>
        /// <seealso cref="FieldType"/>
        [DefaultValue(null)]
        [ForeignKey(typeof(GXContentType), OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        public GXContentType? Source
        {
            get;
            set;
        }

        /// <summary>
        /// Expected string format.
        /// </summary>
        [StringLength(128)]
        public string? Format
        {
            get;
            set;
        }

        /// <summary>
        /// Default value.
        /// </summary>
        [StringLength(128)]
        public string? Default
        {
            get;
            set;
        }

        /// <summary>
        /// Minimum value.
        /// </summary>
        [StringLength(128)]
        public string? Minimum
        {
            get;
            set;
        }

        /// <summary>
        /// Maximum value.
        /// </summary>
        [StringLength(128)]
        public string? Maximum
        {
            get;
            set;
        }

        /// <summary>
        /// Parent content.
        /// </summary>
        [DefaultValue(null)]
        [ForeignKey(typeof(GXContentType), OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        public GXContentType? Parent
        {
            get;
            set;
        }

        /// <summary>
        /// Content type unit.
        /// </summary>
        [StringLength(128)]
        public string? Unit
        {
            get;
            set;
        }

        /// <summary>
        /// Available values.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXContentTypeFieldValue))]
        [Filter(FilterType.Contains)]
        [DefaultValue(null)]
        public GXContentTypeFieldValue[]? Values
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
        public DateTimeOffset? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Time when content type field was removed.
        /// </summary>
        /// <remarks>
        /// In filter if the removed time is set it will return values that are not null.
        /// </remarks>
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
        /// When the content type field is updated for the last time.
        /// </summary>
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
        /// Concurrency stamp.
        /// </summary>
        /// <remarks>
        /// Concurrency stamp is used to verify that several user's can't 
        /// modify the target at the same time.
        /// </remarks>
        [DataMember]
        [StringLength(36)]
        public string? ConcurrencyStamp
        {
            get;
            set;
        }

        /// <summary>
        /// Update creation time before update.
        /// </summary>
        public override void BeforeAdd()
        {
            if (CreationTime == null)
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
            string? str = Name;
            if (string.IsNullOrEmpty(str))
            {
                str = nameof(GXContentTypeField);
            }
            return str;
        }
    }
}
