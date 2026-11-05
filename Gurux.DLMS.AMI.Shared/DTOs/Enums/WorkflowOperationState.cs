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
    /// <summary>Identifies the current lifecycle state of a workflow operation.</summary>
    /// <remarks>An operation starts as Running and transitions to one terminal state: Completed, Failed, or Cancelled.</remarks>
    public enum WorkflowOperationState : byte
    {
        /// <summary>The operation has started and has no recorded terminal outcome.</summary>
        [XmlEnum("0")]
        Running = 0,
        /// <summary>The operation finished successfully.</summary>
        [XmlEnum("1")]
        Completed = 1,
        /// <summary>The operation finished with a failure.</summary>
        [XmlEnum("2")]
        Failed = 2,
        /// <summary>The operation ended because it was cancelled.</summary>
        [XmlEnum("3")]
        Cancelled = 3
    }
}

