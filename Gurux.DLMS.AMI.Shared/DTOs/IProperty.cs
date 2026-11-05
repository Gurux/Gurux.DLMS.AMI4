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

using Gurux.DLMS.AMI.Shared.DTOs.Module;
using System.ComponentModel;
using Gurux.Service.Orm.Common;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Common identity, value and version contract for named properties associated with resources.
    /// </summary>
    [Description("Common identity, value and version contract for named properties associated with resources.")]
    public interface IGXProperty : IUnique<Guid>
    {
        /// <summary>Writable property identifier.</summary>
        [Description("Writable property identifier.")]
        new Guid Id { get; set; }

        /// <summary>
        /// Module settings unique name.
        /// </summary>
        [Description("Module settings unique name.")]
        public string? Name { get; set; }


        /// <summary>
        /// The module whose settings these are.
        /// </summary>
        [Description("The module whose settings these are.")]
        public GXModule? Module { get; set; }

        /// <summary>
        /// Module settings.
        /// </summary>
        [Description("Module settings.")]
        public string? Value { get; set; }

        /// <summary>
        /// Version expected by optimistic updates and deletes.
        /// </summary>
        [Description("Version expected by optimistic updates and deletes.")]
        public string? ConcurrencyStamp { get; set; }
    }
}
