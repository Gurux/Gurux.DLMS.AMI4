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
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Device
{
    /// <summary>
    /// A data contract class representing device group to device group binding object.
    /// </summary>
    [DataContract(Name = "GXDeviceGroupDevice"), Serializable]
    [IndexCollection(true, nameof(DeviceId), nameof(DeviceGroupId), Clustered = true)]
    public class GXDeviceGroupDevice
    {
        /// <summary>
        /// Device ID.
        /// </summary>
        [DataMember(Name = "DeviceID")]
        [ForeignKey(typeof(GXDevice), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid DeviceId
        {
            get;
            set;
        }

        /// <summary>
        /// Device Group ID.
        /// </summary>
        [DataMember(Name = "DeviceGroupID")]
        [ForeignKey(typeof(GXDeviceGroup), OnDelete = ForeignKeyDelete.None)]
        [IsRequired]
        public Guid DeviceGroupId
        {
            //ForeignKeyDelete is None because Device will handle the deletion.
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// The time when the device was added to the device group.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Time when device was removed from device group.
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
    }
}
