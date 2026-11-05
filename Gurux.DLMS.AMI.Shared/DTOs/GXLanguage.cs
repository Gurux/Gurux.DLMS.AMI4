// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>A supported language and its localization settings.</summary>
    [Description("A supported language and its localization settings.")]
    public class GXLanguage : IUnique<string>
    {
        /// <summary>
        /// Language name is the identifier.
        /// </summary>
        [Description("Language name is the identifier.")]
        [StringLength(5)]
        [Filter(FilterType.Exact)]
        public string Id { get; set; }

        /// <summary>
        /// Language English name.
        /// </summary>
        [Description("Language English name.")]
        [StringLength(30)]
        public string EnglishName { get; set; }

        /// <summary>
        /// Native name.
        /// </summary>
        [Description("Native name.")]
        [StringLength(30)]
        public string NativeName { get; set; }

        /// <summary>Whether the language is enabled.</summary>
        [Description("Whether the language is enabled.")]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }

        /// <summary>Whether this is the default language.</summary>
        [Description("Whether this is the default language.")]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Default { get; set; }

        /// <summary>
        /// Localized strings.
        /// </summary>
        [Description("Localized strings.")]
        public IEnumerable<GXLocalizedResource>? Resources { get; set; }

        /// <summary>Initializes a new instance of GXLanguage.</summary>
        [Description("Initializes a new instance of GXLanguage.")]
        public GXLanguage()
        {
            Id = "";
            EnglishName = "";
            NativeName = "";
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(NativeName))
            {
                if (!string.IsNullOrEmpty(EnglishName))
                {
                    return NativeName + " (" + EnglishName + ")";
                }
                return NativeName;
            }
            return nameof(GXLanguage);
        }
    }
}
