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
using System.Xml.Linq;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Culture.
    /// </summary>
    public class GXLanguage : IUnique<string>
    {
        /// <summary>
        /// Language name is the identifier.
        /// </summary>
        [StringLength(5)]
        [Filter(FilterType.Exact)]
        public string Id
        {
            get;
            set;
        }

        /// <summary>
        /// Language English name.
        /// </summary>
        [StringLength(30)]
        public string EnglishName
        {
            get;
            set;
        }

        /// <summary>
        /// Native name.
        /// </summary>
        [StringLength(30)]
        public string NativeName
        {
            get;
            set;
        }

        /// <summary>
        /// Is the language actived.
        /// </summary>
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Active
        {
            get;
            set;
        }

        /// <summary>
        /// Is this the default language.
        /// </summary>
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Default
        {
            get;
            set;
        }

        /// <summary>
        /// Localized strings.
        /// </summary>
        public GXLocalizedResource[]? Resources
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXLanguage()
        {
            Id = "";
            EnglishName = "";
            NativeName = "";
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(EnglishName))
            {
                return EnglishName;
            }
            return nameof(GXLanguage);
        }
    }
}
