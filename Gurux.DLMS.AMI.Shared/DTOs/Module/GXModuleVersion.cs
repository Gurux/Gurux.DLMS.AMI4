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

namespace Gurux.DLMS.AMI.Shared.DTOs.Module
{
    /// <summary>Version and release information for a module.</summary>
    ///
    [Description("Version and release information for a module.")]
    [IndexCollection(true, nameof(Module), nameof(Number))]
    public class GXModuleVersion : IUnique<Guid>
    {
        /// <summary>
        /// Version identifier.
        /// </summary>
        [Description("Version identifier.")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>Whether the module version is enabled.</summary>
        //Filter uses default value.
        [Description("Whether the module version is enabled.")]
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Enabled { get; set; }

        /// <summary>Time when the record was created.</summary>
        [Description("Time when the record was created.")]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Module Version number.
        /// </summary>
        [Description("Module Version number.")]
        [StringLength(128)]
        [DefaultValue(null)]
        public string? Number { get; set; }

        /// <summary>Whether this is a prerelease version.</summary>
        [Description("Whether this is a prerelease version.")]
        public bool Prerelease { get; set; }

        /// <summary>
        /// List of framework versions that can use this module version.
        /// </summary>
        [Description("List of framework versions that can use this module version.")]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? RequiredFrameworkVersions { get; set; }

        /// <summary>
        /// Installation Url.
        /// </summary>
        /// <remarks>
        /// If module is added manually this is null.
        /// </remarks>
        [Description("Installation Url.")]
        [StringLength(2048)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? Url { get; set; }

        /// <summary>
        /// File path.
        /// </summary>
        [Description("File path.")]
        public string? FileName { get; set; }

        /// <summary>
        /// Version description.
        /// </summary>
        [Description("Version description.")]
        public string? Description { get; set; }

        /// <summary>
        /// The module that owns this version.
        /// </summary>
        [Description("The module that owns this version.")]
        [DefaultValue(null)]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        public GXModule? Module { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Number))
            {
                return Number;
            }
            return nameof(GXModuleVersion);
        }
    }
}
