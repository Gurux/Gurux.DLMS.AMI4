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
