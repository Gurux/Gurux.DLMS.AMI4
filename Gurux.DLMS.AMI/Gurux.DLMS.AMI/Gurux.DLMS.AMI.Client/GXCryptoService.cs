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

using Gurux.DLMS.AMI.Module;
using System.Net.Http.Headers;
using System.Text;

namespace Gurux.DLMS.AMI.Client
{
    internal class GXCryptoService(IServiceProvider serviceProvider) : IGXAmiCryptoService
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        private async Task<byte[]> ReadAsync(byte[] encrypted, string url)
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            var http = scope.ServiceProvider.GetRequiredService<HttpClient>();
            var content = new ByteArrayContent(encrypted);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        public async Task<byte[]> DecryptAsync(byte[] encrypted)
        {
            return await ReadAsync(encrypted, "api/Decrypt");
        }

        public async Task<byte[]> EncryptAsync(byte[] plainText)
        {
            return await ReadAsync(plainText, "api/Encrypt");
        }

        public async Task<byte[]> DecryptAsync(string encrypted)
        {
            byte[] value = await ReadAsync(Convert.FromBase64String(encrypted), "api/Decrypt");
            return value;
        }

        public async Task<byte[]> EncryptAsync(string plainText)
        {
            byte[] value = await ReadAsync(ASCIIEncoding.UTF8.GetBytes(plainText), "api/Encrypt");
            return value;
        }
    }
}