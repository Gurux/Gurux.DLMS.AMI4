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

using Gurux.DLMS.AMI.Shared.DTOs.Log;
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
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Gateway
{
    /// <summary>A gateway providing communication access to devices or device groups.</summary>
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
            Enabled = true;
            Name = name;
            GatewayGroups = new List<GXGatewayGroup>();
            Logs = new List<GXLog>();
            DeviceGroups = new List<GXDeviceGroup>();
            Devices = new List<GXDevice>();
            TraceLevel = System.Diagnostics.TraceLevel.Error;
        }

        /// <summary>
        /// Gateway ID.
        /// </summary>
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// Gateway Name.
        /// </summary>
        [StringLength(128)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        public string? Name { get; set; }

        /// <summary>
        /// Description.
        /// </summary>
        [DataMember]
        [StringLength(256)]
        [Description("Description.")]
        //Filter uses default value.
        [DefaultValue(null)]
        public string? Description { get; set; }

        /// <summary>
        /// Unique gateway identifier.
        /// </summary>
        [StringLength(128)]
        [Index(false)]
        [Filter(FilterType.Equals)]
        public string? Identifier { get; set; }

        /// <summary>
        /// Gateway Status.
        /// </summary>
        [DataMember]
        [DefaultValue(AgentStatus.Offline)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GatewayStatus? Status { get; set; }

        /// <summary>
        /// Script method that this gateway uses.
        /// </summary>
        [DefaultValue(null)]
        [ForeignKey(typeof(GXScriptMethod))]
        public GXScriptMethod? ScriptMethod { get; set; }

        /// <summary>
        /// Is gateway enabled.
        /// </summary>
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }

        /// <summary>
        /// List of device groups that this gateway can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDeviceGroup), typeof(GXGatewayDeviceGroup))]
        public List<GXDeviceGroup>? DeviceGroups { get; set; }

        /// <summary>
        /// List of devices that this gateway can access.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDevice), typeof(GXGatewayDevice))]
        public List<GXDevice>? Devices { get; set; }

        /// <summary>Gateway groups containing this gateway.</summary>
        [DataMember, ForeignKey(typeof(GXGatewayGroup), typeof(GXGatewayGroupGateway))]
        [Filter(FilterType.Contains)]
        public List<GXGatewayGroup>? GatewayGroups { get; set; }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// The creator of the gateway.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// When the gateway is detected for the last time.
        /// </summary>
        [Description("When the gateway is detected for the last time.")]
        [Filter(FilterType.GreaterOrEqual)]
        [DefaultValue(null)]
        public DateTimeOffset? Detected { get; set; }

        /// <summary>
        /// Holds metadata about the client connection, including the remote IP address and other optional
        /// network-related details associated with the current request.
        /// </summary>
        [DataMember]
        [Description("Holds metadata about the client connection, including the remote IP address and other optional network-related details associated with the current request.")]
        [Filter(FilterType.Contains)]
        [DefaultValue(null)]
        [StringLength(64)]
        public string? ConnectionInfo { get; set; }

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
        public DateTimeOffset? Removed { get; set; }

        /// <summary>
        /// When the gateway is updated for the last time.
        /// </summary>
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }


        /// <summary>
        /// Used trace level.
        /// </summary>
        [DataMember]
        [DefaultValue(System.Diagnostics.TraceLevel.Error)]
        [Description("Used trace level.")]
        [IsRequired]
        public TraceLevel? TraceLevel { get; set; }

        /// <summary>
        /// Gateway logs.
        /// </summary>
        [DataMember, Ignore(IgnoreType.Db)]
        [Filter(FilterType.Contains)]
        public List<GXLog>? Logs { get; set; }


        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

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

        /// <summary>
        /// Agent where gateway is connected.
        /// </summary>
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [Index(false)]
        [DataMember]
        [DefaultValue(null)]
        public GXAgent? Agent { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            string? str = Name;
            if (Enabled.HasValue && Enabled.Value)
            {
                str += ", Enabled";
            }
            if (string.IsNullOrEmpty(str))
            {
                str = nameof(GXGateway);
            }
            return str;
        }
    }
}
