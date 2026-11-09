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

using System.Security.Cryptography;
using System.Text;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Module;
using Microsoft.Extensions.Options;

namespace Gurux.DLMS.AMI.Services
{
    /// <summary>
    /// Provides cryptographic services for encrypting and decrypting data using a symmetric key.
    /// </summary>
    /// <remarks>This class supports both string and byte array encryption and decryption operations. The
    /// cryptographic key must be 32 bytes in length and is typically provided via configuration. Instances of this
    /// class are not thread-safe; concurrent access should be synchronized if used in multi-threaded
    /// scenarios.</remarks>
    public class GXCryptoService : IGXAmiCryptoService
    {
        private readonly byte[] _key;

        /// <summary>
        /// Constrctor.
        /// </summary>
        /// <param name="options"></param>
        public GXCryptoService(IOptions<CryptoOptions> options)
        {
            _key = GXDLMSTranslator.HexToBytes(options.Value.Key);
            if (_key.Length != 32)
            {
                throw new Exception("Invalid crypto key.");
            }
        }

        /// <inheritdoc/>
        public Task<byte[]> DecryptAsync(byte[] encrypted)
        {
            using Aes aes = Aes.Create();
            byte[] iv = new byte[aes.IV.Length];
            byte[] data = new byte[encrypted.Length - aes.IV.Length];
            using MemoryStream ms = new(encrypted);
            ms.Read(iv, 0, aes.IV.Length);
            using CryptoStream cs = new(ms, aes.CreateDecryptor(_key, iv), CryptoStreamMode.Read);
            int total = 0;
            while (total < data.Length)
            {
                int amount = cs.Read(data, total, data.Length - total);
                if (amount == 0)
                {
                    break;
                }
                total += amount;
            }
            byte[] tmp = new byte[total];
            Array.Copy(data, tmp, total);
            return Task.FromResult(tmp);
        }

        /// <inheritdoc/>
        public async Task<byte[]> DecryptAsync(string encrypted)
        {
            byte[] value = Convert.FromBase64String(encrypted);
            return await DecryptAsync(value);
        }

        /// <inheritdoc/>
        public Task<byte[]> EncryptAsync(byte[] plainText)
        {
            using MemoryStream ms = new();
            Aes aes = Aes.Create();
            ms.Write(aes.IV, 0, aes.IV.Length);
            using (CryptoStream cs = new(ms, aes.CreateEncryptor(_key, aes.IV), CryptoStreamMode.Write))
            {
                cs.Write(plainText);
            }
            return Task.FromResult(ms.ToArray());
        }

        /// <inheritdoc/>
        public async Task<byte[]> EncryptAsync(string plainText)
        {
            return await EncryptAsync(ASCIIEncoding.UTF8.GetBytes(plainText));
        }
    }
}
