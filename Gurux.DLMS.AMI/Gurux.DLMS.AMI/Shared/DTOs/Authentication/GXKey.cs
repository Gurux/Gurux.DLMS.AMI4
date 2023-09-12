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
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>
    /// Keys table.
    /// </summary>
    [DataContract(Name = "GXKey"), Serializable]
    public partial class GXKey : IUnique<string>
    {
        /// <summary>
        /// Identifier.
        /// </summary>
        [StringLength(450)]
        [Key]
        [DataMember(Name = "ID"), Index(Unique = true)]
        [Filter(FilterType.Exact)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string Id
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            get;
            set;
        }

        /// <summary>
        /// Key version.
        /// </summary>
        [DataMember]
        public int Version
        {
            get;
            set;
        }

        /// <summary>
        /// When key is created.
        /// </summary>
        [DataMember]
        public DateTime Created
        {
            get;
            set;
        }

        [Index(Unique=false)]
        [DataMember]
        [StringLength(450)]
        public string? Use
        {
            get;
            set;
        }

        /// <summary>
        /// Key algorithm.
        /// </summary>
        [DataMember]
        [StringLength(100)]
        public string? Algorithm
        {
            get;
            set;
        }


        [DataMember]
        [IsRequired]
        public bool IsX509Certificate
        {
            get;
            set;
        }

        [DataMember]
        [IsRequired]
        public bool DataProtected
        {
            get;
            set;
        }

        [DataMember]
        public string? Data
        {
            get;
            set;
        }       
    }
}
