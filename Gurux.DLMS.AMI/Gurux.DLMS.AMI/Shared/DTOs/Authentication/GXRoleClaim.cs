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
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>
    /// Role claims table.
    /// </summary>
    [DataContract(Name = "GXRoleClaim"), Serializable]
    public class GXRoleClaim : IUnique<int>
    {
        /// <summary>
        ///Identifier. 
        /// </summary>
        [Key]
        [DataMember(Name = "ID"), Index]
        [AutoIncrement]
        public int Id
        {
            get;
            set;
        }

        /// <summary>
        /// Role ID.
        /// </summary>
        [DataMember]
        [StringLength(36)]
        [Index(Unique = false)]
        [IsRequired]
        [ForeignKey(typeof(GXRole), OnDelete = ForeignKeyDelete.Cascade)]
        public string RoleId
        {
            get;
            set;
        } = "";

        /// <summary>
        /// Claim type.
        /// </summary>
        [DataMember]
        public string? ClaimType
        {
            get;
            set;
        }

        /// <summary>
        /// Claim value.
        /// </summary>
        [DataMember]
        public string? ClaimValue
        {
            get;
            set;
        }
    }
}
