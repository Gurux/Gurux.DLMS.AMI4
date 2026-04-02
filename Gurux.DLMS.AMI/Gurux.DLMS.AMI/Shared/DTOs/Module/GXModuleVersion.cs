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
    /// <summary>
    /// Gurux.DLMS.AMI module version information.
    /// </summary>
    /// 
    [IndexCollection(true, nameof(Module), nameof(Number))]
    public class GXModuleVersion : IUnique<Guid>
    {
        /// <summary>
        /// Version identifier.
        /// </summary>
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Is version active.
        /// </summary>
        //Filter uses default value.
        [DefaultValue(true)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? Active { get; set; }

        /// <summary>
        /// Creation time.
        /// </summary>
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Module Version number.
        /// </summary>
        [StringLength(20)]
        [DefaultValue(null)]
        public string? Number
        {
            get;
            set;
        }

        /// <summary>
        /// Is this a pre-release version.
        /// </summary>
        public bool Prerelease
        {
            get;
            set;
        }

        /// <summary>
        /// Installation Url.
        /// </summary>
        /// <remarks>
        /// If module is added manually this is null.
        /// </remarks>
        [StringLength(256)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public string? Url
        {
            get;
            set;
        }

        /// <summary>
        /// File path.
        /// </summary>
        public string? FileName
        {
            get;
            set;
        }

        /// <summary>
        /// Version description.
        /// </summary>
        public string? Description
        {
            get;
            set;
        }

        /// <summary>
        /// The module that owns this version.
        /// </summary>
        [DefaultValue(null)]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        public GXModule? Module
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Number;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXModuleVersion()
        {
            Number = "";
        }
    }
}