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
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Manufacturer
{
    /// <summary>A saved set of device settings associated with a device model version.</summary>
    [Description("A saved set of device settings associated with a device model version.")]
    public class GXDeviceSettings : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXDeviceSettings.</summary>
        [Description("Initializes a new instance of GXDeviceSettings.")]
        public GXDeviceSettings()
        {

        }

        /// <summary>Initializes a new instance of GXDeviceSettings.</summary>
        /// <param name="name">Settings name.</param>
        [Description("Initializes a new instance of GXDeviceSettings.")]
        public GXDeviceSettings(string? name)
        {
            Name = name;
        }

        /// <summary>Device settings identifier.</summary>
        [Description("Device settings identifier.")]
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>Stable identity in the DeviceProfiles index; distinct from the database identifier.</summary>
        [Description("Stable identity in the DeviceProfiles index; distinct from the database identifier.")]
        [DataMember]
        [StringLength(1024)]
        public string? ProfileId { get; set; }

        /// <summary>
        /// Settings version.
        /// </summary>
        [Description("Settings version.")]
        [DefaultValue(null)]
        [ForeignKey(typeof(GXDeviceVersion), OnDelete = ForeignKeyDelete.Cascade)]
        public GXDeviceVersion? Version { get; set; }

        /// <summary>
        /// Name of the model version settings.
        /// </summary>
        [Description("Name of the model version settings.")]
        [StringLength(128)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        public string? Name { get; set; }

        /// <summary>
        /// Device settings as JSON.
        /// </summary>
        [Description("Device settings as JSON.")]
        [DataMember]
        [DefaultValue(null)]
        public string? Settings { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Installation time.
        /// </summary>
        [Description("Installation time.")]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? InstallationTime { get; set; }

        /// <summary>
        /// Device Template location.
        /// </summary>
        [Description("Device Template location.")]
        [DataMember]
        public string? Location { get; set; }

        /// <summary>
        /// Installed device template.
        /// </summary>
        /// <remarks>
        /// Deletion cascades through Version. This reference does not cascade,
        /// avoiding multiple cascade paths in SQL Server.
        /// </remarks>
        [Description("Installed device template.")]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [DataMember]
        public GXDeviceTemplate? Template { get; set; }

        /// <summary>
        /// Time when device type was removed.
        /// </summary>
        [Description("Time when device type was removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>Time when the device settings was last updated.</summary>
        [Description("Time when the device settings was last updated.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

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
            return nameof(GXDeviceSettings);
        }
    }
}
