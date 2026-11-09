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
    /// Device trigger type.
    /// </summary>
    public class DeviceTrigger : ITriggerAction
    {
        /// <summary>Published device.read-started event.</summary>
        [GXTriggerEvent("device.read-started")]
        public const string ReadStarted = "device.read-started";

        /// <summary>Published device.read-completed event.</summary>
        [GXTriggerEvent("device.read-completed")]
        public const string ReadCompleted = "device.read-completed";

        /// <summary>Published device.read-failed event.</summary>
        [GXTriggerEvent("device.read-failed")]
        public const string ReadFailed = "device.read-failed";

        /// <summary>Published device.read-cancelled event.</summary>
        [GXTriggerEvent("device.read-cancelled")]
        public const string ReadCancelled = "device.read-cancelled";

        /// <summary>Published device.read-finished event.</summary>
        [GXTriggerEvent("device.read-finished")]
        public const string ReadFinished = "device.read-finished";

        /// <summary>Published device.add event.</summary>
        [GXTriggerEvent("device.add")]
        public const string Added = "device.add";

        /// <summary>Published device.edit event.</summary>
        [GXTriggerEvent("device.edit")]
        public const string Edited = "device.edit";

        /// <summary>Published device.delete event.</summary>
        [GXTriggerEvent("device.delete")]
        public const string Deleted = "device.delete";

        /// <summary>Published device.connected event.</summary>
        [GXTriggerEvent("device.connected")]
        public const string ReadConnected = "device.connected";

        /// <summary>Published device.disconnected event.</summary>
        [GXTriggerEvent("device.disconnected")]
        public const string ReadDisconnected = "device.disconnected";

        /// <inheritdoc />
        public string Name => "Device";

        /// <inheritdoc />
        public Type? ConfigurationUI => null;

        /// <inheritdoc />
        public string? Icon => "oi oi-tablet";

        /// <summary>
        /// A new device is added.
        /// </summary>
        /// <remarks>
        /// This trigger can be used to update the block cipher and authentication keys when the connection is stableished for the first time.
        /// </remarks>
        public const string Add = "Add";
        /// <summary>
        /// The connected is established to the meter for the first time.
        /// </summary>
        /// <remarks>
        /// This trigger can be used to update the block cipher and authentication keys when the connection is stableished for the first time.
        /// </remarks>
        public const string Initialize = "Initialize";
        /// <summary>
        /// The connected is established to the meter.
        /// </summary>
        /// <remarks>
        /// This trigger can be used to update the block cipher and authentication keys when the connection is stableished for the first time.
        /// </remarks>
        /// <remarks>
        /// This trigger can be used to update the clock for the meter.
        /// </remarks>
        public const string Connected = "Connected";
        /// <summary>
        /// The meter is disconnecing.
        /// </summary>
        public const string Disconnecting = "Disconnecting";
        /// <summary>
        /// Device is updated.
        /// </summary>
        public const string Updated = "Updated";
        /// <summary>
        /// Device error is occurred.
        /// </summary>
        public const string Error = "Error";
        /// <summary>
        /// Device is removed.
        /// </summary>
        public const string Removed = "Removed";
    }
}
