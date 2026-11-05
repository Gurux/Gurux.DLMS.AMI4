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

using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.ComponentView
{
    /// <summary>A named group of component views with shared access settings.</summary>
    [Description("A named group of component views with shared access settings.")]
    [DataContract(Name = "GXComponentViewGroup"), Serializable]
    public class GXComponentViewGroup : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXComponentViewGroup.</summary>
        [Description("Initializes a new instance of GXComponentViewGroup.")]
        public GXComponentViewGroup()
        {
        }

        /// <summary>Initializes a new instance of GXComponentViewGroup.</summary>
        /// <remarks>
        /// This constructor is called when a new component view group is created. It will create all required collections.
        /// </remarks>
        /// <param name="name">Component view group name.</param>
        [Description("Initializes a new instance of GXComponentViewGroup.")]
        public GXComponentViewGroup(string? name)
        {
            Name = name;
            UserGroups = [];
            ComponentViews = [];
        }

        /// <summary>Component view group identifier.</summary>
        [Description("Component view group identifier.")]
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// The creator of the component view group.
        /// </summary>
        [Description("The creator of the component view group.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Name of the component view group.
        /// </summary>
        [Description("Name of the component view group.")]
        [DataMember]
        [StringLength(64)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// Component view group description.
        /// </summary>
		[DataMember]
        [StringLength(256)]
        [Description("Component view group description.")]
        //Filter uses default value.
        [DefaultValue(null)]
        public string? Description { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when component view group was removed.
        /// </summary>
        [Description("Time when component view group was removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>Time when the component view group was last updated.</summary>
        [Description("Time when the component view group was last updated.")]
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

        /// <summary>User groups associated with this component view group.</summary>
        [Description("User groups associated with this component view group.")]
        [DataMember, ForeignKey(typeof(GXUserGroup), typeof(GXUserGroupComponentViewGroup))]
        public List<GXUserGroup>? UserGroups { get; set; }

        /// <summary>Component views belonging to this group.</summary>
        [Description("Component views belonging to this group.")]
        [DataMember, ForeignKey(typeof(GXComponentView),
            typeof(GXComponentViewGroupComponentView))]
        public List<GXComponentView>? ComponentViews { get; set; }

        /// <summary>
        /// This is default component view group where new component views are added automatically when user creates them.
        /// </summary>
        [Description("This is default component view group where new component views are added automatically when user creates them.")]
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
            return nameof(GXComponentViewGroup);
        }
    }
}
