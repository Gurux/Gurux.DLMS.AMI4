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

namespace Gurux.DLMS.AMI.Shared.DTOs.Device
{
    /// <summary>A named group of device templates with shared access settings.</summary>
    [DataContract(Name = "GXDeviceTemplateGroup"), Serializable]
    public class GXDeviceTemplateGroup : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXDeviceTemplateGroup()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constuctor is called when a new device template group is created. It will create all needed lists.
        /// </remarks>
        /// <param name="name">Device template group name.</param>
        public GXDeviceTemplateGroup(string? name)
        {
            Name = name;
            UserGroups = new List<GXUserGroup>();
            DeviceTemplates = new List<GXDeviceTemplate>();
        }

        /// <summary>
        /// Device templategroup ID.
        /// </summary>
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }


        /// <summary>
        /// The creator of the device template group.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Name of the device template group.
        /// </summary>
        [DataMember]
        [StringLength(64)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// Device template group description.
        /// </summary>
        [DataMember]
        [StringLength(256)]
        [Description("Description.")]
        [DefaultValue(null)]
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
        /// Time when device template group was removed.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>
        /// When was the device template group last updated.
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

        /// <summary>User groups associated with this device template group.</summary>
        [DataMember, ForeignKey(typeof(GXUserGroup), typeof(GXUserGroupDeviceTemplateGroup))]
        public List<GXUserGroup>? UserGroups { get; set; }

        /// <summary>Device templates belonging to this group.</summary>
        [DataMember, ForeignKey(typeof(GXDeviceTemplate),
            typeof(GXDeviceTemplateGroupDeviceTemplate))]
        public List<GXDeviceTemplate>? DeviceTemplates { get; set; }

        /// <summary>
        /// This is default device template group where new device templates are added automatically when user creates them.
        /// </summary>
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Default { get; set; }

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
            return nameof(GXDeviceTemplate);
        }
    }
}
