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
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Device
{
    /// <summary>Reusable device settings and COSEM object definitions for creating devices.</summary>
    [Description("Reusable device settings and COSEM object definitions for creating devices.")]
    public class GXDeviceTemplate : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXDeviceTemplate.</summary>
        [Description("Initializes a new instance of GXDeviceTemplate.")]
        public GXDeviceTemplate()
        {
        }

        /// <summary>Initializes a new instance of GXDeviceTemplate.</summary>
        /// <remarks>
        /// This constructor is called when a new device template group is created. It will create all required collections.
        /// </remarks>
        /// <param name="name">Device template name.</param>
        [Description("Initializes a new instance of GXDeviceTemplate.")]
        public GXDeviceTemplate(string? name)
        {
            Name = name;
            Objects = [];
            DeviceTemplateGroups = [];
            WaitTime = 5;
            ResendCount = 3;
            Keys = [];
        }

        /// <summary>Device template identifier.</summary>
        [Description("Device template identifier.")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// Template type.
        /// </summary>
        [Description("Template type.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Type { get; set; }

        /// <summary>
        /// This is default device group where new devices are added automatically when user creates them.
        /// </summary>
        [Description("This is default device group where new devices are added automatically when user creates them.")]
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Default { get; set; }

        /// <summary>
        /// Name of the meter template.
        /// </summary>
        [Description("Name of the meter template.")]
        [StringLength(64)]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? Name { get; set; }

        /// <summary>
        /// Protocol that the device uses.
        /// </summary>
        [DataMember(Name = "Protocol")]
        [Description("Protocol that the device uses.")]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? Protocol { get; set; }

        /// <summary>
        /// Define how long reply is waited in seconds.
        /// </summary>
        [Description("Define how long reply is waited in seconds.")]
        [DefaultValue(5)]
        public int WaitTime { get; set; }

        /// <summary>
        /// Define re-send count.
        /// </summary>
        [Description("Define re-send count.")]
        [DefaultValue(3)]
        public int ResendCount { get; set; }

        /// <summary>
        /// Device settings.
        /// </summary>
        [Description("Device settings.")]
        [DataMember]
        [DefaultValue(null)]
        public string? Settings { get; set; }

        /// <summary>
        /// GXAppearance image used as the device's profile picture; null means no picture.
        /// </summary>
        [Description("GXAppearance image used as the device's profile picture; null means no picture.")]
        [DataMember, ForeignKey(OnDelete = ForeignKeyDelete.Restrict)]
        [DefaultValue(null)]
        public GXAppearance? ProfilePicture { get; set; }

        /// <summary>
        /// Media type.
        /// </summary>
        [Description("Media type.")]
        [StringLength(64)]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public virtual string? MediaType { get; set; }

        /// <summary>Serialized settings for the communication media.</summary>
        [Description("Serialized settings for the communication media.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public string? MediaSettings { get; set; }

        /// <summary>
        /// The creator of the device template.
        /// </summary>
        [Description("The creator of the device template.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Gets or sets who can view the device template.
        /// </summary>
        /// <remarks>
        /// Modification, deletion and operation permissions are evaluated separately.
        /// </remarks>
        [Description("Gets or sets who can view the device template.")]
        [DataMember]
        [DefaultValue(ResourceVisibility.Inherit)]
        [IsRequired]
        public ResourceVisibility? AccessVisibility { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>Time when the device template was last updated.</summary>
        [Description("Time when the device template was last updated.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }


        /// <summary>
        /// Manufacturer of the device settings.
        /// </summary>
        /// <remarks>
        /// This is null if device template is a custom template.
        /// This information is not saved for the DB.
        /// </remarks>
        [Description("Manufacturer of the device settings.")]
        [Ignore]
        public GXManufacturer? Manufacturer { get; set; }

        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [Description("Version token used by the database to detect conflicting updates.")]
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>Time when the record was marked as removed.</summary>
        [Description("Time when the record was marked as removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>
        /// Object templates.
        /// </summary>
        [Description("Object templates.")]
        [ForeignKey(typeof(GXObjectTemplate))]
        public List<GXObjectTemplate>? Objects { get; set; }

        /// <summary>Device template groups containing this template.</summary>
        [Description("Device template groups containing this template.")]
        [DataMember]
        [ForeignKey(typeof(GXDeviceTemplateGroup),
            typeof(GXDeviceTemplateGroupDeviceTemplate))]
        public List<GXDeviceTemplateGroup>? DeviceTemplateGroups { get; set; }

        /// <summary>
        /// List of key managements.
        /// </summary>
        [Description("List of key managements.")]
        [DataMember]
        [ForeignKey(typeof(GXKeyManagement))]
        [Filter(FilterType.Contains)]
        public List<GXKeyManagement>? Keys { get; set; }

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
            return nameof(GXDeviceTemplate);
        }
    }
}
