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
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>A named image, icon pack, or theme resource used to customize the application appearance.</summary>
    [Description("A named image, icon pack, or theme resource used to customize the application appearance.")]
    public class GXAppearance : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXAppearance.</summary>
        [Description("Initializes a new instance of GXAppearance.")]
        public GXAppearance()
        {
        }

        /// <summary>Initializes a new instance of GXAppearance.</summary>
        [Description("Initializes a new instance of GXAppearance.")]
        public GXAppearance(byte type, string name, string description)
        {
            ResourceType = type;
            Name = name;
            Description = description;
        }

        /// <summary>Initializes a new instance of GXAppearance.</summary>
        [Description("Initializes a new instance of GXAppearance.")]
        public GXAppearance(byte type, string name, string description, string value)
        {
            ResourceType = type;
            Name = name;
            Description = description;
            Value = value;
        }

        /// <summary>
        /// Permanent appearance identifier used by references.
        /// </summary>
        [Description("Permanent appearance identifier used by references.")]
        [Key]
        [DataMember(Name = "ID")]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>Unique, editable resource name used for symbolic resource lookups.</summary>
        [Description("Unique, editable resource name used for symbolic resource lookups.")]
        [DataMember, StringLength(64), IsRequired, Required]
        [Index(Unique = true)]
        [Filter(FilterType.Contains)]
        public string Name { get; set; } = "";

        /// <summary>
        /// The Appearance creator.
        /// </summary>
        [Description("The Appearance creator.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        [IsRequired]
        public GXUser? Creator { get; set; }

        /// <summary>Whether the appearance is enabled.</summary>
        [Description("Whether the appearance is enabled.")]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Category.
        /// </summary>
        [Description("Category.")]
        [DataMember]
        [Filter(FilterType.Equals)]
        [DefaultValue(null)]
        [StringLength(64)]
        [Index(Unique = false)]
        public string? Category { get; set; }

        /// <summary>
        /// Appearance description.
        /// </summary>
        [Description("Appearance description.")]
        [DataMember]
        public string? Description { get; set; }

        /// <summary>
        /// Appearance value.
        /// </summary>
        [Description("Appearance value.")]
        [DataMember]
        [IsRequired]
        public string? Value { get; set; }

        /// <summary>Server-generated relative key for a file image; null for legacy inline resources.</summary>
        /// <remarks>Assigned by the host. Clients cannot attach arbitrary stored files using this field.</remarks>
        [Description("Server-generated relative key for a file image; null for legacy inline resources.")]
        [DataMember, StringLength(128)]
        public string? StorageKey { get; set; }

        /// <summary>Original upload filename for display, without directory components.</summary>
        [Description("Original upload filename for display, without directory components.")]
        [DataMember, StringLength(255)]
        public string? OriginalFileName { get; set; }

        /// <summary>Media type inferred from uploaded content rather than the supplied filename.</summary>
        [Description("Media type inferred from uploaded content rather than the supplied filename.")]
        [DataMember, StringLength(64)]
        public string? ContentType { get; set; }

        /// <summary>
        /// Resource type.
        /// </summary>
        [Description("Resource type.")]
        [IsRequired]
        [DataMember]
        [Filter(FilterType.Exact)]
        [Index(false)]
        public byte? ResourceType { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when Appearance was removed.
        /// </summary>
        /// <remarks>
        /// In filter if the removed time is set it will return values that are not null.
        /// </remarks>
        [Description("Time when Appearance was removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>Time when the appearance was last updated.</summary>
        [Description("Time when the appearance was last updated.")]
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
            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }
            return nameof(GXAppearance);
        }
        /// <summary>Alternative policies allowing use of this appearance; empty means unrestricted.</summary>
        [Description("Alternative policies allowing use of this appearance; empty means unrestricted.")]
        [DataMember, Ignore(IgnoreType.Db)]
        public List<string>? Policies { get; set; }

        /// <summary>Higher values take precedence when choosing a default theme.</summary>
        [Description("Higher values take precedence when choosing a default theme.")]
        [DataMember, DefaultValue(0), Filter(FilterType.Exact)]
        public int Priority { get; set; }

        /// <summary>Load this resource during client initialization when access is allowed.</summary>
        [Description("Load this resource during client initialization when access is allowed.")]
        [DataMember, IsRequired, DefaultValue(false), Filter(FilterType.Exact)]
        public bool? Preload { get; set; }
    }
}
