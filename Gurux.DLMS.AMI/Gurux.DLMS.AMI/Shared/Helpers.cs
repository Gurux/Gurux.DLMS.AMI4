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

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// This class implements helper functions to communicate with Gurux.DLMS.AMI.Server.
    /// </summary>
    public static class Helpers
    {
        public static async Task<RET> PostAsJson<RET>(this HttpClient client, string requestUri, object value, CancellationToken cancellationToken = default)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
            };
            HttpResponseMessage response = await client.PostAsJsonAsync(requestUri, value, options, cancellationToken);
            ValidateStatusCode(response);
            var ret = await response.Content.ReadFromJsonAsync<RET>();
            if (ret == null)
            {
                throw new Exception("Invalid reply.");
            }
            return ret;
        }

        public static async Task PostAsJson(this HttpClient client, string requestUri, object value, CancellationToken cancellationToken = default)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
            };
            HttpResponseMessage response = await client.PostAsJsonAsync(requestUri, value, options, cancellationToken);
            ValidateStatusCode(response);
        }

        public static async Task<RET> GetAsJsonAsync<RET>(this HttpClient client, string requestUri, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await client.GetAsync(requestUri, cancellationToken);
            ValidateStatusCode(response);
            var ret = await response.Content.ReadFromJsonAsync<RET>();
            if (ret == null)
            {
                throw new Exception("Invalid reply.");
            }
            return ret;
        }

        public static void ValidateStatusCode(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    string? error = response.Content.ReadAsStringAsync().Result;
                    if (string.IsNullOrEmpty(error))
                    {
                        error = response.ReasonPhrase;
                    }
                    throw new GXAmiNotFoundException(error);
                }
                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new Exception(response.ReasonPhrase);
                }
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    string? error = response.Content.ReadAsStringAsync().Result;
                    if (string.IsNullOrEmpty(error))
                    {
                        error = response.ReasonPhrase;
                    }
                    GXMaintenanceException e1 = new GXMaintenanceException(error);
                    if (response.Headers.RetryAfter != null && response.Headers.RetryAfter.Delta.HasValue
                        && response.Headers.RetryAfter.Delta.Value.TotalSeconds != 0)
                    {
                        e1.RetryAfter = response.Headers.RetryAfter.Delta;
                        e1.EndTime = DateTime.Now + response.Headers.RetryAfter.Delta;
                    }
                    throw e1;
                }
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    string error = response.Content.ReadAsStringAsync().Result;
                    throw new Exception(error);
                }
                else
                {
                    Exception? ex = null;
                    try
                    {
                        ex = JsonSerializer.Deserialize<GXAmiException>(response.Content.ReadAsStringAsync().Result);
                    }
                    catch (Exception)
                    {
                        ex = null;
                    }
                    if (ex != null)
                    {
                        throw ex;
                    }
                    response.EnsureSuccessStatusCode();
                }
            }
            else if (response.StatusCode == HttpStatusCode.NoContent)
            {
                throw new Exception("No content is returned from the server.");
            }
        }

        /// <summary>
        /// Get objects properties as a string.
        /// </summary>
        /// <param name="target">Target object.</param>
        /// <returns>Target properties as a string.</returns>
        public static string GetProperties(object target)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var it in target.GetType().GetProperties())
            {
                sb.AppendLine(it.Name + ": " + it.GetValue(target));
            }
            return sb.ToString();
        }
    }
}
