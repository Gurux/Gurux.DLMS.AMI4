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

namespace Gurux.DLMS.AMI.Shared.Enums
{
    /// <summary>Resource operations used by permissions, filtering, and audit records.</summary>
    [System.ComponentModel.Description("Resource operations used by permissions, filtering, and audit records.")]
    public enum CrudAction : byte
    {
        /// <summary>No resource operation is specified.</summary>
        [System.ComponentModel.Description("No resource operation is specified.")]
        None = 0,
        /// <summary>Create a resource.</summary>
        [System.ComponentModel.Description("Create a resource.")]
        Create = 0x1,
        /// <summary>Read an individual resource.</summary>
        [System.ComponentModel.Description("Read an individual resource.")]
        Read = 0x2,
        /// <summary>Update an existing resource.</summary>
        [System.ComponentModel.Description("Update an existing resource.")]
        Update = 0x4,
        /// <summary>Delete selected resources.</summary>
        [System.ComponentModel.Description("Delete selected resources.")]
        Delete = 0x8,
        /// <summary>List resources matching a query.</summary>
        [System.ComponentModel.Description("List resources matching a query.")]
        List = 0x10,
        /// <summary>Clear resources within the selected scope.</summary>
        [System.ComponentModel.Description("Clear resources within the selected scope.")]
        Clear = 0x20,
        /// <summary>
        /// Agent asks the next task.
        /// </summary>
        [System.ComponentModel.Description("Agent asks the next task.")]
        Next = 0x40,
        /// <summary>
        /// Clone target object.
        /// </summary>
        [System.ComponentModel.Description("Clone target object.")]
        Clone = Next,
    }
}