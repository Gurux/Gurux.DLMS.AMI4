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
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Manufacturer
{
    /// <summary>A device manufacturer and its available device models.</summary>
    public class GXManufacturer : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXManufacturer()
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Manufacturer's name.</param>
        public GXManufacturer(string? name)
        {
            Name = name;
            Models = new List<GXDeviceModel>();
            ManufacturerGroups = new List<GXManufacturerGroup>();
        }

        /// <summary>
        /// Favorite ID.
        /// </summary>
        [DataMember(Name = "ID")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// Manufacturer Name.
        /// </summary>
        [StringLength(128)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        public string? Name { get; set; }

        /// <summary>
        /// Manufacturer template settings are used to show download information.
        /// </summary>
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Template { get; set; }

        /// <summary>
        /// Manufacturer logo.
        /// </summary>
        [DefaultValue(null)]
        public string? Logo { get; set; }

        /// <summary>
        /// Manufacturer url.
        /// </summary>
        [StringLength(128)]
        [DefaultValue(null)]
        public string? Url { get; set; }

        /// <summary>
        /// List of manufacturer device models.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDeviceModel))]
        [Filter(FilterType.Contains)]
        public List<GXDeviceModel>? Models { get; set; }

        /// <summary>Manufacturer groups containing this manufacturer.</summary>
        [DataMember, ForeignKey(typeof(GXManufacturerGroup), typeof(GXManufacturerGroupManufacturer))]
        [Filter(FilterType.Contains)]
        public List<GXManufacturerGroup>? ManufacturerGroups { get; set; }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when manufacturer was removed.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>
        /// When the block is updated for the last time.
        /// </summary>
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

        /// <summary>Revision of the manufacturer profiles installed in AMI.</summary>
        [DataMember, StringLength(64)]
        public string? Version { get; set; }

        /// <summary>Latest manufacturer profile revision recorded by cron.</summary>
        [DataMember, StringLength(64)]
        public string? AvailableVersion { get; set; }

        /// <summary>Whether at least one profile belonging to this manufacturer is installed.</summary>
        [DataMember]
        public bool ProfilesInstalled { get; set; }


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
