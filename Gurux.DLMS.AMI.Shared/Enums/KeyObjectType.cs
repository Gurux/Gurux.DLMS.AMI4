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

namespace Gurux.DLMS.AMI.Shared.Enums;

/// <summary>Managed secret representation.</summary>
[System.ComponentModel.Description("Managed secret representation.")]
public enum KeyObjectType
{
    /// <summary>Secret data, such as an authentication password.</summary>
    [System.ComponentModel.Description("Secret data, such as an authentication password.")]
    SecretData = 1,
    /// <summary>Symmetric cryptographic key.</summary>
    [System.ComponentModel.Description("Symmetric cryptographic key.")]
    SymmetricKey,
    /// <summary>Private key of an asymmetric key pair.</summary>
    [System.ComponentModel.Description("Private key of an asymmetric key pair.")]
    PrivateKey,
    /// <summary>Public key of an asymmetric key pair.</summary>
    [System.ComponentModel.Description("Public key of an asymmetric key pair.")]
    PublicKey,
    /// <summary>Certificate containing a public key.</summary>
    [System.ComponentModel.Description("Certificate containing a public key.")]
    Certificate
}
