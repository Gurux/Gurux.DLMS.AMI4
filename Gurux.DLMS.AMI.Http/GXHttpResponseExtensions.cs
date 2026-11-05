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

using System.Net;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Http;

/// <summary>Validates AMI responses while retaining server diagnostics.</summary>
public static class GXHttpResponseExtensions
{
    private static string? Text(JsonElement root, string name) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <summary>Throws for unsuccessful responses. The caller retains response ownership.</summary>
    public static async Task EnsureSuccessAsync(this HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode) return;
        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        JsonDocument? document = null;
        try
        {
            try { document = JsonDocument.Parse(content); }
            catch (JsonException) { }
            var root = document?.RootElement ?? default;
            string? title = Text(root, "title"), detail = Text(root, "detail");
            string fallback = $"HTTP {(int)response.StatusCode} ({response.ReasonPhrase})";
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable && Text(root, "type") == "maintenance")
            {
                var maintenance = new GXMaintenanceException(detail ?? "Site is on maintenance mode.");
                if (DateTimeOffset.TryParse(Text(root, "endTime"), out var end)) maintenance.EndTime = end;
                maintenance.RetryAfter = response.Headers.RetryAfter?.Delta ??
                    (response.Headers.RetryAfter?.Date is DateTimeOffset retryAt ? retryAt - DateTimeOffset.UtcNow : null);
                throw maintenance;
            }
            var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("errors", out var validation) &&
                validation.ValueKind == JsonValueKind.Object)
            {
                foreach (var field in validation.EnumerateObject())
                {
                    if (field.Value.ValueKind == JsonValueKind.Array)
                        errors[field.Name] = field.Value.EnumerateArray().Where(v => v.ValueKind == JsonValueKind.String)
                            .Select(v => v.GetString()!).ToArray();
                    else if (field.Value.ValueKind == JsonValueKind.String)
                        errors[field.Name] = [field.Value.GetString()!];
                }
            }
            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(title)) lines.Add(title);
            if (!string.IsNullOrWhiteSpace(detail)) lines.Add(detail);
            foreach (var field in errors)
            {
                if (field.Key == "DuplicateUserName") lines.AddRange(field.Value.Take(1));
                else lines.AddRange(field.Value.Select(message => $"{field.Key}: {message}"));
            }
            string message = string.Join(Environment.NewLine, lines);
            if (string.IsNullOrWhiteSpace(message))
            {
                string? explanation = root.ValueKind == JsonValueKind.String ? root.GetString() :
                    document == null ? content.Trim() : null;
                message = string.IsNullOrWhiteSpace(explanation) ? fallback : $"{fallback}: {explanation}";
            }
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                throw new UnauthorizedAccessException(detail ?? title ?? message);
            throw new GXHttpException(message, response.StatusCode, title, detail, Text(root, "traceId"), errors);
        }
        finally { document?.Dispose(); }
    }
}
