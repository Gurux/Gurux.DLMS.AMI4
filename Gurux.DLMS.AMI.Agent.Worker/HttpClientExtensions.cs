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
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Agent.Worker
{
    internal static class HttpClientExtensions
    {
        public static async Task<RET?> PostAsJson<RET>(this HttpClient httpClient, string url, object data,
            CancellationToken cancellationToken = default)
        {
            JsonSerializerOptions options = new()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
            };
            string str = JsonSerializer.Serialize(data, options);
            StringContent content = new StringContent(str);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            using (HttpResponseMessage response = await httpClient.PostAsync(url, content, cancellationToken))
            {
                Helpers.ValidateStatusCode(response);
                return await response.Content.ReadFromJsonAsync<RET>();
            }
        }

        public static async Task PostAsJson(this HttpClient httpClient, string url, object data)
        {
            JsonSerializerOptions options = new()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
            };
            string str = JsonSerializer.Serialize(data, options);
            StringContent content = new StringContent(str);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            using (HttpResponseMessage response = await httpClient.PostAsync(url, content))
            {
                Helpers.ValidateStatusCode(response);
            }
        }
    }
}
