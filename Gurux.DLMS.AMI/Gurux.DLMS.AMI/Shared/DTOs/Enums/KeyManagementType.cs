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
// More information of Gurux products: https://www.gurux.org
//
// This code is licensed under the GNU General Public License v2. 
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

namespace Gurux.DLMS.AMI.Shared.DTOs.Enums
{
    /// <summary>
    /// Defines key management key type.
    /// </summary>
    [Flags]
    public enum KeyManagementType
    {
        /// <summary>
        /// None security.
        /// </summary>
        None = 0,
        /// <summary>
        /// Low level security.
        /// </summary>
        LLSPassword = 0x1,
        /// <summary>
        /// High level security.
        /// </summary>
        HLSPassword = 0x2,
        /// <summary>
        /// Global unicast encryption key (GUEK).
        /// </summary>
        BlockCipher = 0x4,
        /// <summary>
        /// Authentication key.
        /// </summary>
        Authentication = 0x8,
        /// <summary>
        /// Global broadcast encryption key (GBEK).
        /// </summary>
        Broadcast = 0x10,
        /// <summary>
        /// Master key, also known as Key Encrypting Key.
        /// </summary>
        /// <remarks>
        /// Master key is used to update new Block cipher 
        /// or authentication keys to the meter.
        /// </remarks>
        MasterKey = 0x20,
        /// <summary>
        /// ECDSA private key.
        /// </summary>
        PrivateKey = 0x40,
        /// <summary>
        /// ECDSA public key.
        /// </summary>
        PublicKey = 0x80
    }
}
