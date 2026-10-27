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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Menu
{
    /// <summary>A menu definition whose links are presented as navigation, tabs, or configuration entries.</summary>
    [Description("A menu definition whose links are presented as navigation, tabs, or configuration entries.")]
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class GXMenu : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXMenu.</summary>
        [Description("Initializes a new instance of GXMenu.")]
        public GXMenu()
        {
        }

        /// <summary>Initializes a new instance of GXMenu.</summary>
        /// <remarks>
        /// This constructor is called when a new menu is created. It will create all required collections.
        /// </remarks>
        /// <param name="name">Menu name.</param>
        [Description("Initializes a new instance of GXMenu.")]
        public GXMenu(string? name)
        {
            Enabled = true;
            Name = name;
            MenuGroups = [];
            Links = [];
        }

        /// <summary>Menu identifier.</summary>
        [Description("Menu identifier.")]
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// Menu order index.
        /// </summary>
        [Description("Menu order index.")]
        public int Order { get; set; }

        /// <summary>Optional layout region; null preserves placement defined by the containing view.</summary>
        [Description("Optional layout region; null preserves placement defined by the containing view.")]
        [DataMember, DefaultValue(null), Filter(FilterType.Exact), EnumDataType(typeof(MenuLocation))]
        public MenuLocation? Location { get; set; }

        /// <summary>The presentation purpose of this menu.</summary>
        [Description("The presentation purpose of this menu.")]
        [DataMember, IsRequired, DefaultValue(MenuKind.Navigation), Filter(FilterType.Exact, MenuKind.All)]
        public MenuKind Kind { get; set; } = MenuKind.Navigation;

        /// <summary>Whether the menu is enabled.</summary>
        [Description("Whether the menu is enabled.")]
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }

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

        ///<summary>
        ///Used icon.
        ///</summary>
        [Description("Used icon.")]
        [DefaultValue(null)]
        public string? Icon { get; set; }

        /// <summary>
        /// The menu creator.
        /// </summary>
        [Description("The menu creator.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Menu Title.
        /// </summary>
        [Description("Menu Title.")]
        [StringLength(128)]
        public string? Title { get; set; }

        /// <summary>
        /// Menu Name.
        /// </summary>
        [Description("Menu Name.")]
        [StringLength(128)]
        [Index(true)]
        [Filter(FilterType.Contains)]
        public string? Name { get; set; }

        /// <summary>
        /// Menu description.
        /// </summary>
        [Description("Menu description.")]
        public string? Description { get; set; }


        /// <summary>
        /// Named visibility policy; empty means public within the surrounding resource.
        /// </summary>
        [Description("Named visibility policy; empty means public within the surrounding resource.")]
        [DataMember, Ignore(IgnoreType.Db)]
        public List<string>? Policies { get; set; }

        /// <summary>Menu groups containing this menu.</summary>
        [Description("Menu groups containing this menu.")]
        [DataMember, ForeignKey(typeof(GXMenuGroup), typeof(GXMenuGroupMenu))]
        [Filter(FilterType.Contains)]
        public List<GXMenuGroup>? MenuGroups { get; set; }

        /// <summary>
        /// Menu links.
        /// </summary>
        [Description("Menu links.")]
        [DataMember, ForeignKey(typeof(GXMenuLink))]
        [Filter(FilterType.Contains)]
        public List<GXMenuLink>? Links { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when menu was removed.
        /// </summary>
        /// <remarks>
        /// In filter if the removed time is set it will return values that are not null.
        /// </remarks>
        [Description("Time when menu was removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>Time when the menu was last updated.</summary>
        [Description("Time when the menu was last updated.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

        /// <summary>
        /// When menu is published.
        /// </summary>
        [Description("When menu is published.")]
        [DataMember]
        [DefaultValue(null)]
        public DateTimeOffset? PublishTime { get; set; }


        /// <summary>
        /// Parent menu.
        /// </summary>
        [Description("Parent menu.")]
        [DataMember]
        [DefaultValue(null)]
        [ForeignKey(typeof(GXMenu), OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        public GXMenu? Parent { get; set; }

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
            if (Enabled.HasValue && Enabled.Value)
            {
                str += ", Enabled";
            }
            if (string.IsNullOrEmpty(str))
            {
                str = nameof(GXMenu);
            }
            return str;
        }
        /// <summary>
        /// Load this resource during client initialization when access is allowed.
        /// </summary>
        [Description("Load this resource during client initialization when access is allowed.")]
        [DataMember, IsRequired, DefaultValue(false), Filter(FilterType.Exact, false)]
        public bool? Preload { get; set; }
    }
}
