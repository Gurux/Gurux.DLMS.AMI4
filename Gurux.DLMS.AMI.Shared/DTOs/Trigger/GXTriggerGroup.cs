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
using Gurux.DLMS.AMI.Shared.DTOs.User;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Trigger
{
    /// <summary>A named group of triggers with shared access settings.</summary>
    [DataContract(Name = "GXTriggerGroup"), Serializable]
    public class GXTriggerGroup : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXTriggerGroup()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constuctor is called when a new trigger group is created. It will create all needed lists.
        /// </remarks>
        /// <param name="name">Trigger group name.</param>
        public GXTriggerGroup(string? name)
        {
            Name = name;
            UserGroups = new List<GXUserGroup>();
            Triggers = new List<GXTrigger>();
        }

        /// <summary>
        /// Trigger group ID.
        /// </summary>
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// The creator of the trigger group.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Name of the trigger group.
        /// </summary>
        [DataMember]
        [StringLength(64)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// Trigger group description.
        /// </summary>
		[DataMember]
        public string? Description { get; set; }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when trigger group was removed.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>
        /// When was the trigger group last updated.
        /// </summary>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }
        /// <summary>User groups associated with this trigger group.</summary>
        [DataMember, ForeignKey(typeof(GXUserGroup), typeof(GXUserGroupTriggerGroup))]
        public List<GXUserGroup>? UserGroups { get; set; }

        /// <summary>Triggers belonging to this group.</summary>
        [DataMember, ForeignKey(typeof(GXTrigger), typeof(GXTriggerGroupTrigger))]
        public List<GXTrigger>? Triggers { get; set; }

        /// <summary>
        /// This is default trigger group where new triggers are added automatically when user creates them.
        /// </summary>
        [IgnoreDataMember]
        [Ignore]
        public bool Default { get; set; }

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
            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }
            return nameof(GXTriggerGroup);
        }
    }
}
