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
    /// <summary>Associates a device template with a device template group.</summary>
    [Description("Associates a device template with a device template group.")]
    [DataContract(Name = "GXDeviceTemplateGroupDeviceTemplate"), Serializable]
    [IndexCollection(true, nameof(DeviceTemplateGroupId), nameof(DeviceTemplateId), Clustered = true)]
    public class GXDeviceTemplateGroupDeviceTemplate : GXTableBase
    {
        /// <summary>The database identifier of the Schedule group.</summary>
        [Description("The database identifier of the Schedule group.")]
        [DataMember(Name = "DeviceTemplateGroupId")]
        [ForeignKey(typeof(GXDeviceTemplateGroup), OnDelete = ForeignKeyDelete.None)]
        [IsRequired]
        //ForeignKeyDelete is None because creator of the device template is causing multiple cascade paths error in MSSQL.
        public Guid DeviceTemplateGroupId { get; set; }

        /// <summary>The database identifier of the schedule.</summary>
        [Description("The database identifier of the schedule.")]
        [DataMember(Name = "DeviceTemplateId")]
        [ForeignKey(typeof(GXDeviceTemplate), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid DeviceTemplateId { get; set; }

        /// <summary>
        /// Creation time.
        /// The time when the device template was added to the device template group.
        /// </summary>
        [Description("Creation time. The time when the device template was added to the device template group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when device template was removed from device template group.
        /// </summary>
        [Description("Time when device template was removed from device template group.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>Initializes the creation timestamp before insertion.</summary>
        [Description("Initializes the creation timestamp before insertion.")]
        public override void BeforeAdd()
        {
            CreationTime = DateTime.Now;
        }
    }
}
