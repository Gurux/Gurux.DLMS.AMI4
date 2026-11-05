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
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Module
{
    /// <summary>An external assembly associated with a module.</summary>
    [System.ComponentModel.Description("An external assembly associated with a module.")]
    public class GXModuleAssembly : IUnique<Guid>
    {
        /// <summary>
        /// Assembly identifier.
        /// </summary>
        [System.ComponentModel.Description("Assembly identifier.")]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// Assembly file name.
        /// </summary>
        [System.ComponentModel.Description("Assembly file name.")]
        [StringLength(64)]
        public string? FileName { get; set; }

        /// <summary>
        /// Parent module.
        /// </summary>
        [System.ComponentModel.Description("Parent module.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        public GXModule? Module { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(FileName))
            {
                return FileName;
            }
            return nameof(GXModuleAssembly);
        }
    }
}
