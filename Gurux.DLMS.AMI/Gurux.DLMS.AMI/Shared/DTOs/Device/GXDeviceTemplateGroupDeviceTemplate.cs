﻿//
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

namespace Gurux.DLMS.AMI.Shared.DTOs.Device
{
    /// <summary>
    /// A data contract class representing device template group to device template binding object.
    /// </summary>
    [DataContract(Name = "GXDeviceTemplateGroupDeviceTemplate"), Serializable]
    [IndexCollection(true, nameof(DeviceTemplateGroupId), nameof(DeviceTemplateId), Clustered = true)]
    public class GXDeviceTemplateGroupDeviceTemplate : GXTableBase
    {
        /// <summary>
        /// The database ID of the Schedule group.
        /// </summary>
        [DataMember(Name = "DeviceTemplateGroupId")]
        [ForeignKey(typeof(GXDeviceTemplateGroup), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid DeviceTemplateGroupId
        {
            get;
            set;
        }

        /// <summary>
        /// The database ID of the schedule.
        /// </summary>
        [DataMember(Name = "DeviceTemplateId")]
        [ForeignKey(typeof(GXDeviceTemplate), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid DeviceTemplateId
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// The time when the device template was added to the device template group.
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
        /// Time when device template was removed from device template group.
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
        /// Update Creation time.
        /// </summary>
        public override void BeforeAdd()
        {
            CreationTime = DateTime.Now;
        }
    }
}
