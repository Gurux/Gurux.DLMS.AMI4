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

namespace Gurux.DLMS.AMI.Module.Enums;

/// <summary>Specifies how module-owned data is handled during uninstall.</summary>
public enum ModuleDataRemoval
{
    /// <summary>Preserve module-owned tables and their contents.</summary>
    Preserve = 0,
    /// <summary>Delete module-owned data while preserving table schemas.</summary>
    DeleteData = 1,
    /// <summary>Delete module-owned data and tables.</summary>
    DeleteDataAndTables = 2
}
