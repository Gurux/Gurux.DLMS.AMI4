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
using System.ComponentModel;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;

namespace Gurux.DLMS.AMI.Shared.DTOs.Gateway
{
    /// <summary>A named group of gateways with shared access settings.</summary>
    [Description("A named group of gateways with shared access settings.")]
    public class GXGatewayGroup : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXGatewayGroup.</summary>
        [Description("Initializes a new instance of GXGatewayGroup.")]
        public GXGatewayGroup()
        {
        }

        /// <summary>Initializes a new instance of GXGatewayGroup.</summary>
        /// <remarks>
        /// This constructor is called when a new gateway group is created. It will create all required collections.
        /// </remarks>
        /// <param name="name">Gateway group name.</param>
        [Description("Initializes a new instance of GXGatewayGroup.")]
        public GXGatewayGroup(string? name)
        {
            Name = name;
            Enabled = true;
            Default = true;
            UserGroups = [];
            Gateways = [];
        }

        /// <summary>
        /// Gateway group identifier.
        /// </summary>
        [Description("Gateway group identifier.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// The creator of the gateway group.
        /// </summary>
        [Description("The creator of the gateway group.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Gateway group name.
        /// </summary>
        [Description("Gateway group name.")]
        [DataMember]
        [StringLength(64)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>Description of the gateway group.</summary>
        [DataMember]
        [StringLength(256)]
        [Description("Description of the gateway group.")]
        //Filter uses default value.
        [DefaultValue(null)]
        public string? Description { get; set; }

        /// <summary>Whether the gateway group is enabled.</summary>
        //Filter uses default value.
        [Description("Whether the gateway group is enabled.")]
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }


        /// <summary>Gateways belonging to this group.</summary>
        [Description("Gateways belonging to this group.")]
        [DataMember, ForeignKey(typeof(GXGateway), typeof(GXGatewayGroupGateway))]
        public List<GXGateway>? Gateways { get; set; }

        /// <summary>
        /// User groups that can access this gateway group.
        /// </summary>
        [Description("User groups that can access this gateway group.")]
        [DataMember]
        [ForeignKey(typeof(GXUserGroup), typeof(GXUserGroupGatewayGroup))]
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

        /// <summary>Time when the gateway group was last updated.</summary>
        [Description("Time when the gateway group was last updated.")]
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
        /// This is default gateway group where new gateways are added automatically when user creates them.
        /// </summary>
        [Description("This is default gateway group where new gateways are added automatically when user creates them.")]
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
            return nameof(GXGatewayGroup);
        }
    }
}
