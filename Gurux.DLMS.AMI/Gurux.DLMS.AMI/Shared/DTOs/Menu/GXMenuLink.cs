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
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Menu
{
    /// <summary>
    /// Gurux DLMS AMI menu link.
    /// </summary>
    public class GXMenuLink : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXMenuLink()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constuctor is called when a new content type is created. It will create all needed lists.
        /// </remarks>
        /// <param name="name">Menu link name.</param>
        public GXMenuLink(string? name)
        {
            Active = true;
            Name = name;
        }

        /// <summary>
        /// Menu link ID.
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
        /// Menu link name.
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
        /// Menu link description.
        /// </summary>
        public string? Description
        {
            get;
            set;
        }

        /// <summary>
        /// Menu link target type.
        /// </summary>
        /// <remarks>
        /// Link can be HTML link or link to Blazor content.
        /// </remarks>
        /// <seealso cref="TargetType"/>
        public MenuLinkType? MenuLinkType
        {
            get;
            set;
        }

        /// <summary>
        /// Menu link order index.
        /// </summary>
        public int Order
        {
            get;
            set;
        }

        /// <summary>
        /// Is the menu link active.
        /// </summary>
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Active { get; set; }

        ///<summary>
        ///Used icon.
        ///</summary>
        [DefaultValue(null)]
        public string? Icon { get; set; }

        /// <summary>
        /// Menu link url.
        /// </summary>
        [StringLength(128)]
        [IsRequired]
        public string? Url
        {
            get;
            set;
        }

        /// <summary>
        /// Menu link target type name.
        /// </summary>
        /// <remarks>
        /// Target type name is used when target is blazor component.
        /// </remarks>
        /// <seealso cref="MenuLinkType"/>
        [StringLength(128)]
        public string? TargetType
        {
            get;
            set;
        }

        /// <summary>
        /// Parent menu.
        /// </summary>
        [DefaultValue(null)]
        [ForeignKey(typeof(GXMenu), OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXMenu? Menu
        {
            get;
            set;
        }

        /// <summary>
        /// List of required roles to see this content.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXRole), typeof(GXMenuLinkRole))]
        public List<GXRole>? Roles
        {
            get;
            set;
        }

        /// <summary>
        /// Parent menu link.
        /// </summary>
        [DefaultValue(null)]
        [ForeignKey(typeof(GXMenuLink), OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        public GXMenuLink? Parent
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the visibility level for the associated element.
        /// </summary>
        /// <remarks>A value of 0 typically indicates that the element is always visible, while higher
        /// values may represent different visibility states depending on the application's logic. The meaning of each
        /// value should be defined by the consuming application.</remarks>
        [DefaultValue(0)]
        public byte? Visibility
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the title displayed on the badge associated with this entity.
        /// </summary>
        [StringLength(128)]
        public string? PadgeTitle
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the comma separated list of padges value associated with the entity.
        /// </summary>
        [StringLength(128)]
        public string? Padges
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
                str = nameof(ContentType);
            }
            return str;
        }
    }
}
