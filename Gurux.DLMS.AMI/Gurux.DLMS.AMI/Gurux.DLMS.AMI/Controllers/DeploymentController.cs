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

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using Gurux.DLMS.AMI.Client.Rest;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Update;
using Gurux.Updater.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NuGet.Versioning;

namespace Gurux.DLMS.AMI.Server.Controllers;

/// <summary>Queues updates for the independent Linux deployment agent.</summary>
[ApiController]
[Route("api/Update/Deployment")]
[Authorize(Policy = GXUpdatePolicies.Admin)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class DeploymentController(IConfiguration configuration, CatalogUpdaterOptions catalogOptions, IHttpClientFactory clients) : ControllerBase
{
    static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    string? Root => configuration["Updater:Deployment:Root"];
    bool Compose => configuration["Updater:Deployment:Mode"] == "compose";
    bool IncludeBackend => Compose || configuration.GetValue<bool>("Updater:Deployment:IncludeBackend");
    string ProductId => configuration["Updater:Deployment:ProductId"] ?? (IncludeBackend ? "Gurux.DLMS.AMI" : "Gurux.DLMS.AMI.Wasm");
    bool Configured => OperatingSystem.IsLinux() && Root is { Length: > 0 } && Path.IsPathFullyQualified(Root);
    static string ApiVersion => FileVersionInfo.GetVersionInfo(typeof(DeploymentController).Assembly.Location).FileVersion ?? "0.0.0.0";
    bool AgentAvailable => System.IO.File.Exists(Path.Combine(Root!, "agent-heartbeat")) && DateTime.UtcNow - System.IO.File.GetLastWriteTimeUtc(Path.Combine(Root!, "agent-heartbeat")) < TimeSpan.FromSeconds(30);

    /// <summary>Checks deployable versions and includes the installation's current state.</summary>
    [HttpGet]
    public async Task<ActionResult<GXDeploymentResponse>> Get(CancellationToken token)
    {
        var response = await SnapshotAsync(token);
        if (!response.Configured) {
            return response;
        }
        try
        {
            var releases = await ReleasesAsync(token);
            response.Releases = releases.OrderByDescending(r => NuGetVersion.Parse(r.Version))
                .Select(r => new GXDeploymentRelease { Version = r.Version, MaintenanceRequired = r.Blocked }).ToArray();
            var latest = response.Releases.FirstOrDefault();
            response.AvailableVersion = latest?.Version;
            response.MaintenanceRequired = latest?.MaintenanceRequired == true;
            response.Message = latest?.MaintenanceRequired == true ? "This release requires maintenance or an audited rollback compatibility range." : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidDataException or JsonException)
        { response.Error ??= "Update catalog is unavailable."; }
        return response;
    }
    /// <summary>Reads durable state without contacting the update catalog.</summary>
    [HttpGet("Status")]
    public async Task<GXDeploymentResponse> Status(CancellationToken token, [FromQuery] string? requestId = null)
    {
        if (requestId is not null && !Guid.TryParseExact(requestId, "N", out _)) {
            throw new BadHttpRequestException("Invalid request ID.");
        }
        return await SnapshotAsync(token, requestId);
    }

    /// <summary>Queues a catalog version for the independent deployment agent.</summary>
    [HttpPost]
    public Task<IActionResult> Start([FromBody] GXDeploymentVersion request, CancellationToken token) => QueueAsync(request, false, token);
    /// <summary>Restores only the retained predecessor of the latest successful installation.</summary>
    [HttpPost("Restore")]
    public Task<IActionResult> Restore([FromBody] GXDeploymentVersion request, CancellationToken token) => QueueAsync(request, true, token);
    async Task<IActionResult> QueueAsync(GXDeploymentVersion request, bool restorePrevious, CancellationToken token)
    {
        if (!Configured || !AgentAvailable) {
            return Conflict(new { Message = "The deployment agent is not configured or running." });
        }
        // Resolve and check on BOTH sides of the process boundary; never trust browser-supplied asset metadata.
        if (restorePrevious)
        {
            var snapshot = await SnapshotAsync(token);
            if (snapshot.RestoreVersion is null || snapshot.RestoreVersion != request.Version)
                {
                return BadRequest(new { Message = "Only the retained previous successful installation can be restored." });
            }
        }
        else
        {
            var release = (await ReleasesAsync(token)).SingleOrDefault(r => r.Version == request.Version);
            if (release is null || release.Blocked) {
                return BadRequest(new { Message = "This version is unavailable or incompatible with the running API." });
            }
        }
        string requestId = Guid.NewGuid().ToString("N");
        string? signature = null;
        if (Compose)
        {
            var keyFile = configuration["Updater:Deployment:RequestKeyFile"];
            if (string.IsNullOrWhiteSpace(keyFile)) {
                return Conflict(new { Message = "The deployment request key is not configured." });
            }
            try
            {
                string secret = (await System.IO.File.ReadAllTextAsync(keyFile, token)).Trim();
                if (secret.Length == 0) {
                    return Conflict(new { Message = "The deployment request key is not configured." });
                }
                signature = GXDeploymentAuthentication.Sign(request.Version, requestId, secret, restorePrevious).Signature;
            }
            catch (IOException) { return Conflict(new { Message = "The deployment request key is unavailable." }); }
            catch (UnauthorizedAccessException) { return Conflict(new { Message = "The deployment request key is unavailable." }); }
        }
        string queue = Path.Combine(Root!, "queue");
        string temporary = Path.Combine(queue, ".request-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var gate = new FileStream(Path.Combine(queue, "queue.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            if (System.IO.File.Exists(Path.Combine(queue, "request.json"))) {
                return Conflict(new { Message = "An update is already queued or running." });
            }
            await using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { await JsonSerializer.SerializeAsync(file, new Gurux.Updater.Model.GXDeploymentRequest(request.Version, requestId, signature, restorePrevious), Json, token); file.Flush(true); }
            System.IO.File.Move(temporary, Path.Combine(queue, "request.json"));
        }
        catch (IOException) { return Conflict(new { Message = "The update queue is busy or unavailable." }); }
        finally { System.IO.File.Delete(temporary); }
        if (Compose)
        {
            // The root service waits for this marker, so the accepted ID reaches the browser first.
            Response.OnCompleted(async () =>
            {
                string readyTemporary = Path.Combine(queue, ".ready-" + requestId);
                try
                {
                    await using (var file = new FileStream(readyTemporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    { file.Flush(true); }
                    System.IO.File.Move(readyTemporary, Path.Combine(queue, requestId + ".ready"));
                }
                finally { System.IO.File.Delete(readyTemporary); }
            });
        }
        return Accepted(new GXDeploymentResponse { Configured = true, IncludesBackend = IncludeBackend, Mode = Compose ? "compose" : "static", AgentAvailable = true, Busy = true, State = "Queued", RequestId = requestId, Version = request.Version, ApiVersion = ApiVersion });
    }
    sealed record Release(string Version, bool Blocked = false);
    async Task<List<Release>> ReleasesAsync(CancellationToken token)
    {
        using var client = clients.CreateClient();
        string channel = configuration["Updater:Deployment:Channel"] ?? "stable";
        if (Compose)
        {
            using var stream = await client.GetStreamAsync(catalogOptions.CatalogUrl, token);
            var document = await JsonNode.ParseAsync(stream, cancellationToken: token);
            var releases = document?["items"]?.AsArray().SingleOrDefault(i => i?["id"]?.GetValue<string>() == ProductId)?["releases"]?.AsArray();
            return releases?.Where(r => r is not null && NuGetVersion.TryParse(r["version"]?.GetValue<string>(), out var version) &&
                (channel == "preview" || !version.IsPrerelease && r["isPrerelease"]?.GetValue<bool>() != true) &&
                r["isRevoked"]?.GetValue<bool>() != true && r["isUnsupported"]?.GetValue<bool>() != true &&
                r["compose"] is JsonObject c && c["channel"]?.GetValue<string>() == channel &&
                c["platforms"] is JsonArray platforms && platforms.Any(p => p?.GetValue<string>() == (configuration["Updater:Deployment:Platform"] ?? "linux/amd64")))
                .Select(r => new Release(r!["version"]!.GetValue<string>(), r["compose"]!["maintenanceRequired"]?.GetValue<bool>() != false ||
                    !VersionRange.TryParse(r["compose"]!["rollbackCompatibleFrom"]?.GetValue<string>() ?? "", out var range) || !range.Satisfies(NuGetVersion.Parse(ApiVersion)))).ToList() ?? [];
        }
        var catalog = await new GXCatalogUpdateService(client).GetCatalogAsync(new Uri(catalogOptions.CatalogUrl), token);
        var item = catalog.Items.SingleOrDefault(i => i.Id == ProductId);
        return item?.Releases.Where(r => !r.IsPrerelease && !r.IsRevoked && !r.IsUnsupported && NuGetVersion.TryParse(r.Version, out _) &&
            r.Assets.Count(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) == 1 &&
            r.Assets.Count(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) && ValidDigest(a.Digest)) == 1 &&
            (IncludeBackend || r.AmiVersionRange is not null && VersionRange.TryParse(r.AmiVersionRange, out var range) && range.Satisfies(NuGetVersion.Parse(ApiVersion)))).Select(r => new Release(r.Version)).ToList() ?? [];
    }
    static bool ValidDigest(string? value)
    {
        if (value?.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) == true) {
            value = value[7..];
        }
        return value is { Length: 64 } && value.All(Uri.IsHexDigit);
    }
    async Task<GXDeploymentResponse> SnapshotAsync(CancellationToken token, string? requestId = null)
    {
        var result = new GXDeploymentResponse { Configured = Configured, IncludesBackend = IncludeBackend, ApiVersion = ApiVersion, Mode = Compose ? "compose" : "static" };
        if (!Configured) {
            return result;
        }
        result.AgentAvailable = AgentAvailable;
        result.Busy = System.IO.File.Exists(Path.Combine(Root!, "queue", "request.json"));
        string web = IncludeBackend ? Path.Combine(Root!, "current", "wwwroot") : Path.Combine(Root!, "current");
        var marker = await ReadAsync(Path.Combine(web, "version.json"), token);
        result.CurrentVersion = marker? ["version"]?.GetValue<string>();
        var pending = await ReadAsync(Path.Combine(Root!, "queue", "request.json"), token);
        if (requestId is not null && requestId != pending?["requestId"]?.GetValue<string>()) {
            result.Busy = false;
        }
        requestId ??= pending?["requestId"]?.GetValue<string>();
        var status = await ReadAsync(Path.Combine(Root!, "status.json"), token);
        if (requestId is not null && status?["requestId"]?.GetValue<string>() != requestId)
            {
            status = await ReadAsync(Path.Combine(Root!, "history", requestId + ".json"), token);
        }
        result.RequestId = requestId ?? status?["requestId"]?.GetValue<string>();
        result.Version = status?["version"]?.GetValue<string>() ?? pending?["version"]?.GetValue<string>();
        if (Compose) {
            result.CurrentVersion = ApiVersion;
        }
        if (status is not null)
        {
            result.State = status["state"]?.GetValue<string>() ?? "Idle";
            result.Error = status["error"]?.GetValue<string>();
            result.UpdatedAt = status["updatedAt"]?.GetValue<DateTimeOffset>();
        }
        if (result.Busy && status is null) { result.State = "Queued"; result.Error = null; }
        if (!result.Busy)
        {
            // The backend can read the public journal but not the updater's private snapshots.
            // The independent updater validates those snapshots before activating a restore.
            var previous = await GXDeploymentRestoration.ReadEligiblePreviousAsync(Root!, result.CurrentVersion, Compose, token);
            if (previous is not null) {
                result.RestoreVersion = GXDeploymentRestoration.PreviousVersion(previous);
            }
        }
        return result;
    }
    static async Task<System.Text.Json.Nodes.JsonNode?> ReadAsync(string path, CancellationToken token)
    {
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return await System.Text.Json.Nodes.JsonNode.ParseAsync(stream, cancellationToken: token);
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }
}
