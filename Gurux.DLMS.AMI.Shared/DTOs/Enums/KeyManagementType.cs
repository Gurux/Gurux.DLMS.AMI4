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

namespace Gurux.DLMS.AMI.Shared.DTOs.Enums
{
    /// <summary>The purpose of a key or password in DLMS/COSEM security.</summary>
    [System.ComponentModel.Description("The purpose of a key or password in DLMS/COSEM security.")]
    [Flags]
    public enum KeyManagementType
    {
        /// <summary>No security key type is selected.</summary>
        [System.ComponentModel.Description("No security key type is selected.")]
        None = 0,
        /// <summary>Password used for low-level security authentication.</summary>
        [System.ComponentModel.Description("Password used for low-level security authentication.")]
        LLSPassword = 0x1,
        /// <summary>Secret used for high-level security authentication.</summary>
        [System.ComponentModel.Description("Secret used for high-level security authentication.")]
        HLSPassword = 0x2,
        /// <summary>
        /// Global unicast encryption key (GUEK).
        /// </summary>
        [System.ComponentModel.Description("Global unicast encryption key (GUEK).")]
        BlockCipher = 0x4,
        /// <summary>
        /// Authentication key.
        /// </summary>
        [System.ComponentModel.Description("Authentication key.")]
        Authentication = 0x8,
        /// <summary>
        /// Global broadcast encryption key (GBEK).
        /// </summary>
        [System.ComponentModel.Description("Global broadcast encryption key (GBEK).")]
        Broadcast = 0x10,
        /// <summary>
        /// Master key, also known as Key Encrypting Key.
        /// </summary>
        /// <remarks>
        /// Master key is used to update new Block cipher
        /// or authentication keys to the meter.
        /// </remarks>
        [System.ComponentModel.Description("Master key, also known as Key Encrypting Key.")]
        MasterKey = 0x20,
        /// <summary>
        /// ECDSA private key.
        /// </summary>
        [System.ComponentModel.Description("ECDSA private key.")]
        PrivateKey = 0x40,
        /// <summary>
        /// ECDSA public key.
        /// </summary>
        [System.ComponentModel.Description("ECDSA public key.")]
        PublicKey = 0x80
    }
}
