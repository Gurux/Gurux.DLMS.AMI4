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
