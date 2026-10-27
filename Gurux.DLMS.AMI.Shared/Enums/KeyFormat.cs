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
