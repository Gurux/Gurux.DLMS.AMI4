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
    public class GXCryptoService : IAmiCryptoService
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
            using (MemoryStream ms = new MemoryStream(encrypted))
            {
                ms.Read(iv, 0, aes.IV.Length);
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(_key, iv), CryptoStreamMode.Read))
                {
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
            }
        }

        /// <inheritdoc/>
        public Task<string> DecryptAsync(string encrypted)
        {
            byte[] value = Convert.FromBase64String(encrypted);
            value = DecryptAsync(value).Result;
            return Task.FromResult(Convert.ToBase64String(value));
        }

        /// <inheritdoc/>
        public Task<byte[]> EncryptAsync(byte[] plainText)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Aes aes = Aes.Create();
                ms.Write(aes.IV, 0, aes.IV.Length);
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(_key, aes.IV), CryptoStreamMode.Write))
                {
                    cs.Write(plainText);
                }
                return Task.FromResult(ms.ToArray());
            }
        }

        /// <inheritdoc/>
        public Task<string> EncryptAsync(string plainText)
        {
            byte[] value = EncryptAsync(ASCIIEncoding.UTF8.GetBytes(plainText)).Result;
            return Task.FromResult(Convert.ToBase64String(value));
        }
    }
}
