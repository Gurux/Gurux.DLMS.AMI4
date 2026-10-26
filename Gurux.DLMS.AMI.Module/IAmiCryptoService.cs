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
