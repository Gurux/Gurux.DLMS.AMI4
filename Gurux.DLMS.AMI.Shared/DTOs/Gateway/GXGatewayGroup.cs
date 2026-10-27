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
