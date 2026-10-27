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
