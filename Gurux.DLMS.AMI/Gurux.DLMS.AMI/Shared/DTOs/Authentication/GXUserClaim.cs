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

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>
    /// User claims table.
    /// </summary>
    [DataContract(Name = "GXUserClaim"), Serializable]
    public class GXUserClaim : IUnique<int>
    {
        /// <summary>
        /// Identifier.
        /// </summary>
        [Key]
        [DataMember(Name = "ID"), Index(Unique = true), AutoIncrement]
        public int Id
        {
            get;
            set;
        }

        [DataMember]
        [Index]
        [IsRequired]
        [ForeignKey(typeof(GXUser), OnDelete = ForeignKeyDelete.Cascade)]
        [StringLength(36)]
        public string UserId
        {
            get;
            set;
        }

        [DataMember]
        public string ClaimType
        {
            get;
            set;
        }

        [DataMember]
        public string ClaimValue
        {
            get;
            set;
        }
    }
}
