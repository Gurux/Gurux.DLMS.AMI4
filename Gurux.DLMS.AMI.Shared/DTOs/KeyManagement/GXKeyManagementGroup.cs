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

namespace Gurux.DLMS.AMI.Shared.DTOs.KeyManagement
{
    /// <summary>A named group of key management configurations with shared access settings.</summary>
    [Description("A named group of key management configurations with shared access settings.")]
    [DataContract(Name = "GXKeyManagementGroup"), Serializable]
    public class GXKeyManagementGroup : GXTableBase, IUnique<Guid>
    {

        /// <summary>Initializes a new instance of GXKeyManagementGroup.</summary>
        [Description("Initializes a new instance of GXKeyManagementGroup.")]
        public GXKeyManagementGroup()
        {
        }

        /// <summary>Initializes a new instance of GXKeyManagementGroup.</summary>
        /// <remarks>
        /// This constructor is called when a new key management group is created. It will create all required collections.
        /// </remarks>
        /// <param name="name">Key management group name.</param>
        [Description("Initializes a new instance of GXKeyManagementGroup.")]
        public GXKeyManagementGroup(string? name)
        {
            Name = name;
            UserGroups = [];
            KeyManagements = [];
        }

        /// <summary>Key management group identifier.</summary>
        [Description("Key management group identifier.")]
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }


        /// <summary>
        /// The creator of the key management group.
        /// </summary>
        [Description("The creator of the key management group.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Name of the key management group.
        /// </summary>
        [Description("Name of the key management group.")]
        [DataMember]
        [StringLength(64)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// Key management group description.
        /// </summary>
        [Description("Key management group description.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? Description { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when key management group was removed.
        /// </summary>
        [Description("Time when key management group was removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>Time when the key management group was last updated.</summary>
        [Description("Time when the key management group was last updated.")]
        [DataMember]
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

        /// <summary>User groups associated with this key management group.</summary>
        [Description("User groups associated with this key management group.")]
        [DataMember, ForeignKey(typeof(GXUserGroup), typeof(GXUserGroupKeyManagementGroup))]
        public List<GXUserGroup>? UserGroups { get; set; }

        /// <summary>Key management configurations belonging to this group.</summary>
        [Description("Key management configurations belonging to this group.")]
        [DataMember, ForeignKey(typeof(GXKeyManagement), typeof(GXKeyManagementGroupKeyManagement))]
        public List<GXKeyManagement>? KeyManagements { get; set; }

        /// <summary>
        /// This is default key management group where new key managements are added automatically when user creates them.
        /// </summary>
        [Description("This is default key management group where new key managements are added automatically when user creates them.")]
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
            return nameof(GXKeyManagementGroup);
        }
    }
}
