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

/// <summary>Permitted operations; these are application values, not KMIP wire values.</summary>
[System.ComponentModel.Description("Permitted operations; these are application values, not KMIP wire values.")]
[Flags]
public enum KeyUsage
{
    /// <summary>No key operations are permitted.</summary>
    [System.ComponentModel.Description("No key operations are permitted.")]
    None = 0,
    /// <summary>Use the secret as an authentication password.</summary>
    [System.ComponentModel.Description("Use the secret as an authentication password.")]
    Password = 1,
    /// <summary>Encrypt data.</summary>
    [System.ComponentModel.Description("Encrypt data.")]
    Encrypt = 2,
    /// <summary>Decrypt data.</summary>
    [System.ComponentModel.Description("Decrypt data.")]
    Decrypt = 4,
    /// <summary>Generate digital signatures.</summary>
    [System.ComponentModel.Description("Generate digital signatures.")]
    Sign = 8,
    /// <summary>Verify digital signatures.</summary>
    [System.ComponentModel.Description("Verify digital signatures.")]
    Verify = 16,
    /// <summary>Generate message authentication codes (MACs).</summary>
    [System.ComponentModel.Description("Generate message authentication codes (MACs).")]
    MacGenerate = 32,
    /// <summary>Verify message authentication codes (MACs).</summary>
    [System.ComponentModel.Description("Verify message authentication codes (MACs).")]
    MacVerify = 64,
    /// <summary>Wrap other keys to protect their key material.</summary>
    [System.ComponentModel.Description("Wrap other keys to protect their key material.")]
    Wrap = 128,
    /// <summary>Unwrap protected key material.</summary>
    [System.ComponentModel.Description("Unwrap protected key material.")]
    Unwrap = 256
}
