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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.ContentType
{
    /// <summary>
    /// Gurux DLMS AMI content type.
    /// </summary>
    public class GXContentType : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXContentType()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constuctor is called when a new content type is created. It will create all needed lists.
        /// </remarks>
        /// <param name="name">Content type name.</param>
        public GXContentType(string? name)
        {
            Active = true;
            Name = name;
            ContentTypeGroups = new List<GXContentTypeGroup>();
            Fields = new List<GXContentTypeField>();
        }

        /// <summary>
        /// Content type ID.
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
        /// Is the content type active.
        /// </summary>
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Active { get; set; }

        /// <summary>
        /// Is the content type enumeration.
        /// </summary>
        /// <remarks>
        /// Enumeration is used to give user to list of options.
        /// </remarks>
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enumeration { get; set; }

        /// <summary>
        /// The content type creator.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXUser? Creator
        {
            get;
            set;
        }

        /// <summary>
        /// The name of the content type.
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
        /// Content path name.
        /// </summary>
        [StringLength(128)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        public string? Path
        {
            get;
            set;
        }

        /// <summary>
        /// ContentType description.
        /// </summary>
        public string? Description
        {
            get;
            set;
        }

        /// <summary>
        /// Can user close the content.
        /// </summary>
        /// <remarks>
        /// If content is closed it's not shown for the user.
        /// </remarks>
        /// <seealso cref="PublishTime"/>
        [DefaultValue(ContentVisibility.None)]
        [IsRequired]
        [Filter(FilterType.Exact)]
        public ContentVisibility? Visibility
        {
            get;
            set;
        }

        /// <summary>
        /// List of content type groups where this content type belongs.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXContentTypeGroup), typeof(GXContentTypeGroupContentType))]
        [Filter(FilterType.Contains)]
        public List<GXContentTypeGroup>? ContentTypeGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Target type.
        /// </summary>
        [DataMember]
        [Ignore(IgnoreType.Db)]
        public string? Type
        {
            get;
            set;
        }

        /// <summary>
        /// Content type fields.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXContentTypeField))]
        [Filter(FilterType.Contains)]
        [DefaultValue(null)]
        public List<GXContentTypeField>? Fields
        {
            get;
            set;
        }

        /// <summary>
        /// Content type menu.
        /// </summary>
        [DefaultValue(null)]
        [ForeignKey(typeof(GXMenu), OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        public GXMenu? Menu
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
        /// Time when content type was removed.
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
        /// When the content type is updated for the last time.
        /// </summary>
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// When content type is published to the user.
        /// </summary>
        /// <seealso cref="Visibility"/>
        [DataMember]
        [DefaultValue(null)]
        public DateTimeOffset? PublishTime
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
            if (Active == true)
            {
                str += ", Active";
            }
            if (string.IsNullOrEmpty(str))
            {
                str = nameof(ContentType);
            }
            return str;
        }
    }
}
