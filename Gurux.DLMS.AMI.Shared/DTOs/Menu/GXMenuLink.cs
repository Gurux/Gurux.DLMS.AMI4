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
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Menu
{
    /// <summary>A menu entry pointing to a URL or component content.</summary>
    [Description("A menu entry pointing to a URL or component content.")]
    [System.Text.Json.Serialization.JsonUnmappedMemberHandling(System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow)]
    public class GXMenuLink : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXMenuLink.</summary>
        [Description("Initializes a new instance of GXMenuLink.")]
        public GXMenuLink()
        {
        }

        /// <summary>Initializes a new instance of GXMenuLink.</summary>
        /// <remarks>
        /// This constructor is called when a new content type is created. It will create all required collections.
        /// </remarks>
        /// <param name="name">Menu link name.</param>
        [Description("Initializes a new instance of GXMenuLink.")]
        public GXMenuLink(string? name)
        {
            Enabled = true;
            Name = name;
            Visibility = 2;//Visibility.All
        }

        /// <summary>Menu link identifier.</summary>
        [Description("Menu link identifier.")]
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// Menu link name.
        /// </summary>
        [Description("Menu link name.")]
        [StringLength(128)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        public string? Name { get; set; }

        /// <summary>
        /// Menu link description.
        /// </summary>
        [Description("Menu link description.")]
        public string? Description { get; set; }

        /// <summary>
        /// Menu link target type.
        /// </summary>
        /// <remarks>
        /// Link can be HTML link or link to Blazor content.
        /// </remarks>
        /// <seealso cref="TargetType"/>
        [Description("Menu link target type.")]
        public MenuLinkType? MenuLinkType { get; set; }

        /// <summary>
        /// Menu link order index.
        /// </summary>
        [Description("Menu link order index.")]
        public int Order { get; set; }

        /// <summary>Whether the menu link is enabled.</summary>
        [Description("Whether the menu link is enabled.")]
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }

        /// <summary>Load this link during client initialization when access is allowed; the parent menu also enables preloading.</summary>
        [Description("Load this link during client initialization when access is allowed; the parent menu also enables preloading.")]
        [DataMember, IsRequired, DefaultValue(false), Filter(FilterType.Exact, false)]
        public bool? Preload { get; set; } = false;

        ///<summary>
        ///Used icon.
        ///</summary>
        [Description("Used icon.")]
        [DefaultValue(null)]
        public string? Icon { get; set; }

        /// <summary>
        /// Menu link url.
        /// </summary>
        [Description("Menu link url.")]
        [StringLength(128)]
        [IsRequired]
        public string? Url { get; set; }

        /// <summary>
        /// Menu link target type name.
        /// </summary>
        /// <remarks>
        /// Target type name is used when target is blazor component.
        /// </remarks>
        /// <seealso cref="MenuLinkType"/>
        [Description("Menu link target type name.")]
        [StringLength(128)]
        public string? TargetType { get; set; }

        /// <summary>
        /// Parent menu.
        /// </summary>
        [Description("Parent menu.")]
        [DataMember]
        [DefaultValue(null)]
        [ForeignKey(typeof(GXMenu), OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXMenu? Menu { get; set; }

        /// <summary>The configuration opened by this link; its settings are loaded separately.</summary>
        [Description("The configuration opened by this link; its settings are loaded separately.")]
        [DataMember, DefaultValue(null), ForeignKey(typeof(GXConfiguration), OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        public GXConfiguration? Configuration { get; set; }

        /// <summary>Named visibility policy; empty means public within the surrounding resource.</summary>
        [Description("Named visibility policy; empty means public within the surrounding resource.")]
        [DataMember, Ignore(IgnoreType.Db)]
        public List<string>? Policies { get; set; }

        /// <summary>
        /// Parent menu link.
        /// </summary>
        [Description("Parent menu link.")]
        [DefaultValue(null)]
        [ForeignKey(typeof(GXMenuLink), OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        public GXMenuLink? Parent { get; set; }

        /// <summary>
        /// Gets or sets the visibility level for the associated element.
        /// </summary>
        /// <remarks>A value of 0 typically indicates that the element is always visible, while higher
        /// values may represent different visibility states depending on the application's logic. The meaning of each
        /// value should be defined by the consuming application.</remarks>
        [Description("Gets or sets the visibility level for the associated element.")]
        [DefaultValue(0)]
        [DataMember, IsRequired]
        public byte? Visibility { get; set; }

        /// <summary>
        /// Gets or sets the title displayed on the badge associated with this entity.
        /// </summary>
        [Description("Gets or sets the title displayed on the badge associated with this entity.")]
        [StringLength(128)]
        public string? PadgeTitle { get; set; }

        /// <summary>
        /// Gets or sets the comma separated list of padges value associated with the entity.
        /// </summary>
        [Description("Gets or sets the comma separated list of padges value associated with the entity.")]
        [StringLength(128)]
        public string? Padges { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when this menu link was removed; <see langword="null"/> indicates an active link.</summary>
        /// <remarks>
        /// In filter if the removed time is set it will return values that are not null.
        /// </remarks>
        [Description("Time when this menu link was removed; null indicates an active link.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>Time when this menu link was last updated.</summary>
        [Description("Time when this menu link was last updated.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }


        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [Description("Version token used by the database to detect conflicting updates.")]
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>
        /// Indicates whether the entity is defined and managed by the system.
        /// </summary>
        /// <remarks>
        /// When set to true, the entity is protected from user modifications,
        /// including editing and deletion. System-defined entities are controlled
        /// by the application and are required for core functionality.
        /// </remarks>
        [Description("Indicates whether the entity is defined and managed by the system.")]
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? SystemDefined { get; set; }

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
            string? str = Name;
            if (string.IsNullOrEmpty(str))
            {
                str = nameof(GXMenuLink);
            }
            return str;
        }
    }
}
