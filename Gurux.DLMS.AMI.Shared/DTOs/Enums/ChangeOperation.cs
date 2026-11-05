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
    /// <summary>Specifies the operation performed on the entity by a recorded change.</summary>
    [System.ComponentModel.Description("Specifies the operation performed on the entity by a recorded change.")]
    public enum ChangeOperation
    {
        /// <summary>The entity was created; the history item has no previous JSON snapshot.</summary>
        [System.ComponentModel.Description("The entity was created; the history item has no previous JSON snapshot.")]
        [XmlEnum("0")]
        Create,
        /// <summary>The entity was modified; the history item contains JSON snapshots before and after the change.</summary>
        [System.ComponentModel.Description("The entity was modified; the history item contains JSON snapshots before and after the change.")]
        [XmlEnum("1")]
        Update,
        /// <summary>The entity was deleted; the history item has no subsequent JSON snapshot.</summary>
        [System.ComponentModel.Description("The entity was deleted; the history item has no subsequent JSON snapshot.")]
        [XmlEnum("2")]
        Delete
    }
}
