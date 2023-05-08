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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Manufacturer
{
    /// <summary>
    /// Device model table is used to save manufacturer device models.
    /// </summary>
    public class GXDeviceModel : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXDeviceModel()
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Model name.</param>
        public GXDeviceModel(string? name)
        {
            Name = name;
            Versions = new List<GXDeviceVersion>();
        }

        /// <summary>
        /// Device type ID.
        /// </summary>
        [DataMember(Name = "ID")]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Manufacturer who owns this model.
        /// </summary>
        [DefaultValue(null)]
        [ForeignKey(typeof(GXManufacturer), OnDelete = ForeignKeyDelete.Cascade)]
        public GXManufacturer? Manufacturer
        {
            get;
            set;
        }

        /// <summary>
        /// List of device versions.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDeviceVersion))]
        [Filter(FilterType.Contains)]
        public List<GXDeviceVersion>? Versions
        {
            get;
            set;
        }

        /// <summary>
        /// Device model name.
        /// </summary>
        [StringLength(128)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Optional url where is more information from this model.
        /// </summary>
        [StringLength(128)]
        [DefaultValue(null)]
        public string? Url
        {
            get; set;
        }

        /// <summary>
        /// Device picture.
        /// </summary>
        [DefaultValue(null)]
        public string? Picture
        {
            get; set;
        }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Time when device type was removed.
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
        /// When the device type is updated for the last time.
        /// </summary>
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// Update creation time before update.
        /// </summary>
        public override void BeforeAdd()
        {
            if (CreationTime == null)
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
            return typeof(GXDeviceModel).Name;
        }
    }
}
