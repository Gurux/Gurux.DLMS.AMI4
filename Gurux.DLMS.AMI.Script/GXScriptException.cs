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

namespace Gurux.DLMS.AMI.Script
{
    /// <summary>
    /// Describes a compiler diagnostic reported while generating a script assembly.
    /// </summary>
    /// <remarks>
    /// This is a diagnostic data object, not a throwable exception. Diagnostics are collected by
    /// <see cref="GXAmiScript.Generate"/> when compilation fails.
    /// </remarks>
    public class GXScriptException
    {
        /// <summary>
        /// Gets or sets the compiler diagnostic identifier. Defaults to an empty string.
        /// </summary>
        [Filter(FilterType.Exact)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the zero-based source line containing the diagnostic.
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// Gets or sets the diagnostic message. Defaults to an empty string.
        /// </summary>
        [Filter(FilterType.Contains)]
        public string Message { get; set; }

        /// <summary>
        /// Initializes a diagnostic with an empty identifier and message.
        /// </summary>
        public GXScriptException()
        {
            Id = "";
            Message = "";
        }

        /// <summary>Formats the diagnostic identifier, zero-based line and message for display.</summary>
        /// <returns>The diagnostic in the form "ID: identifier Line: line, message".</returns>
        public override string ToString()
        {
            return string.Format("ID: {0} Line: {1}, {2}", Id, Line, Message);
        }
    }
}
