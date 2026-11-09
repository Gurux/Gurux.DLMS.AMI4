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

namespace Gurux.DLMS.AMI.Client.Shared
{
    /// <summary>
    /// Prune base settings.
    /// </summary>
    public class PruneBase
    {
        /// <summary>
        /// Prune interval in minutes.
        /// </summary>
        public int Interval { get; set; } = 0;

        /// <summary>
        /// Are items removed permamently.
        /// </summary>
        /// <remarks>
        /// If false, items are only disabled and not removed from the database. 
        /// </remarks>
        public bool Delete { get; set; }

        /// <summary>
        /// The maximum number of items to delete during a run.
        /// </summary>
        /// <remarks>
        /// If the value is zero, there are no restrictions.
        /// </remarks>
        public int Maximum { get; set; }

        /// <summary>
        /// last execution time.
        /// </summary>
        public DateTimeOffset? Run { get; set; }
    }
}