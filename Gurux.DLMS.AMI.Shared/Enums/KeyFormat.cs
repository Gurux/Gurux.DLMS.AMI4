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

/// <summary>Supported key material encodings.</summary>
[System.ComponentModel.Description("Supported key material encodings.")]
public enum KeyFormat
{
    /// <summary>Raw secret or symmetric key bytes.</summary>
    [System.ComponentModel.Description("Raw secret or symmetric key bytes.")]
    Raw = 1,
    /// <summary>DER-encoded PKCS #8 private key.</summary>
    [System.ComponentModel.Description("DER-encoded PKCS #8 private key.")]
    Pkcs8,
    /// <summary>DER-encoded SubjectPublicKeyInfo public key.</summary>
    [System.ComponentModel.Description("DER-encoded SubjectPublicKeyInfo public key.")]
    SubjectPublicKeyInfo,
    /// <summary>Raw elliptic curve private scalar.</summary>
    [System.ComponentModel.Description("Raw elliptic curve private scalar.")]
    EcPrivateScalar,
    /// <summary>Encoded elliptic curve public point.</summary>
    [System.ComponentModel.Description("Encoded elliptic curve public point.")]
    EcPublicPoint,
    /// <summary>DER-encoded X.509 certificate.</summary>
    [System.ComponentModel.Description("DER-encoded X.509 certificate.")]
    X509,
    /// <summary>PEM-encoded private key.</summary>
    [System.ComponentModel.Description("PEM-encoded private key.")]
    PemPrivateKey,
    /// <summary>PEM-encoded public key.</summary>
    [System.ComponentModel.Description("PEM-encoded public key.")]
    PemPublicKey,
    /// <summary>PEM-encoded X.509 certificate.</summary>
    [System.ComponentModel.Description("PEM-encoded X.509 certificate.")]
    PemCertificate
}
