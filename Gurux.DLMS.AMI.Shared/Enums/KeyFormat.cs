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
public enum KeyFormat
{
    /// <summary>Raw secret or symmetric key bytes.</summary>
    Raw = 1,
    /// <summary>DER-encoded PKCS #8 private key.</summary>
    Pkcs8,
    /// <summary>DER-encoded SubjectPublicKeyInfo public key.</summary>
    SubjectPublicKeyInfo,
    /// <summary>Raw elliptic curve private scalar.</summary>
    EcPrivateScalar,
    /// <summary>Encoded elliptic curve public point.</summary>
    EcPublicPoint,
    /// <summary>DER-encoded X.509 certificate.</summary>
    X509,
    /// <summary>PEM-encoded private key.</summary>
    PemPrivateKey,
    /// <summary>PEM-encoded public key.</summary>
    PemPublicKey,
    /// <summary>PEM-encoded X.509 certificate.</summary>
    PemCertificate
}
