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
