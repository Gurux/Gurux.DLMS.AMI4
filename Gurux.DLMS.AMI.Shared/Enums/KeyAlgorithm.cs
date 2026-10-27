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

/// <summary>Supported key algorithms.</summary>
[System.ComponentModel.Description("Supported key algorithms.")]
public enum KeyAlgorithm
{
    /// <summary>No cryptographic algorithm is associated with the secret.</summary>
    [System.ComponentModel.Description("No cryptographic algorithm is associated with the secret.")]
    None = 1,
    /// <summary>Advanced Encryption Standard (AES) symmetric key algorithm.</summary>
    [System.ComponentModel.Description("Advanced Encryption Standard (AES) symmetric key algorithm.")]
    Aes,
    /// <summary>Elliptic Curve Digital Signature Algorithm (ECDSA).</summary>
    [System.ComponentModel.Description("Elliptic Curve Digital Signature Algorithm (ECDSA).")]
    Ecdsa
}
