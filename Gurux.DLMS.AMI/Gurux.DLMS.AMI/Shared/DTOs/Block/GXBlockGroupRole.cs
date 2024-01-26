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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Block
{
    /// <summary>
    /// Block to role map.
    /// </summary>
    [Serializable]
    [IndexCollection(true, nameof(BlockGroupId), nameof(RoleId), Clustered = true)]
    public class GXBlockGroupRole
    {
        [ForeignKey(typeof(GXBlockGroup), OnDelete = ForeignKeyDelete.Cascade)]
        [DataMember]
        [StringLength(36)]
        [IsRequired]
        public string BlockGroupId
        {
            get;
            set;
        }

        [DataMember]
        [StringLength(36)]
        [ForeignKey(typeof(GXRole), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public string RoleId
        {
            get;
            set;
        }
    }
}
