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

using System.Net.Http.Headers;
using System.Text.Json;
using Gurux.Updater.Model;
using Gurux.Updater.Services;

namespace Gurux.DLMS.AMI.ContainerUpdater;

/// <summary>Independent container deployment daemon using Gurux.Updater.Net queue and recovery services.</summary>
internal static class GXDeploymentCommand
{
    internal static async Task RunAsync(string[] args, CancellationToken token)
    {
        if (args.Length != 2 || args[0] != "--config")
        {
            throw new ArgumentException("Usage: Gurux.Updater.Tool deployment-agent --config /etc/gurux-ami/updater.json");
        }
        var options = JsonSerializer.Deserialize<GXDeploymentOptions>(await File.ReadAllTextAsync(args[1], token), new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidDataException("Empty deployment configuration.");
        bool compose = options.Mode == "compose";
        if (!compose && !OperatingSystem.IsLinux())
        {
            throw new PlatformNotSupportedException("Static deployment requires Linux.");
        }
        if (compose)
        {
            GXComposeDeploymentService.ValidateOptions(options);
        }
        else if (options.Mode == "static")
        {
            GXDeploymentService.ValidateOptions(options);
        }
        else
        {
            throw new ArgumentException("Unknown installation method.");
        }
        string? requestKey = null;
        if (compose)
        {
            if (options.RequestReadyTimeoutSeconds < 1)
            {
                throw new ArgumentException("A positive response-ready timeout is required.");
            }
            requestKey = string.IsNullOrWhiteSpace(options.RequestKeyEnvironmentVariable) ? null : Environment.GetEnvironmentVariable(options.RequestKeyEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(requestKey))
            {
                throw new InvalidOperationException("Compose requests require a host-only HMAC key environment variable.");
            }
        }
        Directory.CreateDirectory(options.Root);
        Directory.CreateDirectory(Path.Combine(options.Root, "queue"));
        using var daemonLock = new FileStream(Path.Combine(options.Root, "agent.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        // No default credentials: authentication applies only to explicitly validated GitHub download requests.
        using var client = Bootstrap.CreateHttpClient(options);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Gurux.Updater/1.0");
        var service = new GXDeploymentService(client);
        GXComposeDeploymentService? composeService = compose ? new(client, new GXProjectRunner(options)) : null;
        var queue = new GXDeploymentQueue(options.Root);
        if (compose)
        {
            await composeService!.RecoverAsync(options);
        }
        else
        {
            await service.RecoverAsync(options);
        }
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
        var heartbeat = HeartbeatAsync(options.Root, lifetime.Token);
        try
        {
            while (!token.IsCancellationRequested)
            {
                GXDeploymentRequest? request = await queue.ReadRequestAsync(token);
                if (request is not null)
                {
                    bool installationStarted = false;
                    try
                    {
                        if (compose)
                        {
                            GXDeploymentAuthentication.Verify(request, requestKey!);
                            // The backend publishes this only after the Accepted response has completed.
                            if (!await IsResponseReadyAsync(queue, request, options.RequestReadyTimeoutSeconds))
                            { await Task.Delay(1000, token); continue; }
                        }
                        if (!options.IncludeBackend && options.ApiVersionUrl is not null)
                        {
                            options.ApiVersion = (await client.GetStringAsync(options.ApiVersionUrl, token)).Trim().Trim('"');
                        }
                        if (request.RestorePrevious)
                        {
                            // Saved local installation identity, never a catalog downgrade, authorizes restoration.
                            if (compose)
                            {
                                await composeService!.RestorePreviousAsync(options, request, token);
                            }
                            else
                            {
                                await service.RestorePreviousAsync(options, request, token);
                            }
                        }
                        else
                        {
                            var catalog = await new GXCatalogUpdateService(client).GetCatalogAsync(new Uri(options.CatalogUrl), token);
                            var release = compose ? GXComposeDeploymentService.SelectRelease(catalog, options, request.Version) : GXDeploymentService.SelectRelease(catalog, options, request.Version);
                            release = await AgentRelease.LoadDescriptorAsync(client, options, release, token);
                            if (compose)
                            {
                                await AgentRelease.WaitForHealthyContainerAsync(new GXProjectRunner(options), options, token);
                            }
                            installationStarted = true;
                            if (compose)
                            {
                                await composeService!.InstallAsync(options, release, request, token);
                            }
                            else
                            {
                                await service.InstallAsync(options, release, token, request.RequestId);
                            }
                        }
                        queue.CompleteRequest();
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException || !token.IsCancellationRequested)
                    {
                        // Never log configuration or private request headers.
                        Console.Error.WriteLine($"Deployment {request.Version} failed: {exception.Message}");
                        if (request.RestorePrevious && (await queue.ReadStatusAsync())?.RequestId == request.RequestId)
                        {
                            installationStarted = true;
                        }
                        await HandleFailureAsync(queue, request, installationStarted, exception);
                    }
                }
                await Bootstrap.SyncOverrideAsync(options.Root, "/project");
                await Task.Delay(1000, token);
            }
        }
        finally
        {
            lifetime.Cancel();
            try
            { await heartbeat; }
            catch (OperationCanceledException) { }
            File.Delete(Path.Combine(options.Root, "agent-heartbeat"));
        }
    }
    internal static async Task<bool> IsResponseReadyAsync(GXDeploymentQueue queue, GXDeploymentRequest request, int timeoutSeconds)
    {
        if (File.Exists(Path.Combine(queue.Root, "queue", request.RequestId + ".ready")))
        {
            return true;
        }
        if (DateTime.UtcNow - File.GetLastWriteTimeUtc(Path.Combine(queue.Root, "queue", "request.json")) >= TimeSpan.FromSeconds(timeoutSeconds))
        {
            await HandleFailureAsync(queue, request, false, new TimeoutException("AMI did not finish its Accepted response. The request expired without replacing any container; submit it again."));
        }
        return false;
    }
    internal static async Task HandleFailureAsync(GXDeploymentQueue queue, GXDeploymentRequest request, bool installationStarted, Exception exception)
    {
        if (installationStarted)
        {
            var status = await queue.ReadStatusAsync();
            // An installation that did not durably reach a safe terminal state must be recovered,
            // not overwritten by a generic failure that discards the previous-link journal.
            if (status?.RequestId != request.RequestId || status?.Version != request.Version || status.State is not ("Failed" or "RolledBack" or "Succeeded" or "MaintenanceRequired"))
            {
                throw new InvalidOperationException("Deployment outcome is uncertain; retaining journal and request for recovery.", exception);
            }
            await queue.WriteStatusAsync(status); // Repair terminal history before allowing the next request.
        }
        else
        {
            await queue.WriteStatusAsync(new() { RequestId = Guid.TryParseExact(request.RequestId, "N", out _) ? request.RequestId : null, Version = request.Version, State = "Failed", Error = exception.Message });
        }
        queue.CompleteRequest();
    }
    static async Task HeartbeatAsync(string root, CancellationToken token)
    {
        while (true)
        {
            await File.WriteAllTextAsync(Path.Combine(root, "agent-heartbeat"), DateTimeOffset.UtcNow.ToString("O"), token);
            await Task.Delay(5000, token);
        }
    }
}
