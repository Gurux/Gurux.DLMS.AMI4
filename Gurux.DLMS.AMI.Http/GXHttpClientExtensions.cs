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

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Http
{
    /// <summary>Sends JSON requests using the common AMI response validation contract.</summary>
    public static class GXHttpClientExtensions
    {
        /// <summary>
        /// Sends a JSON-encoded request to the specified URI and reads the response as a specified type.
        /// </summary>
        /// <typeparam name="RET">The type of the response content.</typeparam>
        /// <param name="client">The HttpClient used to send the request.</param>
        /// <param name="requestUri">The URI to which the request is sent.</param>
        /// <param name="value">The object to serialize and send as the request content.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The deserialized response content of type RET.</returns>
        /// <exception cref="Exception">Thrown when the response content is null.</exception>
        [System.ComponentModel.Description("Sends a JSON-encoded request to the specified URI and reads the response as a specified type.")]
        public static async Task<RET> PostAsJson<RET>(this HttpClient client, string requestUri, object? value, CancellationToken cancellationToken = default)
        {
            JsonSerializerOptions options = new()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
            };
            using HttpResponseMessage response = await client.PostAsJsonAsync(requestUri, value, options, cancellationToken);
            await response.EnsureSuccessAsync(cancellationToken);
            var ret = await response.Content.ReadFromJsonAsync<RET>(cancellationToken: cancellationToken) ?? throw new Exception("Invalid reply.");
            return ret;
        }

        /// <summary>
        /// Post object as JSON to the server.
        /// </summary>
        /// <param name="client">HttpClient instance.</param>
        /// <param name="requestUri">Request URI.</param>
        /// <param name="value">Object to be sent as JSON.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Server reply.</returns>
        [System.ComponentModel.Description("Post object as JSON to the server.")]
        public static async Task PostAsJson(this HttpClient client, string requestUri, object value, CancellationToken cancellationToken = default)
        {
            JsonSerializerOptions options = new()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
            };
            using HttpResponseMessage response = await client.PostAsJsonAsync(requestUri, value, options, cancellationToken);
            await response.EnsureSuccessAsync(cancellationToken);
        }

        /// <summary>
        /// Get object from the server as JSON.
        /// </summary>
        /// <typeparam name="RET">Type of the object to be returned.</typeparam>
        /// <param name="client">HttpClient instance.</param>
        /// <param name="requestUri">Request URI.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Deserialized object from the server response.</returns>
        /// <exception cref="Exception">Thrown when the server response is invalid.</exception>
        [System.ComponentModel.Description("Get object from the server as JSON.")]
        public static async Task<RET> GetAsJsonAsync<RET>(this HttpClient client, string requestUri, CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await client.GetAsync(requestUri, cancellationToken);
            await response.EnsureSuccessAsync(cancellationToken);
            var ret = await response.Content.ReadFromJsonAsync<RET>(cancellationToken: cancellationToken) ?? throw new Exception("Invalid reply.");
            return ret;
        }

    }
}
