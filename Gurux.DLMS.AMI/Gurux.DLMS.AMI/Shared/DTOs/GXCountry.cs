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
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Country.
    /// </summary>
    public class GXCountry : IUnique<string>
    {
        /// <summary>
        /// Country name is the identifier.
        /// </summary>
        [StringLength(2)]
        [Filter(FilterType.Exact)]
        public string Id
        {
            get;
            set;
        }

        /// <summary>
        /// Country English name.
        /// </summary>
        [StringLength(30)]
        public string EnglishName
        {
            get;
            set;
        }

        /// <summary>
        /// Country flag in SVG.
        /// </summary>
        [StringLength(60)]
        public string Flag
        {
            get;
            set;
        }

        /// <summary>
        /// Is country location in EU area.
        /// </summary>
        public bool InEU { get; set; }

        /// <summary>
        /// Languages.
        /// </summary>
        public GXLanguage[]? Languages
        {
            get;
            set;
        }

        /// <summary>
        /// IP address blocks for the county.
        /// </summary>
        public GXIpAddressBlock[]? Clocks
        {
            get;
            set;
        }
        
    }
}
