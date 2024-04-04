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
using Gurux.Common.Db;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Device
{
    /// <summary>
    /// Devices are group to device groups.
    /// </summary>
    [DataContract(Name = "GXDeviceGroup"), Serializable]
    public class GXDeviceGroup : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXDeviceGroup()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constuctor is called when a new device group is created. It will create all needed lists.
        /// </remarks>
        /// <param name="name">Device group name.</param>
        public GXDeviceGroup(string? name)
        {
            Name = name;
            Devices = new List<GXDevice>();
            UserGroups = new List<GXUserGroup>();
            AgentGroups = new List<GXAgentGroup>();
            Keys = new List<GXKeyManagement>();
            Parameters = new();
            Gateways = new List<GXGateway>();
            Active = true;
        }

        /// <summary>
        /// Device group ID.
        /// </summary>
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// The device group creator.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]        
        public GXUser? Creator
        {
            get;
            set;
        }

        /// <summary>
        /// Is device group active.
        /// </summary>
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Active { get; set; }

        /// <summary>
        /// Device group name.
        /// </summary>
		[DataMember]
        [DefaultValue(null)]
        [StringLength(64)]
        [Filter(FilterType.Contains)]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Device group description.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        public string? Description
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// Date and time when the device group was created.
        /// </summary>
        [DataMember]
        [Description("Creation time.")]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime CreationTime
        {
            get;
            set;
        }


        /// <summary>
        /// Date and time when the device group was removed.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed
        {
            get;
            set;
        }

        /// <summary>
        /// When was the device group last updated.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// User has modified the item.
        /// </summary>
        [IgnoreDataMember]
        [Ignore]
        [JsonIgnore]
        public bool Modified
        {
            get;
            set;
        }


        /// <summary>
        /// Concurrency stamp.
        /// </summary>
        /// <remarks>
        /// Concurrency stamp is used to verify that several user's can't 
        /// modify the target at the same time.
        /// </remarks>
        [DataMember]
        [StringLength(36)]
        public string? ConcurrencyStamp
        {
            get;
            set;
        }

        /// <summary>
        /// Device group parameters.
        /// </summary>
        [DataMember]
        [ForeignKey]
        public List<GXDeviceGroupParameter>? Parameters
        {
            get;
            set;
        }

        /// <summary>
        /// List of key managements.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXKeyManagement))]
        [Filter(FilterType.Contains)]
        public List<GXKeyManagement>? Keys
        {
            get;
            set;
        }

        /// <summary>
        /// List of devices that belongs to this device group.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXDevice), typeof(GXDeviceGroupDevice))]
        public List<GXDevice>? Devices
        {
            get;
            set;
        }

        /// <summary>
        /// List of user groups that can access this device group
        /// </summary>
        [DataMember, ForeignKey(typeof(GXUserGroup), typeof(GXUserGroupDeviceGroup))]
        public List<GXUserGroup>? UserGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of agent groups that are allower to read this device group.
        /// </summary>
        [DataMember(IsRequired = false)]
        [ForeignKey(typeof(GXAgentGroup), typeof(GXAgentGroupDeviceGroup))]
        public List<GXAgentGroup>? AgentGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of gateway that are allower to read this device group.
        /// </summary>
        [DataMember(IsRequired = false)]
        [ForeignKey(typeof(GXGateway), typeof(GXGatewayDeviceGroup))]
        public List<GXGateway>? Gateways
        {
            get;
            set;
        }

        /// <summary>
        /// This is default device group where new devices are added automatically when user creates them.
        /// </summary>
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Default
        {
            get;
            set;
        }

        /// <summary>
        /// Update creation time before update.
        /// </summary>
        public override void BeforeAdd()
        {
            if (CreationTime == DateTime.MinValue)
            {
                CreationTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Update concurrency stamp.
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
            return nameof(GXDeviceGroup);
        }
    }
}
