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
using System.Diagnostics;
using System.Text.Json;
using Gurux.DLMS.AMI.Agent;
using Gurux.DLMS.AMI.Agent.Shared;
using Gurux.DLMS.AMI.Agent.Worker;

internal static class AuthenticationChecks
{
    internal static async Task RunAsync()
    {
        int signals = 0;
        var state = new GXAuthenticationState(() => ++signals);
        var transport = new ResponseHandler(HttpStatusCode.Unauthorized);
        using var client = new HttpClient(new GXAuthenticationHandler(transport, state));
        using var other = new HttpClient(new GXAuthenticationHandler(new ResponseHandler(HttpStatusCode.OK), state));
        for (int attempt = 0; attempt != 2; ++attempt)
        {
            await RequireUnauthorizedAsync(client);
        }
        await RequireUnauthorizedAsync(other);
        Require(signals == 1 && transport.Calls == 1, "Rejected PAT was sent again or signaled repeatedly.");
        using var forbidden = new HttpClient(new GXAuthenticationHandler(new ResponseHandler(HttpStatusCode.Forbidden), new GXAuthenticationState(() => ++signals)));
        using var response = await forbidden.GetAsync("https://ami.test/api/Script");
        Require(response.StatusCode == HttpStatusCode.Forbidden && signals == 1, "403 triggered PAT recovery.");

        var options = new GXAgentOptions { Address = "https://ami.test", Id = Guid.NewGuid(), Token = "old-token" };
        var replacement = await Gurux.DLMS.AMI.Agent.Program.ReadReplacementTokenAsync(options, true, _ => Task.FromResult("new-token"), CancellationToken.None);
        Require(options.Token == "old-token" && replacement.Token == "new-token" && replacement.Id == options.Id, "Replacement mutated saved options or changed identity.");
        int prompts = 0;
        try
        {
            await Gurux.DLMS.AMI.Agent.Program.ReadReplacementTokenAsync(options, false, _ => { ++prompts; return Task.FromResult("new-token"); }, CancellationToken.None);
            throw new Exception("Unattended execution prompted for PAT.");
        }
        catch (InvalidOperationException error) when (error.Message.Contains("Configure"))
        {
        }
        Require(prompts == 0 && options.Token == "old-token", "Unattended recovery read input or changed PAT.");
        try
        {
            await Gurux.DLMS.AMI.Agent.Program.ReadReplacementTokenAsync(options, true, _ => Task.FromResult(""), CancellationToken.None);
            throw new Exception("Empty replacement was accepted.");
        }
        catch (OperationCanceledException)
        {
        }
        using var notifications = new GXAgentHostNotifications();
        notifications.NotifyAuthenticationRequired();
        notifications.NotifySettingsChanged();
        var changes = await notifications.WaitForChangeAsync();
        Require(changes.HasFlag(GXAgentHostChange.AuthenticationRequired) && changes.HasFlag(GXAgentHostChange.SettingsChanged), "Authentication notification was lost.");
        await CheckStaleCompletionAsync();
        await CheckInjectedWorkerAsync();
        await CheckUnattendedProcessAsync(HttpStatusCode.Unauthorized);
        await CheckUnattendedProcessAsync(HttpStatusCode.NotFound);
        await CheckUnattendedProcessAsync(HttpStatusCode.InternalServerError);
        Console.WriteLine("PASS PAT recovery, missing registration stops without changing settings, and transient server errors still retry.");
    }

