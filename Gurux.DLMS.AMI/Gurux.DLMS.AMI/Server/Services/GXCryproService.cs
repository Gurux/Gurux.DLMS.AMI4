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
    /// This service is used to encrypt and descrypt passwords.
    /// </summary>
    public class GXCryproService : IAmiCryproService
    {
        private readonly byte[] key;

        /// <summary>
        /// Constrctor.
        /// </summary>
        /// <param name="options"></param>
        public GXCryproService(IOptions<CryptoOptions> options)
        {
            key = GXDLMSTranslator.HexToBytes(options.Value.Key);
            if (key.Length != 16)
            {
                throw new Exception("Invalid crypto key.");
            }
        }

        /// <inheritdoc/>
        public string Decrypt(string encrypted)
        {
            return Decrypt(encrypted, key);
        }

        /// <summary>
        /// Encrypt the plain text.
        /// </summary>
        /// <param name="plainText">Plain text.</param>
        /// <returns>Encrypted text</returns>
        public string Encrypt(string plainText)
        {
            return Encrypt(plainText, key);
        }

        /// <summary>
        /// Decrypt the password.
        /// </summary>
        /// <param name="encrypted">Encrypted text.</param>
        /// <param name="key">Encryption key.</param>
        /// <returns>Decrypted text.</returns>
        public static string Decrypt(string encrypted, byte[] key)
        {
            byte[] tmp = Convert.FromBase64String(encrypted);
            byte[] iv = new byte[16];
            byte[] data = new byte[tmp.Length - 16];
            using (MemoryStream ms = new MemoryStream(tmp))
            {
                ms.Read(iv, 0, 16);
                Aes aes = Aes.Create();
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(key, iv), CryptoStreamMode.Read))
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
                    return ASCIIEncoding.Unicode.GetString(data, 0, total);
                }
            }
        }

        /// <summary>
        /// Encrypt the password.
        /// </summary>
        /// <param name="plainText">Encrypted text.</param>
        /// <param name="key">Encryption key.</param>
        /// <returns>Encrypted text.</returns>
        public static string Encrypt(string plainText, byte[] key)
        {
            byte[] data = ASCIIEncoding.Unicode.GetBytes(plainText);
            using (MemoryStream ms = new MemoryStream())
            {
                Aes aes = Aes.Create();
                ms.Write(aes.IV, 0, 16);
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(key, aes.IV), CryptoStreamMode.Write))
                {
                    cs.Write(data);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        /// <inheritdoc/>
        public byte[] Decrypt(byte[] encrypted)
        {
            byte[] iv = new byte[16];
            byte[] data = new byte[encrypted.Length - 16];
            using (MemoryStream ms = new MemoryStream(encrypted))
            {
                ms.Read(iv, 0, 16);
                Aes aes = Aes.Create();
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(key, iv), CryptoStreamMode.Read))
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
                    return tmp;
                }
            }
        }

        /// <inheritdoc/>
        public byte[] Encrypt(byte[] plainText)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Aes aes = Aes.Create();
                ms.Write(aes.IV, 0, 16);
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(key, aes.IV), CryptoStreamMode.Write))
                {
                    cs.Write(plainText);
                }
                return ms.ToArray();
            }
        }
    }
}
