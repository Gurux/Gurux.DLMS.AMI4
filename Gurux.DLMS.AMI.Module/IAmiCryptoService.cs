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

namespace Gurux.DLMS.AMI.Module
{
    /// <summary>
    /// Encrypts module secrets and decrypts values protected by the host cryptographic service.
    /// </summary>
    public interface IAmiCryptoService
    {
        /// <summary>
        /// Decrypts a protected byte array.
        /// </summary>
        /// <param name="encrypted">Encrypted bytes produced by this service.</param>
        /// <returns>Decrypted byte array.</returns>
        Task<byte[]> DecryptAsync(byte[] encrypted);

        /// <summary>
        /// Decodes a Base64 encrypted value and decrypts its bytes.
        /// </summary>
        /// <param name="encrypted">Base64 representation of encrypted bytes.</param>
        /// <returns>Decrypted byte array.</returns>
        Task<byte[]> DecryptAsync(string encrypted);

        /// <summary>
        /// Encrypts plaintext bytes using the host cryptographic service.
        /// </summary>
        /// <param name="plainText">Unencrypted bytes to protect.</param>
        /// <returns>Encrypted byte array.</returns>
        Task<byte[]> EncryptAsync(byte[] plainText);

        /// <summary>
        /// Encodes plaintext as UTF-8 and encrypts the resulting bytes.
        /// </summary>
        /// <param name="plainText">Unencrypted text to protect.</param>
        /// <returns>Encrypted byte array.</returns>
        Task<byte[]> EncryptAsync(string plainText);
    }
}
