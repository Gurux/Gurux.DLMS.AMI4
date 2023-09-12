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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Gateway.
    /// </summary>
    public class GXGateway : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXGateway()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constuctor is called when a new gateway is created. It will create all needed lists.
        /// </remarks>
        /// <param name="name">Gateway name.</param>
        public GXGateway(string? name)
        {
            Active = true;
            Name = name;
            GatewayGroups = new List<GXGatewayGroup>();
            Logs = new List<GXGatewayLog>();
            DeviceGroups = new List<GXDeviceGroup>();
            Devices = new List<GXDevice>();
        }

        /// <summary>
        /// Gateway ID.
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
        /// Gateway Name.
        /// </summary>
        [StringLength(128)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Unique gateway identifier.
        /// </summary>
        [StringLength(128)]
        [Index]
        [Filter(FilterType.Equals)]
        public string? Identifier
        {
            get;
            set;
        }

        /// <summary>
        /// Gateway Status.
        /// </summary>
        [DataMember]
        [DefaultValue(AgentStatus.Offline)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GatewayStatus? Status
        {
            get;
            set;
        }

        /// <summary>
        /// Script method that this gateway uses.
        /// </summary>
        [DefaultValue(null)]
        [ForeignKey(typeof(GXScriptMethod), OnDelete = ForeignKeyDelete.Cascade)]
        public GXScriptMethod? ScriptMethod
        {
            get;
            set;
        }

        /// <summary>
        /// Is gateway active.
        /// </summary>
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Active { get; set; }

        /// <summary>
        /// List of device groups that this gateway can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDeviceGroup), typeof(GXGatewayDeviceGroup))]
        public List<GXDeviceGroup>? DeviceGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of devices that this gateway can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDevice), typeof(GXGatewayDevice))]
        public List<GXDevice>? Devices
        {
            get;
            set;
        }

        /// <summary>
        /// List of gateway groups where this gateway belongs.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXGatewayGroup), typeof(GXGatewayGroupGateway))]
        [Filter(FilterType.Contains)]
        public List<GXGatewayGroup>? GatewayGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// The creator of the gateway.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXUser? Creator
        {
            get;
            set;
        }

        /// <summary>
        /// When the gateway is detected for the last time.
        /// </summary>
        [Description("When the gateway is detected for the last time.")]
        [Filter(FilterType.GreaterOrEqual)]
        [DefaultValue(null)]
        public DateTimeOffset? Detected
        {
            get;
            set;
        }

        /// <summary>
        /// Time when gateway was removed.
        /// </summary>
        /// <remarks>
        /// In filter if the removed time is set it will return values that are not null.
        /// </remarks>
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
        /// When the gateway is updated for the last time.
        /// </summary>
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
        /// Gateway logs.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXGatewayLog))]
        [Filter(FilterType.Contains)]
        public List<GXGatewayLog>? Logs
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
        /// Update creation time before update.
        /// </summary>
        public override void BeforeAdd()
        {
            if (CreationTime == null)
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

        /// <summary>
        /// Agent where gateway is connected.
        /// </summary>
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [Index(false)]
        [DataMember]
        [DefaultValue(null)]
        public GXAgent? Agent
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string? str = Name;
            if (Active.HasValue && Active.Value)
            {
                str += ", Active";
            }
            if (string.IsNullOrEmpty(str))
            {
                str = nameof(GXGateway);
            }
            return str;
        }
    }
}
