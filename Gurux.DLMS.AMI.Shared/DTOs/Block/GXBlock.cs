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
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Block
{
    /// <summary>A configurable content block displayed at a selected location in the user interface.</summary>
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
        /// This constuctor is called when a new block is created. It will create all needed lists.
        /// </remarks>
        /// <param name="name">Block name.</param>
        public GXBlock(string? name)
        {
            Enabled = true;
            Name = name;
            BlockGroups = new List<GXBlockGroup>();
        }

        /// <summary>
        /// Block ID.
        /// </summary>
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// The block creator.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        //ForeignKeyDelete is None because User will handle the deletion.
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Block Title.
        /// </summary>
        [StringLength(128)]
        public string? Title { get; set; }

        /// <summary>
        /// Block Name.
        /// </summary>
        [StringLength(128)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        public string? Name { get; set; }

        /// <summary>
        /// Block type.
        /// </summary>
        public BlockType BlockType { get; set; }

        /// <summary>
        /// GXComponent view that this block uses.
        /// </summary>
        [DefaultValue(null)]
        [ForeignKey(typeof(GXComponentView), OnDelete = ForeignKeyDelete.Cascade)]
        public GXComponentView? ComponentView { get; set; }

        /// <summary>
        /// Script method that this block uses.
        /// </summary>
        [DefaultValue(null)]
        [ForeignKey(typeof(GXScriptMethod), OnDelete = ForeignKeyDelete.None)]
        public GXScriptMethod? ScriptMethod { get; set; }

        /// <summary>
        /// CSS class name.
        /// </summary>
        [StringLength(128)]
        [DefaultValue(null)]
        public string? CssClass { get; set; }

        /// <summary>
        /// CSS role name.
        /// </summary>
        [StringLength(128)]
        [DefaultValue(null)]
        public string? CssRole { get; set; }

        /// <summary>
        /// CSS style.
        /// </summary>
        [StringLength(256)]
        [DefaultValue(null)]
        public string? Style { get; set; }

        /// <summary>
        /// Inline HTML, Markdown, component settings, or a server content file path.
        /// </summary>
        public string? Body { get; set; }

        /// <summary>
        /// Pages where block is shown.
        /// </summary>
        [DefaultValue(BlockShown.Listed)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public BlockShown? Shown { get; set; }


        /// <summary>
        /// Logaction where block is shown.
        /// </summary>
        /// <seealso cref="LocationOrder"/>
        [Filter(FilterType.Exact)]
        [IsRequired]
        public BlockLocation? Location { get; set; }

        /// <summary>
        /// Locaction order index.
        /// </summary>
        /// <seealso cref="Location"/>
        [DefaultValue(0)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public int? LocationOrder { get; set; }


        /// <summary>Pages included or excluded according to the block's page selection mode.</summary>
        public string? Pages { get; set; }

        /// <summary>
        /// Is block enabled.
        /// </summary>
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Can user close the block.
        /// </summary>
        /// <remarks>
        /// If block is closed it's not shown for the user.
        /// </remarks>
        [DefaultValue(false)]
        public bool Closable { get; set; }

        /// <summary>Block groups containing this block.</summary>
        [DataMember, ForeignKey(typeof(GXBlockGroup), typeof(GXBlockGroupBlock), OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Contains)]
        public List<GXBlockGroup>? BlockGroups { get; set; }

        /// <summary>
        /// Block settings for the user.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXUser), typeof(GXUserBlockSettings), OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Contains)]
        public GXUser? User { get; set; }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

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
        public DateTimeOffset? Removed { get; set; }

        /// <summary>
        /// When the block is updated for the last time.
        /// </summary>
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

        /// <summary>
        /// When block is published.
        /// </summary>
        /// <seealso cref="EndTime"/>/>
        [DataMember]
        [DefaultValue(null)]
        public DateTimeOffset? PublishTime { get; set; }

        /// <summary>
        /// When block is not shown anymore.
        /// </summary>
        /// <seealso cref="PublishTime"/>/>
        [DataMember]
        [DefaultValue(null)]
        public DateTimeOffset? EndTime { get; set; }


        /// <summary>
        /// Parent block.
        /// </summary>
        [DefaultValue(null)]
        [ForeignKey(typeof(GXBlock), OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        public GXBlock? Parent { get; set; }

        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>
        /// Responsive determines the order
        /// in which the contents are hidden if they do not fit the screen.
        /// </summary>
        [DefaultValue(0)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public int? Visibility { get; set; }

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
            string? str = Name;
            if (Enabled.HasValue && Enabled.Value)
            {
                str += ", Enabled";
            }
            if (Closable)
            {
                str += ", Closable";
            }
            if (string.IsNullOrEmpty(str))
            {
                str = nameof(GXBlock);
            }
            return str;
        }
    }
}
