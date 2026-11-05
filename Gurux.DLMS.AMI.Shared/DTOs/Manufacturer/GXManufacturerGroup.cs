// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Manufacturer
{
    /// <summary>A named group of device manufacturers with shared access settings.</summary>
    [Description("A named group of device manufacturers with shared access settings.")]
    public class GXManufacturerGroup : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXManufacturerGroup.</summary>
        [Description("Initializes a new instance of GXManufacturerGroup.")]
        public GXManufacturerGroup()
        {
        }

        /// <summary>Initializes a new instance of GXManufacturerGroup.</summary>
        /// <remarks>
        /// This constructor is called when a new manufacturer group is created. It will create all required collections.
        /// </remarks>
        /// <param name="name">Manufacturer group name.</param>
        [Description("Initializes a new instance of GXManufacturerGroup.")]
        public GXManufacturerGroup(string? name)
        {
            Name = name;
            Default = true;
            UserGroups = [];
            Manufacturers = [];
        }

        /// <summary>
        /// Manufacturer group identifier.
        /// </summary>
        [Description("Manufacturer group identifier.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// The creator of the management group.
        /// </summary>
        [Description("The creator of the management group.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Gets or sets who can view the resource.
        /// </summary>
        /// <remarks>
        /// Modification, deletion and operation permissions are evaluated separately.
        /// </remarks>
        [Description("Gets or sets who can view the resource.")]
        [DataMember]
        [DefaultValue(ResourceVisibility.Private)]
        [IsRequired]
        public ResourceVisibility? AccessVisibility { get; set; }

        /// <summary>
        /// Manufacturer group name.
        /// </summary>
        [Description("Manufacturer group name.")]
        [DataMember]
        [StringLength(64)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>Description of the manufacturer group.</summary>
        [DataMember]
        [StringLength(256)]
        [Description("Description of the manufacturer group.")]
        //Filter uses default value.
        [DefaultValue(null)]
        public string? Description { get; set; }

        /// <summary>Manufacturers belonging to this group.</summary>
        [Description("Manufacturers belonging to this group.")]
        [DataMember, ForeignKey(typeof(GXManufacturer), typeof(GXManufacturerGroupManufacturer))]
        public List<GXManufacturer>? Manufacturers { get; set; }

        /// <summary>
        /// This manufacturer group is public.
        /// </summary>
        /// <remarks>
        /// If manufacturer group is public all the users can see the manufacturers on it.
        /// </remarks>
        [Description("This manufacturer group is public.")]
        [DataMember]
        //Filter uses default value.
        [DefaultValue(true)]
        [Filter(FilterType.Equals)]
        [IsRequired]
        public bool? Public { get; set; }

        /// <summary>
        /// User groups that can access this manufacturer group.
        /// </summary>
        [Description("User groups that can access this manufacturer group.")]
        [DataMember]
        [ForeignKey(typeof(GXUserGroup), typeof(GXUserGroupManufacturerGroup))]
        public List<GXUserGroup>? UserGroups { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        //Filter uses default value.
        [DefaultValue(null)]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when the record was marked as removed.</summary>
        [Description("Time when the record was marked as removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>Time when the manufacturer group was last updated.</summary>
        [Description("Time when the manufacturer group was last updated.")]
        [DataMember]
        [DefaultValue(null)]
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
        /// This is default manufacturer group where new manufacturers are added automatically when user creates them.
        /// </summary>
        [Description("This is default manufacturer group where new manufacturers are added automatically when user creates them.")]
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Default { get; set; }

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
            return nameof(GXManufacturerGroup);
        }
    }
}
