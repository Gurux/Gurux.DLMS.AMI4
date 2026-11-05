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

using Gurux.DLMS.AMI.Shared.DTOs.Log;
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
    [Description("A gateway providing communication access to devices or device groups.")]
    public class GXGateway : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXGateway.</summary>
        [Description("Initializes a new instance of GXGateway.")]
        public GXGateway()
        {
        }

        /// <summary>Initializes a new instance of GXGateway.</summary>
        /// <remarks>
        /// This constructor is called when a new gateway is created. It will create all required collections.
        /// </remarks>
        /// <param name="name">Gateway name.</param>
        [Description("Initializes a new instance of GXGateway.")]
        public GXGateway(string? name)
        {
            Enabled = true;
            Name = name;
            GatewayGroups = [];
            Logs = [];
            DeviceGroups = [];
            Devices = [];
            TraceLevel = System.Diagnostics.TraceLevel.Error;
        }

        /// <summary>Gateway identifier.</summary>
        [Description("Gateway identifier.")]
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// Gateway Name.
        /// </summary>
        [Description("Gateway Name.")]
        [StringLength(128)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        public string? Name { get; set; }

        /// <summary>Description of the gateway.</summary>
        [DataMember]
        [StringLength(256)]
        [Description("Description of the gateway.")]
        //Filter uses default value.
        [DefaultValue(null)]
        public string? Description { get; set; }

        /// <summary>
        /// Unique gateway identifier.
        /// </summary>
        [Description("Unique gateway identifier.")]
        [StringLength(128)]
        [Index(false)]
        [Filter(FilterType.Equals)]
        public string? Identifier { get; set; }

        /// <summary>
        /// Gateway Status.
        /// </summary>
        [Description("Gateway Status.")]
        [DataMember]
        [DefaultValue(AgentStatus.Offline)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public GatewayStatus? Status { get; set; }

        /// <summary>
        /// Script method that this gateway uses.
        /// </summary>
        [Description("Script method that this gateway uses.")]
        [DefaultValue(null)]
        [ForeignKey(typeof(GXScriptMethod))]
        public GXScriptMethod? ScriptMethod { get; set; }

        /// <summary>Whether the gateway is enabled.</summary>
        [Description("Whether the gateway is enabled.")]
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }

        /// <summary>
        /// List of device groups that this gateway can access.
        /// </summary>
        [Description("List of device groups that this gateway can access.")]
        [DataMember]
        [ForeignKey(typeof(GXDeviceGroup), typeof(GXGatewayDeviceGroup))]
        public List<GXDeviceGroup>? DeviceGroups { get; set; }

        /// <summary>
        /// List of devices that this gateway can access.
        /// </summary>
        [Description("List of devices that this gateway can access.")]
        [DataMember]
        [ForeignKey(typeof(GXDevice), typeof(GXGatewayDevice))]
        public List<GXDevice>? Devices { get; set; }

        /// <summary>Gateway groups containing this gateway.</summary>
        [Description("Gateway groups containing this gateway.")]
        [DataMember, ForeignKey(typeof(GXGatewayGroup), typeof(GXGatewayGroupGateway))]
        [Filter(FilterType.Contains)]
        public List<GXGatewayGroup>? GatewayGroups { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// The creator of the gateway.
        /// </summary>
        [Description("The creator of the gateway.")]
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
        [Description("Time when gateway was removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>Time when the gateway was last updated.</summary>
        [Description("Time when the gateway was last updated.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }


        /// <summary>Trace level used for communication diagnostics.</summary>
        [DataMember]
        [DefaultValue(System.Diagnostics.TraceLevel.Error)]
        [Description("Trace level used for communication diagnostics.")]
        [IsRequired]
        public TraceLevel? TraceLevel { get; set; }

        /// <summary>
        /// Gateway logs.
        /// </summary>
        [Description("Gateway logs.")]
        [DataMember, Ignore(IgnoreType.Db)]
        [Filter(FilterType.Contains)]
        public List<GXLog>? Logs { get; set; }


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

        /// <summary>
        /// Agent where gateway is connected.
        /// </summary>
        [Description("Agent where gateway is connected.")]
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
