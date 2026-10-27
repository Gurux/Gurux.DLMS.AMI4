//
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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Manufacturer
{
    /// <summary>Version information for a manufacturer's device model.</summary>
    [Description("Version information for a manufacturer's device model.")]
    public class GXDeviceVersion : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXDeviceVersion.</summary>
        [Description("Initializes a new instance of GXDeviceVersion.")]
        public GXDeviceVersion()
        {

        }

        /// <summary>Initializes a new instance of GXDeviceVersion.</summary>
        /// <param name="name">Version name.</param>
        [Description("Initializes a new instance of GXDeviceVersion.")]
        public GXDeviceVersion(string? name)
        {
            Name = name;
            Settings = [];
        }

        /// <summary>Device version identifier.</summary>
        [Description("Device version identifier.")]
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// Version model.
        /// </summary>
        [Description("Version model.")]
        [DefaultValue(null)]
        [ForeignKey(typeof(GXDeviceModel), OnDelete = ForeignKeyDelete.Cascade)]
        public GXDeviceModel? Model { get; set; }

        /// <summary>
        /// Name of the version version.
        /// </summary>
        /// <remarks>
        /// Generic settings are used for all device versions if the name is empty.
        /// </remarks>
        [Description("Name of the version version.")]
        [StringLength(128)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        public string? Name { get; set; }

        /// <summary>
        /// List of device settings.
        /// </summary>
        [Description("List of device settings.")]
        [DataMember]
        [Filter(FilterType.Contains)]
        public List<GXDeviceSettings>? Settings { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when device type was removed.
        /// </summary>
        [Description("Time when device type was removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>Time when the device version was last updated.</summary>
        [Description("Time when the device version was last updated.")]
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
            return "Generic";
        }
    }
}
