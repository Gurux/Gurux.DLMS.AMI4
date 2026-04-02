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

namespace Gurux.DLMS.AMI.Shared.DTOs.Menu
{
    /// <summary>
    /// Gurux DLMS AMI Menu.
    /// </summary>
    public class GXMenu : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXMenu()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constuctor is called when a new menu is created. It will create all needed lists.
        /// </remarks>
        /// <param name="name">Menu name.</param>
        public GXMenu(string? name)
        {
            Active = true;
            Name = name;
            MenuGroups = new List<GXMenuGroup>();
            Links = new List<GXMenuLink>();
        }

        /// <summary>
        /// Menu ID.
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
        /// Menu order index.
        /// </summary>
        public int Order
        {
            get;
            set;
        }

        /// <summary>
        /// Is the menu active.
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
        /// The menu creator.
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
        /// Menu Title.
        /// </summary>
        [StringLength(128)]
        public string? Title
        {
            get;
            set;
        }

        /// <summary>
        /// Menu Name.
        /// </summary>
        [StringLength(128)]
        [Index(true)]
        [Filter(FilterType.Contains)]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Menu description.
        /// </summary>
        public string? Description
        {
            get;
            set;
        }

        /// <summary>
        /// List of required roles to see this content.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXRole), typeof(GXMenuRole))]
        public List<GXRole>? Roles
        {
            get;
            set;
        }

        /// <summary>
        /// List of menu groups where this menu belongs.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXMenuGroup), typeof(GXMenuGroupMenu))]
        [Filter(FilterType.Contains)]
        public List<GXMenuGroup>? MenuGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Menu links.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXMenuLink))]
        [Filter(FilterType.Contains)]
        public List<GXMenuLink>? Links
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
        /// Time when menu was removed.
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
        /// When the menu is updated for the last time.
        /// </summary>
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// When menu is published.
        /// </summary>
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
        /// Parent menu.
        /// </summary>
        [DefaultValue(null)]
        [ForeignKey(typeof(GXMenu), OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        public GXMenu? Parent
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
                str = nameof(GXMenu);
            }
            return str;
        }
    }
}
