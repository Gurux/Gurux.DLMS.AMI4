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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Device settings.
    /// </summary>
    [Description("Device settings.")]
    public class GXDevice : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXDevice()
        {
            Parameters = new List<GXDeviceParameter>();
            Objects = new List<GXObject>();
            TraceLevel = TraceLevel.Verbose;
            Actions = new List<GXDeviceAction>();
            Traces = new List<GXDeviceTrace>();
            Errors = new List<GXDeviceError>();
            DeviceGroups = new List<GXDeviceGroup>();
            Tasks = new List<GXTask>();
            Name = "";
            Type = "";
            MediaType = "";
            Settings = "";
        }

        /// <summary>
        /// Device identifier.
        /// </summary>
        [Description("Device identifier.")]
        [DataMember]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Name of the meter.
        /// </summary>
        [StringLength(64)]
        [DataMember]
        [Index(false)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Media type.
        /// </summary>
        [DataMember]
        [StringLength(64)]
        [DefaultValue(null)]
        [IsRequired]
        [Filter(FilterType.Contains)]
        public string? MediaType
        {
            get;
            set;
        }

        /// <summary>
        /// Media settings as a string.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        public string? MediaSettings
        {
            get;
            set;
        }

        /// <summary>
        /// Data collector.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        [DataMember]
        [DefaultValue(null)]
        public Guid? Dc
        {
            get;
            set;
        }

        /// <summary>
        /// The device creator.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        [IsRequired]
        public GXUser? Creator
        {
            get;
            set;
        }

        /// <summary>
        /// Device identifier.
        /// </summary>
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [Index(false)]
        [DataMember]
        [IsRequired]
        public GXDeviceTemplate? Template
        {
            get;
            set;
        }

        /// <summary>
        /// Device type.
        /// </summary>
        /// <remarks>
        /// This is coming from the template, but it's saved to make 
        /// device reading from the DB faster when template is not need to read.
        /// </remarks>
        [DataMember]
        [DefaultValue(null)]
        public string Type
        {
            get;
            set;
        }

        /// <summary>
        /// Device settings.
        /// </summary>
        [Description("Device settings.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? Settings
        {
            get;
            set;
        }

        /// <summary>
        /// Define how long reply is waited in seconds.
        /// </summary>
        [DataMember]
        public int WaitTime
        {
            get;
            set;
        }

        /// <summary>
        /// Define re-send count.
        /// </summary>
        [DataMember]
        public int ResendCount
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
        public DateTime CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// When device is updated last time.
        /// </summary>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// User has modified the device.
        /// </summary>
        [IgnoreDataMember]
        [Ignore]
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
        /// When meter is detected last time.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Detected
        {
            get;
            set;
        }

        /// <summary>
        /// Device parameters.
        /// </summary>
        [DataMember]
        [ForeignKey]
        [Filter(FilterType.Contains)]
        public List<GXDeviceParameter> Parameters
        {
            get;
            set;
        }

        /// <summary>
        /// Remove time.
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
        /// Device objects.
        /// </summary>
        [DataMember]
        [ForeignKey]
        [Filter(FilterType.Contains)]
        public List<GXObject> Objects
        {
            get;
            set;
        }

        /// <summary>
        /// Is device using dynamic IP address.
        /// </summary>
        [DataMember]
        [Filter(FilterType.Exact)]
        [DefaultValue(false)]
        [IsRequired]
        public bool? Dynamic
        {
            get;
            set;
        }

        /// <summary>
        /// Used trace level.
        /// </summary>
        [DataMember]
        [Description("Used trace level.")]
        public TraceLevel TraceLevel
        {
            get;
            set;
        }

        /// <summary>
        /// Device actions.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXDeviceAction))]
        [Filter(FilterType.Contains)]
        public List<GXDeviceAction> Actions
        {
            get;
            set;
        }

        /// <summary>
        /// Device hex trace.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXDeviceTrace))]
        [Filter(FilterType.Contains)]
        public List<GXDeviceTrace> Traces
        {
            get;
            set;
        }

        /// <summary>
        /// Device errors.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXDeviceError))]
        [Filter(FilterType.Contains)]
        public List<GXDeviceError> Errors
        {
            get;
            set;
        }

        /// <summary>
        /// List of device groups where this device belongs.
        /// </summary>
        [DataMember,
            ForeignKey(typeof(GXDeviceGroup), typeof(GXDeviceGroupDevice))]
        [Filter(FilterType.Contains)]
        public List<GXDeviceGroup> DeviceGroups
        {
            get;
            set;
        }

        /// <summary>
        /// Executed tasks.
        /// </summary>
        [DataMember]
        [ForeignKey]
        [Filter(FilterType.Contains)]
        public List<GXTask> Tasks
        {
            get;
            set;
        }

        /// <summary>
        /// Latitude and Longitude.
        /// </summary>
        [DataMember]
        [StringLength(26)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? Coordinates { get; set; }

        /// <summary>
        /// Street address.
        /// </summary>
        [DataMember]
        [StringLength(64)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? StreetAddress { get; set; }

        /// <summary>
        /// Postal code.
        /// </summary>
        [DataMember]
        [StringLength(64)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? PostalCode { get; set; }

        /// <summary>
        /// State or province.
        /// </summary>
        [DataMember]
        [StringLength(64)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? StateOrProvince { get; set; }

        /// <summary>
        /// Country.
        /// </summary>
        [DataMember]
        [StringLength(64)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? Country { get; set; }

        /// <summary>
        /// Meter profile Picture.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        public string? ProfilePicture { get; set; }

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
            return nameof(GXDevice);
        }
    }
}
