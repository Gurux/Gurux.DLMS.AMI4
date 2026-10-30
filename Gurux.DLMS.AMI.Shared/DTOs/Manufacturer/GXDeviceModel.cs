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
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Manufacturer
{
    /// <summary>A device model offered by a manufacturer.</summary>
    [Description("A device model offered by a manufacturer.")]
    public class GXDeviceModel : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXDeviceModel.</summary>
        [Description("Initializes a new instance of GXDeviceModel.")]
        public GXDeviceModel()
        {

        }

        /// <summary>Initializes a new instance of GXDeviceModel.</summary>
        /// <param name="name">Model name.</param>
        [Description("Initializes a new instance of GXDeviceModel.")]
        public GXDeviceModel(string? name)
        {
            Name = name;
            Versions = [];
        }

        /// <summary>Device type identifier.</summary>
        [Description("Device type identifier.")]
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// The creator of the device model.
        /// </summary>
        [Description("The creator of the device model.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Gets or sets who can view the device model.
        /// </summary>
        /// <remarks>
        /// Modification, deletion and operation permissions are evaluated separately.
        /// </remarks>
        [Description("Gets or sets who can view the device model.")]
        [DataMember]
        [DefaultValue(ResourceVisibility.Inherit)]
        [IsRequired]
        public ResourceVisibility? AccessVisibility { get; set; }

        /// <summary>
        /// GXAppearance image used as the manufacturer's profile picture; null means no picture.
        /// </summary>
        [Description("GXAppearance image used as the manufacturer's profile picture; null means no picture.")]
        [DataMember, ForeignKey(OnDelete = ForeignKeyDelete.Restrict)]
        [DefaultValue(null)]
        public GXAppearance? ProfilePicture { get; set; }

        /// <summary>
        /// Manufacturer who owns this model.
        /// </summary>
        [Description("Manufacturer who owns this model.")]
        [DefaultValue(null)]
        [ForeignKey(typeof(GXManufacturer), OnDelete = ForeignKeyDelete.Cascade)]
        public GXManufacturer? Manufacturer { get; set; }

        /// <summary>
        /// List of device versions.
        /// </summary>
        [Description("List of device versions.")]
        [DataMember]
        [ForeignKey(typeof(GXDeviceVersion))]
        [Filter(FilterType.Contains)]
        public List<GXDeviceVersion>? Versions { get; set; }

        /// <summary>
        /// Device model name.
        /// </summary>
        [Description("Device model name.")]
        [StringLength(128)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        public string? Name { get; set; }

        /// <summary>
        /// Optional url where is more information from this model.
        /// </summary>
        [Description("Optional url where is more information from this model.")]
        [StringLength(128)]
        [DefaultValue(null)]
        public string? Url { get; set; }

        /// <summary>
        /// Device picture.
        /// </summary>
        [Description("Device picture.")]
        [DefaultValue(null)]
        public string? Picture { get; set; }

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

        /// <summary>Time when the device model was last updated.</summary>
        [Description("Time when the device model was last updated.")]
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
            return typeof(GXDeviceModel).Name;
        }
    }
}
