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
