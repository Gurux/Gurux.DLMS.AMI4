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
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Device
{
    /// <summary>
    /// Device action table is used to save send and receive meter data to the DB.
    /// </summary>
    public class GXDeviceAction : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Request Id.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
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
        /// Device Id.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [Filter(FilterType.Equals)]
        [IsRequired]
        public GXDevice? Device
        {
            get;
            set;
        }

        /// <summary>
        /// Action.
        /// </summary>
        [Filter(FilterType.Equals)]
        [IsRequired]
        [DefaultValue(null)]
        public DeviceActionType? Type
        {
            get;
            set;
        }

        /// <summary>
        /// Device action data.
        /// </summary>
        [DataMember]
        public string? Data
        {
            get;
            set;
        }

        /// <summary>
        /// Action reply.
        /// </summary>
        /// <remarks>
        /// Exception is saved here.
        /// </remarks>
        [DataMember]
        public string? Reply
        {
            get;
            set;
        }

        /// <summary>
        /// Update creation time before update.
        /// </summary>
        public override void BeforeAdd()
        {
            if (CreationTime == DateTime.MinValue)
            {
                CreationTime = DateTime.Now;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (Device != null && !string.IsNullOrEmpty(Device.Name))
            {
                return Type.ToString() + " " + Device.Name;
            }
            return Type.ToString();
        }
    }
}
