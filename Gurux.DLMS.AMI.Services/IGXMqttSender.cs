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

namespace Gurux.DLMS.AMI.Services
{
    /// <summary>
    /// Publishes application messages to MQTT topics.
    /// </summary>
    public interface IGXMqttSender
    {
        /// <summary>
        /// Asynchronously publishes a text message to an MQTT topic.
        /// </summary>
        /// <param name="sender">Sender identity supplied to the MQTT service.</param>
        /// <param name="topic">MQTT topic.</param>
        /// <param name="message">Text payload to publish.</param>
        /// <returns>A task representing the asynchronous send operation.</returns>
        Task SendMqttAsync(string sender, string topic, string message);
    }
}
