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

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Gurux.DLMS.AMI.Server.Internal
{
    /// <summary>
    /// Passwords are saved as encrypted to database.
    /// </summary>
    public class GXPasswordCrypto
    {
        /// <summary>
        /// Decrypt the password.
        /// </summary>
        /// <param name="encrypted"></param>
        /// <returns></returns>
        /// <param name="key">The key.</param>
        /// <param name="iv">The iv.</param>
        public static string Decrypt(string encrypted, byte[] key, byte[] iv)
        {
            byte[] data = Convert.FromBase64String(encrypted);
            using MemoryStream ms = new();
            Aes aes = Aes.Create();
            using (CryptoStream cs = new(ms, aes.CreateEncryptor(key, iv), CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
            }
            return ASCIIEncoding.Unicode.GetString(ms.ToArray());
        }

        /// <summary>
        /// Encrypt the password.
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        /// <param name="key">The key.</param>
        /// <param name="iv">The iv.</param>
        public static string Encrypt(string plainText, byte[] key, byte[] iv)
        {
            byte[] data = ASCIIEncoding.Unicode.GetBytes(plainText);
            using MemoryStream ms = new();
            Aes aes = Aes.Create();
            using (CryptoStream cs = new(ms, aes.CreateEncryptor(key, iv), CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
            }
            return Convert.ToBase64String(ms.ToArray());
        }
    }
}
