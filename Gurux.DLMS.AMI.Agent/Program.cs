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

using Gurux.DLMS.AMI.Agent.Shared;
using Gurux.DLMS.AMI.Agent.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Agent;

internal static class Program
{
    private static string settingsFile = "settings.json";
    private static ILogger? _logger;
    private static IGXAgentWorker? worker;
    private static ServiceProvider? workerServices;
    private static bool workerInitialized;
    private static readonly SemaphoreSlim consoleInput = new(1, 1);

    internal static async Task<GXAgentOptions> ReadReplacementTokenAsync(GXAgentOptions options, bool interactive,
        Func<CancellationToken, Task<string>> readSecret, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!interactive)
        {
            throw new InvalidOperationException("AMI authentication failed (HTTP 401). The saved PAT has been retained. Start the agent interactively under the same account to replace its PAT. Use Configure only to register a new agent.");
        }
        Console.WriteLine("AMI authentication failed (HTTP 401). The saved PAT will be retained until a replacement connects successfully.");
        Console.WriteLine("Enter a new Personal Access Token (Enter to exit):");
        string value = await readSecret(token);
        token.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new OperationCanceledException("PAT replacement cancelled; the saved PAT has been retained.");
        }
        var replacement = GXAgentOptionsSnapshot.Capture(options).ToOptions();
        replacement.Token = value;
        replacement.Validate();
        return replacement;
    }

    private static async Task<ServiceProvider> InitializeWorkerAsync(IGXAgentWorker target,
        IServiceCollection services, GXAgentOptions options, IGXAgentHostNotifications notifications)
    {
        ServiceProvider? provider = null;
        try
        {
            target.ConfigureServices(services);
            provider = services.BuildServiceProvider();
            target.Init(provider, options, notifications);
            return provider;
        }
        catch
        {
            await target.DisposeAsync();
            if (provider != null)
                {
                await provider.DisposeAsync();
            }
            throw;
        }
    }

    public static async Task RegisterAgent(IServiceCollection services, IGXAgentHostNotifications notifications,
        IGXAgentWorker worker, GXAgentOptions options, CancellationToken cancellationToken = default)
    {
        ServiceProvider? provider = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            string name = System.Net.Dns.GetHostName();
            Console.WriteLine("Welcome to use Gurux.DLMS.AMI.");
            Console.WriteLine($"Gurux.DLMS.AMI address: [{options.Address}]");
            string? value = await ReadLineAsync(cancellationToken);
            if (!string.IsNullOrEmpty(value))
                {
                options.Address = value;
            }
            Console.WriteLine($"Enter agent name: [{name}]");
            value = await ReadLineAsync(cancellationToken);
            if (!string.IsNullOrEmpty(value))
                {
                name = value;
            }
            Console.WriteLine("Enter Personal Access Token:");
            options.Token = await ReadSecretAsync(cancellationToken);
            options.Validate(requireAgentId: false);
            provider = await InitializeWorkerAsync(worker, services, options, notifications);
            var id = await worker.AddAgentAsync(name, cancellationToken);
            if (id == Guid.Empty)
                {
                throw new ArgumentException("Invalid agent Id.");
            }
            options = worker.Configuration.ToOptions();
            options.Id = id;
            await SaveSettingsAsync(options, cancellationToken);
            Console.WriteLine("Connection succeeded.");
        }
        finally
        {
            using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await worker.StopAsync(shutdown.Token);
            await worker.DisposeAsync().AsTask().WaitAsync(shutdown.Token);
            if (provider != null)
                {
                await provider.DisposeAsync().AsTask().WaitAsync(shutdown.Token);
            }
        }
    }

    private static Task<string?> ReadLineAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return Task.Run(Console.ReadLine).WaitAsync(token);
    }

    private static async Task<string> ReadSecretAsync(CancellationToken token)
    {
        if (Console.IsInputRedirected)
            {
            return await ReadLineAsync(token) ?? "";
        }
        var secret = new System.Text.StringBuilder();
        while (true)
        {
            token.ThrowIfCancellationRequested();
            if (!Console.KeyAvailable)
            { await Task.Delay(50, token); continue; }
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            { Console.WriteLine(); return secret.ToString(); }
            if (key.Key == ConsoleKey.Backspace)
            { if (secret.Length != 0) {
                --secret.Length;
            } }
            else if (!char.IsControl(key.KeyChar))
                {
                secret.Append(key.KeyChar);
            }
        }
    }

    private static async Task<int> Main(string[] args)
    {
        try
        { return await RunAsync(args); }
        catch (Exception error) { Console.Error.WriteLine($"Agent failed: {error.Message}"); return 1; }
    }

    private static async Task<int> RunAsync(string[] args)
    {
        using var cancellation = new CancellationTokenSource();
        using var notifications = new GXAgentHostNotifications();
        GXAgentInstanceLock? settingsLock = null, identityLock = null;
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; cancellation.Cancel(); };
        Console.CancelKeyPress += cancel;
        using var sigterm = OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()
            ? PosixSignalRegistration.Create(PosixSignal.SIGTERM, context => { context.Cancel = true; cancellation.Cancel(); }) : null;
        try
        {
            var config = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.default.json", optional: true, reloadOnChange: false)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false).AddEnvironmentVariables().Build();
            using var configLifetime = config as IDisposable;
            using var logging = LoggerFactory.Create(builder => builder.AddConfiguration(config.GetSection("Logging")).AddConsole());
            _logger = logging.CreateLogger("Gurux.DLMS.AMI.Agent");
            string? version = AgentRuntimeVersion.Resolve(AppContext.BaseDirectory, null, FileVersionInfo.GetVersionInfo(typeof(Settings).Assembly.Location).ProductVersion);
            _logger.LogInformation("Starting Gurux.DLMS.AMI.Agent {Version}", version);
            var commandLine = new Settings();
            if (Settings.GetParameters(args.Where(a => a != "Configure").ToArray(), commandLine) != 0)
                {
                return 1;
            }
            var updaterOptions = GXAgentUpdaterOptions.Read(config);
            settingsFile = updaterOptions.SettingsPath;
            settingsLock = GXAgentInstanceLock.Acquire(settingsFile);
            GXAgentSettingsStore.ProtectExisting(settingsFile);
            var completionOptions = GXCompletionOutboxOptions.Read(config);
            IServiceCollection Services()
            {
                var services = new ServiceCollection();
                services.AddSingleton(updaterOptions);
                services.AddSingleton(completionOptions);
                services.AddLogging(builder => builder.AddConfiguration(config.GetSection("Logging")).AddConsole());
                return services;
            }
            string? settingsJson = File.Exists(settingsFile)
                ? await File.ReadAllTextAsync(settingsFile, cancellation.Token)
                : null;
            if (string.IsNullOrWhiteSpace(settingsJson) || args.Contains("Configure"))
            {
                await RegisterAgent(Services(), notifications, new GXAgentWorker(),
                    new GXAgentOptions { Address = commandLine.Host ?? "https://localhost:8001", Version = version }, cancellation.Token);
                settingsJson = await File.ReadAllTextAsync(settingsFile, cancellation.Token);
            }
            var options = JsonSerializer.Deserialize<GXAgentOptions>(settingsJson!)
                ?? throw new InvalidOperationException("Agent settings are empty.");
            if (!string.IsNullOrEmpty(commandLine.Host))
                {
                options.Address = commandLine.Host;
            }
            // A saved or legacy bin<version> selection can never override the executable actually running.
            options.Version = version;
            options.Validate();
            identityLock = GXAgentInstanceLock.AcquireIdentity(options.Address, options.Id);
            async Task ReplaceTokenAsync()
            {
                await consoleInput.WaitAsync(cancellation.Token);
                try
                {
                    options = await ReadReplacementTokenAsync(options, Environment.UserInteractive && !Console.IsInputRedirected,
                        ReadSecretAsync, cancellation.Token);
                }
                finally
                {
                    consoleInput.Release();
                }
            }
            async Task StartRuntimeAsync()
            {
                int failedAttempts = 0;
                while (true)
                {
                    cancellation.Token.ThrowIfCancellationRequested();
                    worker = new GXAgentWorker();
                    workerInitialized = false;
                    try
                    {
                        workerServices = await InitializeWorkerAsync(worker, Services(), options, notifications);
                        workerInitialized = true;
                        await worker.StartAsync(cancellation.Token);
                        if (worker is GXAgentWorker active && active.AuthenticationRejected)
                        {
                            throw new HttpRequestException("AMI authentication failed (HTTP 401).", null, System.Net.HttpStatusCode.Unauthorized);
                        }
                        options = worker.Configuration.ToOptions();
                        await SaveSettingsAsync(options, cancellation.Token);
                        break;
                    }
                    catch (GXMaintenanceException error)
                    {
                        await DisconnectWithDeadlineAsync();
                        _logger?.LogWarning("Server maintenance: {Message}", error.Message);
                        await GXMaintenanceRetry.WaitAsync(error, cancellation.Token);
                    }
                    catch (HttpRequestException error) when (error.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        await DisconnectAfterDrainAsync();
                        await ReplaceTokenAsync();
                    }
                    catch (HttpRequestException error)
                    {
                        await DisconnectWithDeadlineAsync();
                        _logger?.LogWarning(error, "Server unavailable; retrying agent startup");
                        var delay = GXRetryBackoff.GetDelay(++failedAttempts, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(5));
                        _logger?.LogInformation("Retrying startup in {DelaySeconds} seconds", delay.TotalSeconds);
                        await Task.Delay(delay, cancellation.Token);
                    }
                }
            }
            await StartRuntimeAsync();
            if (Environment.UserInteractive && !Console.IsInputRedirected)
            {
                _ = Task.Run(() => ClosePoller(cancellation));
            }
            while (true)
            {
                var change = await notifications.WaitForChangeAsync(cancellation.Token);
                if (change.HasFlag(GXAgentHostChange.AuthenticationRequired) && worker is GXAgentWorker active && active.AuthenticationRejected)
                {
                    options = worker.Configuration.ToOptions();
                    await DisconnectAfterDrainAsync();
                    await ReplaceTokenAsync();
                    await StartRuntimeAsync();
                    continue;
                }
                if (workerInitialized)
                    {
                    options = worker!.Configuration.ToOptions();
                }
                await SaveSettingsAsync(options, cancellation.Token);
                if (!change.HasFlag(GXAgentHostChange.VersionAvailable))
                    {
                    continue;
                }
                var updater = workerServices!.GetRequiredService<IGXAgentUpdateService>();
                if (updater.PendingUpdate == null)
                    {
                    continue;
                }
                // All tasks, completion reporting, listeners and scripts finish before handing over installation.
                bool handedOff = await TryHandoffUpdateAsync(updater, Environment.ProcessId, async () =>
                {
                    updaterOptions.RetryAfter = DateTimeOffset.UtcNow.AddMinutes(5);
                    await StartRuntimeAsync();
                }, cancellation.Token);
                if (!handedOff)
                    {
                    continue;
                }
                _logger.LogInformation("Application update handed to Gurux.Updater; exiting current agent");
                return 0;
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { return 0; }
        catch (OperationCanceledException error)
        {
            _logger?.LogInformation("{Message}", error.Message);
            return 1;
        }
        catch (GXAgentOptionsValidationException error)
        {
            foreach (var item in error.Errors)
                {
                _logger?.LogError("Invalid configuration: {Code} at {Path}: {Message}", item.Code, item.PropertyPath, item.Message);
            }
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= cancel;
            try
            { await DisconnectAfterDrainAsync(); }
            finally { identityLock?.Dispose(); settingsLock?.Dispose(); }
        }
    }

    internal static async Task HandoffUpdateAsync(IGXAgentUpdateService updater, int processId, CancellationToken token)
    {
        await DisconnectAfterDrainAsync();
        await updater.LaunchAsync(processId, token);
    }
    internal static async Task<bool> TryHandoffUpdateAsync(IGXAgentUpdateService updater, int processId,
        Func<Task> recover, CancellationToken token)
    {
        // A failed shutdown must never create a second runtime alongside an incompletely stopped one.
        await DisconnectAfterDrainAsync();
        token.ThrowIfCancellationRequested();
        try
        {
            await updater.LaunchAsync(processId, token);
            return true;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception error)
        {
            _logger?.LogError(error, "Updater could not start; restoring the installed runtime with a five-minute update cooldown");
            await recover();
            return false;
        }
    }
    private static async Task SaveSettingsAsync(GXAgentOptions options, CancellationToken token)
    {
        await GXAgentSettingsStore.SaveAsync(settingsFile, JsonSerializer.Serialize(options), token);
    }

    private static async Task DisconnectWithDeadlineAsync()
    {
        if (worker == null)
            {
            return;
        }
        using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await worker.StopAsync(shutdown.Token);
        await worker.DisposeAsync().AsTask().WaitAsync(shutdown.Token);
        if (workerServices != null)
            {
            await workerServices.DisposeAsync().AsTask().WaitAsync(shutdown.Token);
        }
        workerServices = null;
        worker = null;
        workerInitialized = false;
    }

    private static async Task DisconnectAfterDrainAsync()
    {
        while (true)
        {
            try { await DisconnectWithDeadlineAsync(); return; }
            catch (OperationCanceledException)
            {
                _logger?.LogWarning("Shutdown is still draining; keeping the process and instance locks alive until cleanup and result persistence finish");
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }

    private static void ClosePoller(CancellationTokenSource cancellation)
    {
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                consoleInput.Wait(cancellation.Token);
                try
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true).Key;
                        if (key == ConsoleKey.Escape)
                        {
                            cancellation.Cancel();
                        }
                        else if (key == ConsoleKey.Delete)
                        {
                            Console.Clear();
                        }
                    }
                }
                finally
                {
                    consoleInput.Release();
                }
                Thread.Sleep(100);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (InvalidOperationException) { /* Console input can be detached while shutting down. */ }
    }
}
