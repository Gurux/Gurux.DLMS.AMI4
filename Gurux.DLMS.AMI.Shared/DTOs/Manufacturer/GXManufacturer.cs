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
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Manufacturer
{
    /// <summary>A device manufacturer and its available device models.</summary>
    [Description("A device manufacturer and its available device models.")]
    public class GXManufacturer : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXManufacturer.</summary>
        [Description("Initializes a new instance of GXManufacturer.")]
        public GXManufacturer()
        {

        }

        /// <summary>Initializes a new instance of GXManufacturer.</summary>
        /// <param name="name">Manufacturer's name.</param>
        [Description("Initializes a new instance of GXManufacturer.")]
        public GXManufacturer(string? name)
        {
            Name = name;
            Models = [];
            ManufacturerGroups = [];
        }

        /// <summary>Favorite identifier.</summary>
        [Description("Favorite identifier.")]
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// The creator of the manufacturer.
        /// </summary>
        [Description("The creator of the manufacturer.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Gets or sets who can view the manufacturer.
        /// </summary>
        /// <remarks>
        /// Modification, deletion and operation permissions are evaluated separately.
        /// </remarks>
        [Description("Gets or sets who can view the manufacturer.")]
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
        /// Manufacturer Name.
        /// </summary>
        [Description("Manufacturer Name.")]
        [StringLength(128)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        public string? Name { get; set; }

        /// <summary>
        /// Manufacturer template settings are used to show download information.
        /// </summary>
        [Description("Manufacturer template settings are used to show download information.")]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Template { get; set; }

        /// <summary>
        /// Manufacturer logo.
        /// </summary>
        [Description("Manufacturer logo.")]
        [DefaultValue(null)]
        public string? Logo { get; set; }

        /// <summary>
        /// Manufacturer url.
        /// </summary>
        [Description("Manufacturer url.")]
        [StringLength(128)]
        [DefaultValue(null)]
        public string? Url { get; set; }

        /// <summary>
        /// List of manufacturer device models.
        /// </summary>
        [Description("List of manufacturer device models.")]
        [DataMember]
        [ForeignKey(typeof(GXDeviceModel))]
        [Filter(FilterType.Contains)]
        public List<GXDeviceModel>? Models { get; set; }

        /// <summary>Manufacturer groups containing this manufacturer.</summary>
        [Description("Manufacturer groups containing this manufacturer.")]
        [DataMember, ForeignKey(typeof(GXManufacturerGroup), typeof(GXManufacturerGroupManufacturer))]
        [Filter(FilterType.Contains)]
        public List<GXManufacturerGroup>? ManufacturerGroups { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when manufacturer was removed.
        /// </summary>
        [Description("Time when manufacturer was removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>Time when the manufacturer was last updated.</summary>
        [Description("Time when the manufacturer was last updated.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

        /// <summary>Revision of the manufacturer profiles installed in AMI.</summary>
        [Description("Revision of the manufacturer profiles installed in AMI.")]
        [DataMember, StringLength(64)]
        public string? Version { get; set; }

        /// <summary>Latest manufacturer profile revision recorded by cron.</summary>
        [Description("Latest manufacturer profile revision recorded by cron.")]
        [DataMember, StringLength(64)]
        public string? AvailableVersion { get; set; }

        /// <summary>Whether at least one profile belonging to this manufacturer is installed.</summary>
        [Description("Whether at least one profile belonging to this manufacturer is installed.")]
        [DataMember]
        public bool ProfilesInstalled { get; set; }


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

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }
            return typeof(GXManufacturer).Name;
        }
    }
}
