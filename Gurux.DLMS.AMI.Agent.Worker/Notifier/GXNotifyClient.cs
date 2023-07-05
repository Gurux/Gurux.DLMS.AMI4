//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using Gurux.Common;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Secure;

namespace Gurux.DLMS.AMI.Agent.Worker.Notifier
{
    internal class GXNotifyClient
    {
        public GXNotifyClient(bool useLogicalNameReferencing, 
            int interfaceType, 
            string? systemTitle)
        {
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
        public GXDLMSSecureClient Client
        {
            get;
            set;
        }

        /// <summary>
        /// Received data is saved to reply buffer because whole message is not always received in one packet.
        /// </summary>
        public GXByteBuffer Reply
        {
            get;
            set;
        }

        /// <summary>
        /// Received data. This is used if GBT is used and data is received on several data blocks.
        /// </summary>
        public GXReplyData Notify
        {
            get;
            set;
        }

        /// <summary>
        /// Time when last data was received.
        /// </summary>
        public DateTime DataReceived
        {
            get;
            set;
        }

    }
}
