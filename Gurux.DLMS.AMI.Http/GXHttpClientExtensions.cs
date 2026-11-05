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

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Http
{
    /// <summary>Sends JSON requests using the common AMI response validation contract.</summary>
    public static class GXHttpClientExtensions
    {
        private static readonly JsonSerializerOptions DefaultRequestOptions = CreateDefaultRequestOptions();

        private static JsonSerializerOptions CreateDefaultRequestOptions()
        {
            var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };
            options.MakeReadOnly(populateMissingResolver: true);
            return options;
        }
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
        public static Task<RET> PostAsJson<RET>(this HttpClient client, string requestUri, object? value, CancellationToken cancellationToken = default)
            => PostAsJson<RET>(client, requestUri, value, cancellationToken, null);

        /// <summary>Sends JSON using custom request serialization settings. Response parsing uses web defaults.</summary>
        public static async Task<RET> PostAsJson<RET>(this HttpClient client, string requestUri, object? value,
            CancellationToken cancellationToken, JsonSerializerOptions? options)
        {
            options ??= DefaultRequestOptions;
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
        public static Task PostAsJson(this HttpClient client, string requestUri, object value, CancellationToken cancellationToken = default)
            => PostAsJson(client, requestUri, value, cancellationToken, null);

        /// <summary>Sends JSON using custom request serialization settings without reading a reply.</summary>
        public static async Task PostAsJson(this HttpClient client, string requestUri, object value,
            CancellationToken cancellationToken, JsonSerializerOptions? options)
        {
            options ??= DefaultRequestOptions;
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

