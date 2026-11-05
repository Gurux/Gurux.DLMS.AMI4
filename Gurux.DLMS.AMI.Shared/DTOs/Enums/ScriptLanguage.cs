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

using System.Xml.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Enums
{
    /// <summary>The programming language used to compile a script.</summary>
    [System.ComponentModel.Description("The programming language used to compile a script.")]
    public enum ScriptLanguage : byte
    {
        /// <summary>Compile the script as C# source code.</summary>
        [System.ComponentModel.Description("Compile the script as C# source code.")]
        [XmlEnum("0")]
        CSharp = 0,
        /// <summary>Compile the script as Visual Basic source code.</summary>
        [System.ComponentModel.Description("Compile the script as Visual Basic source code.")]
        [XmlEnum("1")]
        VisualBasic = 1
    }
}
