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

using System.Text;

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Provides reflection helpers for AMI objects.
    /// </summary>
    [System.ComponentModel.Description("Provides reflection helpers for AMI objects.")]
    public static class Helpers
    {
        /// <summary>
        /// Get objects properties as a string.
        /// </summary>
        /// <param name="target">Target object.</param>
        /// <returns>Target properties as a string.</returns>
        [System.ComponentModel.Description("Get objects properties as a string.")]
        public static string GetProperties(object target)
        {
            StringBuilder sb = new();
            foreach (var it in target.GetType().GetProperties())
            {
                sb.AppendLine(it.Name + ": " + it.GetValue(target));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Get objects properties as a string.
        /// </summary>
        /// <param name="target">Target object.</param>
        /// <returns>Target properties as a string.</returns>
        [System.ComponentModel.Description("Get objects properties as a string.")]
        public static Dictionary<string, object?> GetDictionaryProperties(object target)
        {
            Dictionary<string, object?> list = [];
            foreach (var it in target.GetType().GetProperties())
            {
                list.Add(it.Name, it.GetValue(target));
            }
            return list;
        }
    }
}
