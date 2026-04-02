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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.ContentType;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Content
{
    /// <summary>
    /// Gurux DLMS AMI Content.
    /// </summary>
    public class GXContent : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXContent()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constuctor is called when a new content is created. It will create all needed lists.
        /// </remarks>
        /// <param name="name">Content name.</param>
        public GXContent(string? name)
        {
            Active = true;
            Name = name;
            ContentGroups = [];
            Fields = [];
            Collaborators = [];
        }

        /// <summary>
        /// Content ID.
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
        /// Is the content active.
        /// </summary>
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Active { get; set; }


        /// <summary>
        /// The content cannot be modified once it is closed.
        /// </summary>
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Closed { get; set; }

        /// <summary>
        /// The content creator.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXUser? Creator
        {
            //ForeignKeyDelete is None because Type will handle the deletion.
            get;
            set;
        }

        /// <summary>
        /// Content Name.
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
        /// Url alias.
        /// </summary>
        [Ignore]
        public string? UrlAlias
        {
            get;
            set;
        }

        /// <summary>
        /// Content type.
        /// </summary>
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [Index(false)]
        [DataMember]
        [IsRequired]
        [DefaultValue(null)]
        public GXContentType? Type
        {
            get;
            set;
        }

        /// <summary>
        /// Content fields.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXContentField))]
        [Filter(FilterType.Contains)]
        [DefaultValue(null)]
        public List<GXContentField>? Fields
        {
            get;
            set;
        }

        /// <summary>
        /// Collaborators are users who have access to the content.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXUser), typeof(GXUserContent))]
        [Filter(FilterType.Contains)]
        [DefaultValue(null)]
        public List<GXUser>? Collaborators
        {
            get;
            set;
        }

        /// <summary>
        /// List of content groups where this content belongs.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXContentGroup), typeof(GXContentGroupContent))]
        [Filter(FilterType.Contains)]
        public List<GXContentGroup>? ContentGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Content settings for the user.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXUser), typeof(GXUserContentSettings))]
        [Filter(FilterType.Contains)]
        public GXUser? User
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
        /// Time when content was removed.
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
        /// When the content is updated for the last time.
        /// </summary>
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// When content is published.
        /// </summary>
        /// <seealso cref="EndTime"/>/>
        [DataMember]
        [DefaultValue(null)]
        public DateTimeOffset? PublishTime
        {
            get;
            set;
        }

        /// <summary>
        /// When content is not shown anymore.
        /// </summary>
        /// <seealso cref="PublishTime"/>/>
        [DataMember]
        [DefaultValue(null)]
        public DateTimeOffset? EndTime
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
        /// Parent content.
        /// </summary>
        [DefaultValue(null)]
        [ForeignKey(typeof(GXContent), OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        public GXContent? Parent
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
            if (Active.HasValue && Active.Value)
            {
                str += ", Active";
            }
            if (string.IsNullOrEmpty(str))
            {
                str = nameof(GXContent);
            }
            return str;
        }
    }
}
