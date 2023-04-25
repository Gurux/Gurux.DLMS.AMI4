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
using Gurux.Common.Db;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// UI Blocks.
    /// </summary>
    public class GXBlock : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXBlock()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constuctor is called when a new device is created. It will create all needed lists.
        /// </remarks>
        /// <param name="name">Block name.</param>
        public GXBlock(string? name)
        {
            Active = true;
            Name = name;
            BlockGroups = new List<GXBlockGroup>();
        }
        /// <summary>
        /// Block ID.
        /// </summary>
        [DataMember(Name = "ID")]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Block Title.
        /// </summary>
        [StringLength(128)]
        public string? Title
        {
            get;
            set;
        }

        /// <summary>
        /// Block Name.
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
        /// Block type.
        /// </summary>
        public BlockType BlockType
        {
            get;
            set;
        }

        /// <summary>
        /// GXComponent view that this block uses.
        /// </summary>
        [DefaultValue(null)]
        [ForeignKey(typeof(GXComponentView), OnDelete = ForeignKeyDelete.Cascade)]
        public GXComponentView? ComponentView
        {
            get;
            set;
        }


        /// <summary>
        /// Script method that this block uses.
        /// </summary>
        [DefaultValue(null)]
        [ForeignKey(typeof(GXScriptMethod), OnDelete = ForeignKeyDelete.Cascade)]
        public GXScriptMethod? ScriptMethod
        {
            get;
            set;
        }

        /// <summary>
        /// CSS class name.
        /// </summary>
        [StringLength(128)]
        [DefaultValue(null)]
        public string? CssClass
        {
            get;
            set;
        }

        /// <summary>
        /// CSS role name.
        /// </summary>
        [StringLength(128)]
        [DefaultValue(null)]
        public string? CssRole
        {
            get;
            set;
        }

        /// <summary>
        /// CSS role name.
        /// </summary>
        [StringLength(256)]
        [DefaultValue(null)]
        public string? Style
        {
            get;
            set;
        }

        /// <summary>
        /// HTML Body or component settings are saved here.
        /// </summary>
        public string? Body
        {
            get;
            set;
        }

        /// <summary>
        /// Pages where block is shown.
        /// </summary>
        public BlockShown Shown
        {
            get;
            set;
        }


        /// <summary>
        /// Logaction where block is shown.
        /// </summary>
        /// <seealso cref="LocationOrder"/>
        public BlockLocation Location
        {
            get;
            set;
        }

        /// <summary>
        /// Locaction order index.
        /// </summary>
        /// <seealso cref="Location"/>
        public int LocationOrder
        {
            get;
            set;
        }


        /// <summary>
        /// List of pages where block is shown.
        /// </summary>
        public string? Pages
        {
            get;
            set;
        }

        /// <summary>
        /// Is block active.
        /// </summary>
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Active { get; set; }

        /// <summary>
        /// Can user close the block.
        /// </summary>
        /// <remarks>
        /// If block is closed it's not shown for the user.
        /// </remarks>
        [DefaultValue(false)]
        public bool Closable
        {
            get;
            set;
        }

        /// <summary>
        /// List of block groups where this block belongs.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXBlockGroup), typeof(GXBlockGroupBlock))]
        [Filter(FilterType.Contains)]
        public List<GXBlockGroup>? BlockGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Block settings for the user.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXUser), typeof(GXUserBlockSettings))]
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
        public DateTime? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Time when block was removed.
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
        /// When the block is updated for the last time.
        /// </summary>
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// When block is published.
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
        /// When block is not shown anymore.
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
        /// Localized strings for this block.
        /// </summary>
        /// <remarks>
        /// This is used only for database and it's not send for the user.
        /// </remarks>
        [DataMember]
        [JsonIgnore]
        public GXLocalizedResource[]? Resources
        {
            get;
            set;
        }

        /// <summary>
        /// Localized resources for this block.
        /// </summary>
        /// <remarks>
        /// Localized resources are return with this.
        /// </remarks>
        [DataMember]
        [Ignore(IgnoreType.Db)]
        public GXLanguage[]? Languages
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
            string str = Name;
            if (Active.HasValue && Active.Value)
            {
                str += ", Active";
            }
            if (Closable)
            {
                str += ", Closable";
            }
            return str;
        }
    }
}
