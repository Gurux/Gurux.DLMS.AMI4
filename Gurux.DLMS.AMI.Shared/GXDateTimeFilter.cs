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

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Specifies an inclusive date and time range for a property in a list query.
    /// </summary>
    /// <remarks>
    /// Either bound can be omitted to define an open-ended range. When both bounds
    /// are specified, a value must satisfy both. Null property values do not match
    /// a specified bound. The receiving service validates the property and applies
    /// the filter; this class only describes the requested range.
    /// </remarks>
    public sealed class GXDateTimeFilter
    {
        /// <summary>
        /// Gets or sets the name of the date and time property to filter.
        /// </summary>
        /// <remarks>
        /// Use the DTO property name, for example <c>nameof(GXUser.LastLogin)</c>.
        /// The property must be supported by the receiving list operation.
        /// </remarks>
        public required string Property { get; set; }

        /// <summary>
        /// Gets or sets the inclusive lower bound. Values must be greater than or
        /// equal to this bound. Null means that no lower bound is applied.
        /// </summary>
        public DateTimeOffset? From { get; set; }

        /// <summary>
        /// Gets or sets the inclusive upper bound. Values must be less than or
        /// equal to this bound. Null means that no upper bound is applied.
        /// </summary>
        public DateTimeOffset? To { get; set; }
    }
}
