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
        public static string Decrypt(string encrypted, byte[] key, byte[] iv)
        {
            byte[] data = Convert.FromBase64String(encrypted);
            using (MemoryStream ms = new MemoryStream())
            {
                Aes aes = Aes.Create();
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(key, iv), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                }
                return ASCIIEncoding.Unicode.GetString(ms.ToArray());
            }
        }

        /// <summary>
        /// Encrypt the password.
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        public static string Encrypt(string plainText, byte[] key, byte[] iv)
        {
            byte[] data = ASCIIEncoding.Unicode.GetBytes(plainText);
            using (MemoryStream ms = new MemoryStream())
            {
                Aes aes = Aes.Create();
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(key, iv), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }    
}
