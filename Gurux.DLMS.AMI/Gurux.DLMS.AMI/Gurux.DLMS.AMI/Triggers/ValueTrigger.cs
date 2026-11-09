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

using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Triggers
{
    /// <summary>
    /// Value trigger type.
    /// </summary>
    public class ValueTrigger : ITriggerAction
    {
        /// <summary>Published value.received event.</summary>
        [GXTriggerEvent("value.received")]
        public const string Received = "value.received";

        /// <summary>Published value.changed event.</summary>
        [GXTriggerEvent("value.changed")]
        public const string Changed = "value.changed";

        /// <inheritdoc />
        public string Name => "Value";

        /// <inheritdoc />
        public Type? ConfigurationUI => null;

        /// <inheritdoc />
        public string? Icon => null;

        /// <summary>
        /// Value is read.
        /// </summary>
        public const string Read = "Read";
        /// <summary>
        /// Value has changed.
        /// </summary>
        public const string Change = "Change";
        /// <summary>
        /// Value is written.
        /// </summary>
        public const string Write = "Write";
        /// <summary>
        /// Value error is occurred.
        /// </summary>
        public const string Error = "Error";
    }
}
