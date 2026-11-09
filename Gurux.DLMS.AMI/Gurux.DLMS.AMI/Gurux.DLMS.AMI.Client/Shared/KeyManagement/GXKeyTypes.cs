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

namespace Gurux.DLMS.AMI.Client.Shared.KeyManagement;

/// <summary>Purpose of a single authorized operation.</summary>
public enum GXKeyPurpose
{
    /// <summary>Authenticates using a low-level or high-level security password.</summary>
    Password = 1,
    /// <summary>Encrypts data using the key.</summary>
    Encrypt,
    /// <summary>Decrypts previously encrypted data.</summary>
    Decrypt,
    /// <summary>Creates a digital signature using a private key.</summary>
    Sign,
    /// <summary>Verifies a digital signature using a public key.</summary>
    Verify,
    /// <summary>Generates a message authentication code.</summary>
    MacGenerate,
    /// <summary>Verifies a message authentication code.</summary>
    MacVerify,
    /// <summary>Protects another key for transport or storage.</summary>
    Wrap,
    /// <summary>Recovers a key from its wrapped representation.</summary>
    Unwrap
}
