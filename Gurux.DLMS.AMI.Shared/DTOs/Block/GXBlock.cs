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

namespace Gurux.DLMS.AMI.Shared.DTOs.Block
{
    /// <summary>A configurable content block displayed at a selected location in the user interface.</summary>
    [Description("A configurable content block displayed at a selected location in the user interface.")]
    public class GXBlock : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXBlock.</summary>
        [Description("Initializes a new instance of GXBlock.")]
        public GXBlock()
        {
        }

        /// <summary>Initializes a new instance of GXBlock.</summary>
        /// <remarks>
        /// This constructor is called when a new block is created. It will create all required collections.
        /// </remarks>
        /// <param name="name">Block name.</param>
        [Description("Initializes a new instance of GXBlock.")]
        public GXBlock(string? name)
        {
            Enabled = true;
            Name = name;
            BlockGroups = [];
        }

        /// <summary>Block identifier.</summary>
        [Description("Block identifier.")]
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// The block creator.
        /// </summary>
        [Description("The block creator.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        //ForeignKeyDelete is None because User will handle the deletion.
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Gets or sets who can view the block.
        /// </summary>
        /// <remarks>
        /// Modification, deletion and operation permissions are evaluated separately.
        /// </remarks>
        [Description("Gets or sets who can view the block.")]
        [DataMember]
        [DefaultValue(ResourceVisibility.Inherit)]
        [IsRequired]
        public ResourceVisibility? AccessVisibility { get; set; }

        /// <summary>
        /// Block Title.
        /// </summary>
        [Description("Block Title.")]
        [StringLength(128)]
        public string? Title { get; set; }

        /// <summary>
        /// Block Name.
        /// </summary>
        [Description("Block Name.")]
        [StringLength(128)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        public string? Name { get; set; }

        /// <summary>
        /// Block type.
        /// </summary>
        [Description("Block type.")]
        public BlockType BlockType { get; set; }

        /// <summary>
        /// GXComponent view that this block uses.
        /// </summary>
        [Description("GXComponent view that this block uses.")]
        [DefaultValue(null)]
        [ForeignKey(typeof(GXComponentView), OnDelete = ForeignKeyDelete.Cascade)]
        public GXComponentView? ComponentView { get; set; }

        /// <summary>
        /// Script method that this block uses.
        /// </summary>
        [Description("Script method that this block uses.")]
        [DefaultValue(null)]
        [ForeignKey(typeof(GXScriptMethod), OnDelete = ForeignKeyDelete.None)]
        public GXScriptMethod? ScriptMethod { get; set; }

        /// <summary>
        /// CSS class name.
        /// </summary>
        [Description("CSS class name.")]
        [StringLength(128)]
        [DefaultValue(null)]
        public string? CssClass { get; set; }

        /// <summary>
        /// CSS role name.
        /// </summary>
        [Description("CSS role name.")]
        [StringLength(128)]
        [DefaultValue(null)]
        public string? CssRole { get; set; }

        /// <summary>
        /// CSS style.
        /// </summary>
        [Description("CSS style.")]
        [StringLength(256)]
        [DefaultValue(null)]
        public string? Style { get; set; }

        /// <summary>
        /// Inline HTML, Markdown, component settings, or a server content file path.
        /// </summary>
        [Description("Inline HTML, Markdown, component settings, or a server content file path.")]
        public string? Body { get; set; }

        /// <summary>
        /// Determines whether Pages lists the pages where this block is shown or hidden.
        /// An empty page list displays the block on all pages in either mode.
        /// </summary>
        [Description("Show only on listed pages or hide on listed pages. An empty Pages list displays the block on all pages in either mode.")]
        [DefaultValue(BlockPageVisibility.ShowOnListedPages)]
        [IsRequired]
        [EnumDataType(typeof(BlockPageVisibility))]
        public BlockPageVisibility PageVisibility { get; set; } = BlockPageVisibility.ShowOnListedPages;


        /// <summary>
        /// Logaction where block is shown.
        /// </summary>
        /// <seealso cref="LocationOrder"/>
        [Description("Logaction where block is shown.")]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public BlockLocation? Location { get; set; }

        /// <summary>
        /// Locaction order index.
        /// </summary>
        /// <seealso cref="Location"/>
        [Description("Locaction order index.")]
        [DefaultValue(0)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public int? LocationOrder { get; set; }


        /// <summary>Page paths, one per line, interpreted according to PageVisibility. Empty means all pages.</summary>
        [Description("Page paths, one per line (for example /, /Device, /Config/Menu). Empty means all pages. Paths match exactly, ignoring case, surrounding slashes, query strings and fragments.")]
        public string? Pages { get; set; }

        /// <summary>Whether the block is enabled.</summary>
        [Description("Whether the block is enabled.")]
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Load this resource during client initialization when access is allowed.
        /// </summary>
        [Description("Load this resource during client initialization when access is allowed.")]
        [DataMember, IsRequired, DefaultValue(false), Filter(FilterType.Exact, false)]
        public bool? Preload { get; set; } = false;

        /// <summary>
        /// Can user close the block.
        /// </summary>
        /// <remarks>
        /// If block is closed it's not shown for the user.
        /// </remarks>
        [Description("Can user close the block.")]
        [DefaultValue(false)]
        public bool Closable { get; set; }

        /// <summary>Block groups containing this block.</summary>
        [Description("Block groups containing this block.")]
        [DataMember, ForeignKey(typeof(GXBlockGroup), typeof(GXBlockGroupBlock), OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Contains)]
        public List<GXBlockGroup>? BlockGroups { get; set; }

        /// <summary>
        /// Block settings for the user.
        /// </summary>
        [Description("Block settings for the user.")]
        [DataMember, ForeignKey(typeof(GXUser), typeof(GXUserBlockSettings), OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Contains)]
        public GXUser? User { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
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
        [Description("Time when block was removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>Time when the block was last updated.</summary>
        [Description("Time when the block was last updated.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

        /// <summary>
        /// When block is published.
        /// </summary>
        /// <seealso cref="EndTime"/>/>
        [Description("When block is published.")]
        [DataMember]
        [DefaultValue(null)]
        public DateTimeOffset? PublishTime { get; set; }

        /// <summary>
        /// When block is not shown anymore.
        /// </summary>
        /// <seealso cref="PublishTime"/>/>
        [Description("When block is not shown anymore.")]
        [DataMember]
        [DefaultValue(null)]
        public DateTimeOffset? EndTime { get; set; }


        /// <summary>
        /// Parent block.
        /// </summary>
        [Description("Parent block.")]
        [DataMember]
        [DefaultValue(null)]
        [ForeignKey(typeof(GXBlock), OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        public GXBlock? Parent { get; set; }

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
        /// Responsive determines the order
        /// in which the contents are hidden if they do not fit the screen.
        /// </summary>
        [Description("Responsive determines the order in which the contents are hidden if they do not fit the screen.")]
        [DefaultValue(0)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public int? Visibility { get; set; }

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