    private static async Task CheckStaleCompletionAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "agent-stale-result-" + Guid.NewGuid().ToString("N"));
        int rejected = 0;
        var authentication = new GXAuthenticationState(() => ++rejected);
        using var http = new HttpClient(new GXAuthenticationHandler(new StaleCompletionHandler(), authentication))
        {
            BaseAddress = new Uri("https://ami.test")
        };
        try
        {
            await using var outbox = new GXTaskCompletionOutbox(http, root, null, TimeSpan.FromHours(1),
                new GXCompletionOutboxOptions { MinFreeBytes = 0 });
            await outbox.ReportAsync([new Gurux.DLMS.AMI.Shared.DTOs.GXTask { Id = Guid.NewGuid(), Start = DateTimeOffset.UtcNow }]);
            using var response = await http.GetAsync("/api/Agent");
            Require(response.IsSuccessStatusCode && !authentication.IsRejected && rejected == 0,
                "Stale completion rejected a valid PAT and blocked subsequent API calls.");
            Require(outbox.Status.PendingCount == 0 && outbox.Status.QuarantinedCount == 1,
                "Stale completion was lost or remained in automatic retry instead of quarantine.");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private sealed class StaleCompletionHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            => Task.FromResult(new HttpResponseMessage(request.RequestUri!.AbsolutePath == "/api/task/Done"
                ? HttpStatusCode.BadRequest : HttpStatusCode.OK));
    }

    private static async Task CheckInjectedWorkerAsync()
    {
        var transport = new ResponseHandler(HttpStatusCode.Unauthorized);
        using var supplied = new HttpClient(transport);
        using var notifications = new GXAgentHostNotifications();
        var worker = new GXAgentWorker(supplied);
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        Microsoft.Extensions.DependencyInjection.LoggingServiceCollectionExtensions.AddLogging(services);
        worker.ConfigureServices(services);
        await using var provider = Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services);
        worker.Init(provider, new GXAgentOptions { Address = "https://ami.test", Token = "test-token" }, notifications);
        try
        {
            await RequireUnauthorizedAsync(worker.client);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var change = await notifications.WaitForChangeAsync(timeout.Token);
            Require(change.HasFlag(GXAgentHostChange.AuthenticationRequired) && worker.AuthenticationRejected, "Injected worker did not notify runtime authentication failure.");
            await RequireUnauthorizedAsync(worker.client);
            Require(transport.Calls == 1, "Injected worker retried rejected credentials.");
        }
        finally
        {
            await worker.DisposeAsync();
        }
        using var response = await supplied.GetAsync("https://ami.test/owner-probe");
        Require(response.StatusCode == HttpStatusCode.Unauthorized, "Worker disposed the caller-owned client.");
    }

    private static async Task CheckUnattendedProcessAsync(HttpStatusCode status)
    {
        using var listener = new HttpListener();
        var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        string address = $"http://localhost:{port}/";
        listener.Prefixes.Add(address);
        listener.Start();
        string root = Path.Combine(Path.GetTempPath(), "agent-auth-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string settings = Path.Combine(root, "settings.json");
        string original = JsonSerializer.Serialize(new GXAgentOptions { Address = address, Id = Guid.NewGuid(), Token = "retained-pat" });
        await File.WriteAllTextAsync(settings, original);
        try
        {
            var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(typeof(Settings).Assembly.Location);
            start.Environment["GURUX_AGENT_SETTINGS"] = settings;
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            try
            {
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var context = await listener.GetContextAsync().WaitAsync(deadline.Token);
                context.Response.StatusCode = (int)status;
                context.Response.Close();
                if (status == HttpStatusCode.InternalServerError)
                {
                    var retry = await listener.GetContextAsync().WaitAsync(deadline.Token);
                    retry.Response.StatusCode = 500;
                    retry.Response.Close();
                    Require(!process.HasExited, "A transient server error terminated the agent.");
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync(deadline.Token);
                    string retryText = await output + await error;
                    Require(retryText.Contains("retrying agent startup", StringComparison.OrdinalIgnoreCase), "A transient server error did not retry.");
                    Require(await File.ReadAllTextAsync(settings) == original, "Retrying a server error modified registration.");
                    return;
                }
                await process.WaitForExitAsync(deadline.Token);
                string text = await output + await error;
                Require(process.ExitCode != 0 && text.Contains("Configure") && !text.Contains("retained-pat"), $"Unattended {status} did not exit with safe actionable diagnostics.");
                Require(!text.Contains("retrying agent startup", StringComparison.OrdinalIgnoreCase), $"Unattended {status} retried invalid registration.");
                Require(await File.ReadAllTextAsync(settings) == original, $"{status} modified saved registration.");
            }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync();
                }
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static async Task RequireUnauthorizedAsync(HttpClient client)
    {
        try
        {
            await client.GetAsync("https://ami.test/api/Agent");
            throw new Exception("Unauthorized request succeeded.");
        }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.Unauthorized)
        {
        }
    }

    private static void Require(bool value, string message)
    {
        if (!value)
        {
            throw new Exception(message);
        }
    }

    private sealed class ResponseHandler(HttpStatusCode status) : HttpMessageHandler
    {
        internal int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            ++Calls;
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }
}
