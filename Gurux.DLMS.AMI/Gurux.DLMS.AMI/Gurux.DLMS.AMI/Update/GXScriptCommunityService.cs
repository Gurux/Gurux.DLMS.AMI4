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
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Client.Rest;
using Microsoft.AspNetCore.DataProtection;

namespace Gurux.DLMS.AMI.Update;

/// <summary>Anonymous community publishing with durable owner-only receipts.</summary>
public sealed class GXScriptCommunityService(IGXScriptCommunityStore store, IServiceProvider services,
    IHttpClientFactory clients, IDataProtectionProvider protection, ILogger<GXScriptCommunityService> logger)
{
    /// <summary>Manages durable community submissions.</summary>
    public const string RemoteBase = "https://ami.gurux.fi/api/modules/Gurux.AMI.ScriptManagement/";
    /// <summary>Anonymous device profile receiver owned by the DeviceProfiles module.</summary>
    public const string DeviceProfilesBase = "https://ami.gurux.fi/api/modules/Gurux.DLMS.AMI.DeviceProfiles/";
    /// <summary>Serializes operations that mutate shared state in this component.</summary>
    private static readonly SemaphoreSlim _gate = new(1, 1);
    /// <summary>Protects stored community credentials with the configured data protection provider.</summary>
    private readonly IDataProtector _protector = protection.CreateProtector("Gurux.AMI.ScriptCommunity.v1");
    /// <summary>Serializer settings shared by requests or persisted state in this component.</summary>
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    /// <summary>Submission receipt and opaque tracking credential returned by the community service.</summary>
    /// <param name="Id">Remote identity assigned to the community submission.</param>
    /// <param name="Kind">Community content category assigned to the receipt.</param>
    /// <param name="Message">Public status explanation returned by the community service.</param>
    /// <param name="TrackingToken">Opaque credential used to refresh the submission status.</param>
    private sealed record Receipt(Guid Id, string? Kind, string? Message, string? TrackingToken);
    /// <summary>Public status and approved version returned for a remote community submission.</summary>
    /// <param name="Id">Remote identity assigned to the community submission.</param>
    /// <param name="Status">Current remote submission review or publishing state.</param>
    /// <param name="Version">Catalog or assembly version represented by this record.</param>
    private sealed record RemoteStatus(Guid Id, string Status, string? Version);

    /// <summary>Returns only the requesting owner’s unacknowledged receipts.</summary>
    public async Task<List<GXScriptCommunitySubmission>> ListAsync(string owner, CancellationToken token)
        => (await store.ReadAsync(token)).Where(row => row.OwnerId == owner && !row.Acknowledged).OrderByDescending(row => row.Created).Select(row => row.ToResponse()).ToList();

    /// <summary>Reads authorized persisted source and submits it anonymously.</summary>
    public async Task<GXScriptCommunitySubmission> PublishAsync(string owner, Guid id, CancellationToken token)
    {
        // Repository construction requires an authenticated request; receipt polling runs without HttpContext.
        var repository = services.GetRequiredService<IScriptRepository>();
        var script = await repository.ReadAsync(id, null, token);
        if (script.Removed != null || script.Language != ScriptLanguage.CSharp || string.IsNullOrWhiteSpace(script.SourceCode))
        {
            throw new ArgumentException("Select an available C# script with source code.");
        }
        var bytes = Encoding.UTF8.GetBytes(script.SourceCode);
        if (bytes.Length > 64 * 1024)
        {
            throw new ArgumentException("Community scripts must contain at most 64 KiB of UTF-8 source.");
        }
        if (!string.IsNullOrWhiteSpace(script.Namespaces))
        {
            var image = repository.Compile(script.Id.ToString("N"), script.SourceCode, null, [], out var errors, out _, token);
            if (image == null || image.Length == 0 || !string.IsNullOrWhiteSpace(errors))
            {
                throw new ArgumentException("Community templates do not carry additional namespace settings. Use fully qualified type names before publishing.");
            }
        }
        // Include the publication license so an older GPL receipt cannot suppress a CC0 submission.
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("CC0-1.0\0" + script.SourceCode)));
        await _gate.WaitAsync(token);
        try
        {
            var existing = (await store.ReadAsync(token)).FirstOrDefault(row => row.OwnerId == owner && row.ContentType != "DeviceTemplate" && row.ScriptId == id && row.SourceHash == hash);
            if (existing != null && existing.Status != "Failed")
            {
                return existing.ToResponse();
            }
            var identifier = script.CatalogId;
            if (string.IsNullOrWhiteSpace(identifier))
            {
                identifier = Regex.Replace(script.Name ?? "", "[^A-Za-z0-9_.-]", "_").Trim('_', '.', '-');
                if (identifier.Length == 0 || identifier.Length > 150)
                {
                    identifier = "Script-" + id.ToString("N");
                }
            }
            var stem = Regex.Replace(script.Name ?? "", @"\.csx?$", "", RegexOptions.IgnoreCase);
            stem = Regex.Replace(stem, "[^A-Za-z0-9_.-]", "_").Trim('_', '.', '-');
            if (stem.Length == 0 || stem.Length > 180)
            {
                stem = "Script";
            }
            if (!Regex.IsMatch(stem, "^[A-Za-z_]") || Regex.IsMatch(stem, @"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])($|\.)", RegexOptions.IgnoreCase))
            {
                stem = "Script-" + stem;
            }
            var fileName = stem + ".cs";
            var request = JsonSerializer.Serialize(new
            {
                scriptId = identifier, fileName, source = script.SourceCode,
                description = string.IsNullOrWhiteSpace(script.Description) ? script.Name : script.Description,
                author = "Anonymous", license = "CC0-1.0", amiVersionRange = "[4.1,5.0)", baseVersion = script.CatalogVersion
            }, _json);
            var row = existing ?? new GXScriptCommunityRecord
            {
                Id = Guid.NewGuid(), OwnerId = owner, ScriptId = id, SourceHash = hash,
                ScriptName = (script.Name ?? identifier)[..Math.Min((script.Name ?? identifier).Length, 200)],
                Created = DateTimeOffset.UtcNow
            };
            row.RemoteId = Guid.Empty;
            row.ProtectedToken = null;
            row.Version = null;
            row.Status = "Publishing";
            row.Acknowledged = false;
            row.FailureMessage = null;
            await store.SaveAsync(row, existing == null, token);
            await RefreshOneAsync(row, token, request);
            return row.ToResponse();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Exports authorized saved metadata and submits an anonymous CC0 device profile.</summary>
    public async Task<GXScriptCommunitySubmission> PublishDeviceTemplateAsync(string owner, GXPublishCommunityDeviceTemplate request, CancellationToken token)
    {
        var repository = services.GetRequiredService<IDeviceTemplateRepository>();
        var template = await repository.ReadAsync(request.Id, null, token);
        var payload = GXDeviceTemplateCommunityExport.Create(template, request);
        var body = JsonSerializer.Serialize(payload, _json);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body)));
        await _gate.WaitAsync(token);
        try
        {
            var existing = (await store.ReadAsync(token)).FirstOrDefault(row => row.OwnerId == owner &&
                row.ContentType == "DeviceTemplate" && row.ScriptId == request.Id && row.SourceHash == hash);
            if (existing != null && existing.Status != "Failed")
            {
                return existing.ToResponse();
            }
            var name = template.Name ?? request.Model;
            var row = existing ?? new GXScriptCommunityRecord
            {
                Id = Guid.NewGuid(), OwnerId = owner, ScriptId = request.Id, SourceHash = hash,
                ScriptName = name[..Math.Min(name.Length, 200)], ContentType = "DeviceTemplate", Created = DateTimeOffset.UtcNow
            };
            row.RemoteId = Guid.Empty;
            row.ProtectedToken = null;
            row.Version = null;
            row.Status = "Publishing";
            row.Acknowledged = false;
            row.FailureMessage = null;
            await store.SaveAsync(row, existing == null, token);
            await RefreshOneAsync(row, token, body);
            return row.ToResponse();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Acknowledges a terminal outcome belonging to the requesting owner.</summary>
    public async Task AcknowledgeAsync(string owner, Guid id, CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            var row = (await store.ReadAsync(token)).FirstOrDefault(row => row.OwnerId == owner && row.Id == id);
            if (row != null && row.Status is "Approved" or "Rejected" or "Failed")
            {
                row.Acknowledged = true;
                await store.SaveAsync(row, false, token);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Refreshes known receipts without resending ambiguous publications.</summary>
    public async Task RefreshAsync(CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            foreach (var row in (await store.ReadAsync(token)).Where(row => row.Status is "Publishing" or "Pending"))
            {
                token.ThrowIfCancellationRequested();
                if (row.RemoteId == Guid.Empty)
                {
                    // A process restart may follow remote acceptance. Never resubmit an ambiguous send automatically.
                    row.Status = "Failed";
                    await store.SaveAsync(row, false, token);
                }
                else
                {
                    await RefreshOneAsync(row, token);
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Publishes an explicitly supplied community submission or refreshes the tracked submission's remote status.</summary>
    /// <param name="row">The tracked community submission whose receipt or status is refreshed.</param>
    /// <param name="token">Cancels the database or remote operation.</param>
    /// <param name="sourceRequest">The explicit JSON publication payload required for a new remote submission.</param>
    /// <returns>A task that completes after the submission record has been updated from the remote result.</returns>
    private async Task RefreshOneAsync(GXScriptCommunityRecord row, CancellationToken token, string? sourceRequest = null)
    {
        try
        {
            using var client = clients.CreateClient("ScriptCommunity");
            var remoteBase = row.ContentType == "DeviceTemplate" ? DeviceProfilesBase : RemoteBase;
            if (row.RemoteId == Guid.Empty)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, remoteBase + "submissions")
                {
                    Content = new StringContent(sourceRequest ?? throw new InvalidOperationException("Explicit publication is required."), Encoding.UTF8, "application/json")
                };
                var receipt = await SendAsync<Receipt>(client, request, token);
                if (receipt.Id == Guid.Empty || string.IsNullOrWhiteSpace(receipt.TrackingToken) || receipt.TrackingToken.Length > 4096)
                {
                    throw new InvalidDataException("Invalid community submission receipt.");
                }
                row.RemoteId = receipt.Id;
                row.ProtectedToken = _protector.Protect(receipt.TrackingToken);
                row.Status = "Pending";
            }
            else
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, remoteBase + "submission-status")
                {
                    Content = JsonContent.Create(new { id = row.RemoteId, trackingToken = _protector.Unprotect(row.ProtectedToken!) })
                };
                var status = await SendAsync<RemoteStatus>(client, request, token);
                if (status.Id != row.RemoteId || status.Status is not ("Pending" or "Publishing" or "Approved" or "Rejected") || status.Version?.Length > 64)
                {
                    throw new InvalidDataException("Invalid community submission status.");
                }
                row.Status = status.Status;
                row.Version = status.Version;
                if (row.Status is "Approved" or "Rejected")
                {
                    row.ProtectedToken = null;
                }
            }
            row.Checked = DateTimeOffset.UtcNow;
            await store.SaveAsync(row, false, sourceRequest != null ? CancellationToken.None : token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            // Remote bodies and bearer receipts may contain secrets; never log them.
            logger.LogWarning("Community submission {Id} refresh failed ({ErrorType}).", row.Id, error.GetType().Name);
            if (row.RemoteId == Guid.Empty || error is CryptographicException ||
                error is HttpRequestException { StatusCode: System.Net.HttpStatusCode.NotFound })
            {
                row.Status = "Failed";
                if (error is CommunityProfileAlreadyExistsException)
                {
                    row.FailureMessage = "An identical device template already exists in the community repository.";
                }
                if (row.RemoteId != Guid.Empty)
                {
                    row.FailureMessage = error is CryptographicException
                        ? "The saved community receipt cannot be decrypted. Restore the server data protection keys or contact the community administrator before submitting again."
                        : "The community review receipt is unavailable or has expired. Check with the community administrator before submitting again.";
                }
                row.ProtectedToken = null;
                await store.SaveAsync(row, false, token);
            }
        }
    }

    /// <summary>Sends a bounded community API request with a timeout and deserializes its receipt or status response.</summary>
    /// <typeparam name="T">The callback result or deserialized response type.</typeparam>
    /// <param name="client">The configured community API client used to send the request.</param>
    /// <param name="request">The community API request to send with bounded response buffering.</param>
    /// <param name="token">Cancels the database or remote operation.</param>
    /// <returns>A task containing the deserialized non-null community API response.</returns>
    private static async Task<T> SendAsync<T>(HttpClient client, HttpRequestMessage request, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        token = timeout.Token;
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict &&
            request.RequestUri?.AbsoluteUri.StartsWith(DeviceProfilesBase, StringComparison.Ordinal) == true)
        {
            await response.Content.LoadIntoBufferAsync(16 * 1024, token);
            try
            {
                using var failure = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(token), cancellationToken: token);
                if (failure.RootElement.ValueKind == JsonValueKind.Object && failure.RootElement.TryGetProperty("code", out var code) &&
                    code.ValueKind == JsonValueKind.String && code.GetString() == "profile_already_exists")
                {
                    throw new CommunityProfileAlreadyExistsException();
                }
            }
            catch (JsonException)
            {
                // Unknown remote errors retain the generic receipt failure; their text is never exposed.
            }
        }
        response.EnsureSuccessStatusCode();
        await response.Content.LoadIntoBufferAsync(16 * 1024, token);
        return await response.Content.ReadFromJsonAsync<T>(_json, token) ?? throw new InvalidDataException("Empty community response.");
    }
    /// <summary>Signals that a community profile submission conflicts with an existing published profile.</summary>
    private sealed class CommunityProfileAlreadyExistsException : Exception;

}
