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

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// IP addresses block for country.
    /// </summary>
    public class GXIpAddressBlock : IUnique<Guid>
    {
        /// <summary>
        /// Identifier.
        /// </summary>
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Country.
        /// </summary>
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        public GXLanguage? Country
        {
            get;
            set;
        }

        /// <summary>
        /// From IP address.
        /// </summary>
        [Index(Unique = false)]
        public UInt64 From
        {
            get;
            set;
        }
        /// <summary>
        /// To IP address.
        /// </summary>
        [Index(Unique = false)]
        public UInt64 To
        {
            get;
            set;
        }

        /// <summary>
        /// Owner of the IP address block.
        /// </summary>
        [StringLength(64)]
        [Index(Unique = false)]
        public string? Owner
        {
            get;
            set;
        }
    }
}
