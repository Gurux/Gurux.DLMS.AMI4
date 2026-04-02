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
// This file is a part of Gurux DeviceTemplate Framework.
//
// Gurux DeviceTemplate Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux DeviceTemplate Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.UrlAlias
{
    /// <summary>
    /// A data contract class representing url alias to device template group binding object.
    /// </summary>
    [IndexCollection(true, nameof(DeviceTemplateGroupId), nameof(UrlAliasId), Clustered = true)]
    public class GXUrlAliasDeviceTemplateGroup
    {
        /// <summary>
        /// Device template group ID.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXDeviceTemplateGroup), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid DeviceTemplateGroupId
        {
            get;
            set;
        }

        /// <summary>
        /// Url alias ID.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXUrlAlias), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid UrlAliasId
        {
            get;
            set;
        }
    }
}
