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

using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Device
{
    /// <summary>A managed device with its connection settings, COSEM objects, and operational state.</summary>
    [Description("A managed device with its connection settings, COSEM objects, and operational state.")]
    public class GXDevice : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXDevice.</summary>
        [Description("Initializes a new instance of GXDevice.")]
        public GXDevice()
        {
        }

        /// <summary>Initializes a new instance of GXDevice.</summary>
        /// <remarks>
        /// This constructor is called when a new device is created. It will create all required collections.
        /// </remarks>
        /// <param name="name">Device name.</param>
        [Description("Initializes a new instance of GXDevice.")]
        public GXDevice(string? name)
        {
            Name = name;
            Enabled = true;
            Properties = [];
            Objects = [];
            TraceLevel = System.Diagnostics.TraceLevel.Verbose;
            Actions = [];
            Traces = [];
            Errors = [];
            DeviceGroups = [];
            Tasks = [];
            Keys = [];
            Gateways = [];
        }

        /// <summary>
        /// Device identifier.
        /// </summary>
        [Description("Device identifier.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }


        /// <summary>Whether the device is enabled.</summary>
        [Description("Whether the device is enabled.")]
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }

        /// <summary>
        /// Name of the meter.
        /// </summary>
        [Description("Name of the meter.")]
        [StringLength(64)]
        [DataMember]
        [Index(false)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// The system title of the meter.
        /// </summary>
        [Description("The system title of the meter.")]
        [StringLength(16)]
        [DataMember]
        [Index(false)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? SystemTitle { get; set; }

        /// <summary>
        /// Media type.
        /// </summary>
        [Description("Media type.")]
        [DataMember]
        [StringLength(64)]
        [DefaultValue(null)]
        [IsRequired]
        [Filter(FilterType.Contains)]
        public string? MediaType { get; set; }

        /// <summary>Serialized settings for the communication media.</summary>
        [Description("Serialized settings for the communication media.")]
        [DataMember]
        [DefaultValue(null)]
        public string? MediaSettings { get; set; }

        /// <summary>
        /// Data collector.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        [Description("Data collector.")]
        [DataMember]
        [DefaultValue(null)]
        public Guid? Dc { get; set; }

        /// <summary>
        /// The creator of the device.
        /// </summary>
        [Description("The creator of the device.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        [IsRequired]
        //ForeignKeyDelete is None because Template will handle the deletion.
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Gets or sets who can view the device.
        /// </summary>
        /// <remarks>
        /// Modification, deletion and operation permissions are evaluated separately.
        /// </remarks>
        [Description("Gets or sets who can view the device.")]
        [DataMember]
        [DefaultValue(ResourceVisibility.Inherit)]
        [IsRequired]
        public ResourceVisibility? AccessVisibility { get; set; }

        /// <summary>Template used to define this device's default settings and objects.</summary>
        [Description("Template used to define this device's default settings and objects.")]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [Index(false)]
        [DataMember]
        [IsRequired]
        public GXDeviceTemplate? Template { get; set; }

        /// <summary>
        /// Device type.
        /// </summary>
        /// <remarks>
        /// This is coming from the template, but it's saved to make
        /// device reading from the DB faster when template is not need to read.
        /// </remarks>
        [Description("Device type.")]
        [DataMember]
        [DefaultValue(null)]
        public string? Type { get; set; }

        /// <summary>
        /// Device settings.
        /// </summary>
        [Description("Device settings.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? Settings { get; set; }

        /// <summary>
        /// Define how long reply is waited in seconds.
        /// </summary>
        [Description("Define how long reply is waited in seconds.")]
        [DataMember]
        public int WaitTime { get; set; }

        /// <summary>
        /// Define re-send count.
        /// </summary>
        [Description("Define re-send count.")]
        [DataMember]
        public int ResendCount { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when the device was last updated.</summary>
        [Description("Time when the device was last updated.")]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }


        /// <summary>
        /// Device objects are created when they are read from the meter. This improves device creation speed.
        /// </summary>
        /// <remarks>
        /// This information is not saved in the database.
        /// </remarks>
        [Description("Device objects are created when they are read from the meter. This improves device creation speed.")]
        [Ignore]
        public bool? LateBinding { get; set; }

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
        /// When meter is detected last time.
        /// </summary>
        [Description("When meter is detected last time.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
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
        /// Device Status.
        /// </summary>
        [Description("Device Status.")]
        [DataMember]
        [DefaultValue(DeviceStatus.Disconnected)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public DeviceStatus? Status { get; set; }

        /// <summary>
        /// Device properties.
        /// </summary>
        [Description("Device properties.")]
        [DataMember]
        [ForeignKey]
        [Filter(FilterType.Contains)]
        public List<GXDeviceProperty>? Properties { get; set; }

        /// <summary>Time when the record was marked as removed.</summary>
        [Description("Time when the record was marked as removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>
        /// Device objects.
        /// </summary>
        [Description("Device objects.")]
        [DataMember]
        [ForeignKey]
        [Filter(FilterType.Contains)]
        public List<GXObject>? Objects { get; set; }

        /// <summary>
        /// Is device using auto connect.
        /// </summary>
        [Description("Is device using auto connect.")]
        [DataMember]
        [Filter(FilterType.Exact)]
        [DefaultValue(false)]
        [IsRequired]
        public bool? Dynamic { get; set; }

        /// <summary>
        /// How long the the connection is keeped up in seconds.
        /// </summary>
        /// <remarks>
        /// Auto connect devices can use this so
        /// connection is not closed right after meter has read.
        /// If value is zero, the connection is closed without delay.
        /// If value is -1 the connection is not closed.
        /// </remarks>
        /// <seealso cref="KeepAliveAttribute"/>
        /// <seealso cref="KeepAliveInterval"/>
        [Description("How long the the connection is keeped up in seconds.")]
        public int? ConnectionUpTime { get; set; }

        /// <summary>
        /// Keep-alive attribute.
        /// </summary>
        /// <seealso cref="ConnectionUpTime"/>
        /// <seealso cref="KeepAliveInterval"/>
        [Description("Keep-alive attribute.")]
        public Guid? KeepAliveAttribute { get; set; }

        /// <summary>
        /// Keep-alive interval.
        /// </summary>
        /// <seealso cref="ConnectionUpTime"/>
        /// <seealso cref="KeepAliveAttribute"/>
        [Description("Keep-alive interval.")]
        public int? KeepAliveInterval { get; set; }

        /// <summary>Trace level used for communication diagnostics.</summary>
        [DataMember]
        [DefaultValue(System.Diagnostics.TraceLevel.Off)]
        [Description("Trace level used for communication diagnostics.")]
        [IsRequired]
        public TraceLevel? TraceLevel { get; set; }

        /// <summary>
        /// Device actions.
        /// </summary>
        [Description("Device actions.")]
        [DataMember, Ignore(IgnoreType.Db)]
        [Filter(FilterType.Contains)]
        public List<GXLog>? Actions { get; set; }

        /// <summary>
        /// Device hex trace.
        /// </summary>
        [Description("Device hex trace.")]
        [DataMember, ForeignKey(typeof(GXDeviceTrace))]
        [Filter(FilterType.Contains)]
        public List<GXDeviceTrace>? Traces { get; set; }

        /// <summary>
        /// Device errors.
        /// </summary>
        [Description("Device errors.")]
        [DataMember, Ignore(IgnoreType.Db)]
        [Filter(FilterType.Contains)]
        public List<GXLog>? Errors { get; set; }

        /// <summary>Device groups containing this device.</summary>
        [Description("Device groups containing this device.")]
        [DataMember,
            ForeignKey(typeof(GXDeviceGroup), typeof(GXDeviceGroupDevice))]
        [Filter(FilterType.Contains)]
        public List<GXDeviceGroup>? DeviceGroups { get; set; }

        /// <summary>
        /// Executed tasks.
        /// </summary>
        [Description("Executed tasks.")]
        [DataMember]
        [ForeignKey]
        [Filter(FilterType.Contains)]
        public List<GXTask>? Tasks { get; set; }

        /// <summary>
        /// List of key managements.
        /// </summary>
        [Description("List of key managements.")]
        [DataMember]
        [ForeignKey(typeof(GXKeyManagement))]
        [Filter(FilterType.Contains)]
        public List<GXKeyManagement>? Keys { get; set; }

        /// <summary>Gateways configured to communicate with this device.</summary>
        [Description("Gateways configured to communicate with this device.")]
        [DataMember(IsRequired = false)]
        [ForeignKey(typeof(GXGateway), typeof(GXGatewayDevice))]
        public List<GXGateway>? Gateways { get; set; }


        /// <summary>
        /// Latitude and Longitude.
        /// </summary>
        [Description("Latitude and Longitude.")]
        [DataMember]
        [StringLength(26)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? Coordinates { get; set; }

        /// <summary>
        /// Street address.
        /// </summary>
        [Description("Street address.")]
        [DataMember]
        [StringLength(64)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? StreetAddress { get; set; }

        /// <summary>
        /// Postal code.
        /// </summary>
        [Description("Postal code.")]
        [DataMember]
        [StringLength(64)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? PostalCode { get; set; }

        /// <summary>
        /// State or province.
        /// </summary>
        [Description("State or province.")]
        [DataMember]
        [StringLength(64)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? StateOrProvince { get; set; }

        /// <summary>
        /// Country.
        /// </summary>
        [Description("Country.")]
        [DataMember]
        [StringLength(64)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? Country { get; set; }

        /// <summary>
        /// GXAppearance image used as the device's profile picture; null means no picture.
        /// </summary>
        [Description("GXAppearance image used as the device's profile picture; null means no picture.")]
        [DataMember, ForeignKey(OnDelete = ForeignKeyDelete.Restrict)]
        [DefaultValue(null)]
        public GXAppearance? ProfilePicture { get; set; }

        /// <summary>
        /// When the device's attribute were last read.
        /// </summary>
        [DataMember]
        [Description("When the device's attribute were last read.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? LastRead { get; set; }

        /// <summary>
        /// When the device's attribute were last written.
        /// </summary>
        [DataMember]
        [Description("When the device's attribute were last written.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? LastWrite { get; set; }

        /// <summary>
        /// When the device's actions were last invoked.
        /// </summary>
        [DataMember]
        [Description("When the device's actions were last invoked.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? LastAction { get; set; }

        /// <summary>Time when the most recent error occurred.</summary>
        [DataMember]
        [Description("Time when the most recent error occurred.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? LastError { get; set; }

        /// <summary>
        /// Last error message.
        /// </summary>
        [DataMember]
        [Description("Last error message.")]
        [IsRequired]
        public string? LastErrorMessage { get; set; }

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
            return nameof(GXDevice);
        }
    }
}
