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

using Gurux.Common;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Secure;
using Gurux.DLMS.AMI.Shared.DTOs.Script;

namespace Gurux.DLMS.AMI.Agent.Worker.Notifier
{
    internal class GXNotifyClient
    {
        public GXNotifyClient(bool useLogicalNameReferencing,
            int interfaceType,
            string? systemTitle, GXScriptMethod? scriptMethod = null)
        {
            ScriptMethod = scriptMethod;
            Notify = new GXReplyData();
            Reply = new GXByteBuffer();
            Client = new GXDLMSSecureClient(useLogicalNameReferencing, -1, -1, Authentication.None, null, (InterfaceType)interfaceType);
            if (systemTitle != null)
            {
                Client.Ciphering.SystemTitle = GXCommon.HexToBytes(systemTitle);
            }
            Client.Ciphering.BlockCipherKey = null;
            Client.Ciphering.AuthenticationKey = null;
            DataReceived = DateTime.MinValue;
        }

        /// <summary>
        /// Client used to parse received data.
        /// </summary>
        public GXDLMSSecureClient Client { get; set; }

        /// <summary>Prepared script version captured when this connection begins.</summary>
        public GXScriptMethod? ScriptMethod { get; set; }

        /// <summary>
        /// Received data is saved to reply buffer because whole message is not always received in one packet.
        /// </summary>
        public GXByteBuffer Reply { get; set; }

        /// <summary>
        /// Received data. This is used if GBT is used and data is received on several data blocks.
        /// </summary>
        public GXReplyData Notify { get; set; }

        /// <summary>
        /// Time when last data was received.
        /// </summary>
        public DateTime DataReceived { get; set; }

    }
}
