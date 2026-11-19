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

/// <summary>Runs the agent host and coordinates registration, persistence and update handoff.</summary>
internal static class Program
{
    /// <summary>Path used to load and persist the host configuration.</summary>
    private static string _settingsFile = "settings.json";
    /// <summary>Records runtime diagnostics without exposing authentication credentials.</summary>
    private static ILogger? _logger;
    /// <summary>Agent runtime controlled by this host or connection.</summary>
    private static IGXAgentWorker? _worker;
    /// <summary>Owns the dependency services created for the active worker.</summary>
    private static ServiceProvider? _workerServices;
    /// <summary>Indicates whether worker initialization completed successfully.</summary>
    private static bool _workerInitialized;
    /// <summary>Serializes interactive console prompts.</summary>
    private static readonly SemaphoreSlim _consoleInput = new(1, 1);

    /// <summary>Obtains a replacement access token and returns validated host options.</summary>
    /// <param name="options">Validated configuration used by the runtime operation.</param>
    /// <param name="interactive">Whether the host may prompt the operator for a replacement token.</param>
    /// <param name="readSecret">Reads a replacement token without exposing it in console output.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>Validated options containing the replacement access token.</returns>
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

    /// <summary>Builds worker dependencies and initializes the selected worker with validated options.</summary>
    /// <param name="target">Target object receiving the operation or protocol value.</param>
    /// <param name="services">Dependency registrations used to initialize the worker.</param>
    /// <param name="options">Validated configuration used by the runtime operation.</param>
    /// <param name="notifications">Reports host persistence, authentication and update requests.</param>
    /// <returns>The service provider owning the initialized worker dependencies.</returns>
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

    /// <summary>Registers or reconfigures the hosted agent worker using the current server settings.</summary>
    /// <param name="services">Dependency registrations used to initialize the worker.</param>
    /// <param name="notifications">Reports host persistence, authentication and update requests.</param>
    /// <param name="worker">Agent runtime being registered or reconfigured.</param>
    /// <param name="options">Validated configuration used by the runtime operation.</param>
    /// <param name="cancellationToken">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A task representing this operation: Registers or reconfigures the hosted agent worker using the current server settings.</returns>
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

    /// <summary>Reads console input asynchronously while allowing host cancellation.</summary>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>The line entered by the operator, or null when console input ends.</returns>
    private static Task<string?> ReadLineAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return Task.Run(Console.ReadLine).WaitAsync(token);
    }

    /// <summary>Reads an access token without echoing secret characters to the console.</summary>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>The access token entered without console echo.</returns>
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

    /// <summary>Retrieves the Windows standard console handle used to configure input behavior.</summary>
    /// <param name="standardHandle">Windows identifier selecting the standard input handle.</param>
    /// <returns>The native standard input console handle.</returns>
    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern IntPtr GetStdHandle(int standardHandle);

    /// <summary>Reads the current Windows console input mode.</summary>
    /// <param name="consoleHandle">Windows console handle to inspect or configure.</param>
    /// <param name="mode">Console mode flags read or applied by the operation.</param>
    /// <returns>Whether the current console input mode was read successfully.</returns>
    [DllImport("kernel32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetConsoleMode(IntPtr consoleHandle, out uint mode);

    /// <summary>Updates the Windows console input mode.</summary>
    /// <param name="consoleHandle">Windows console handle to inspect or configure.</param>
    /// <param name="mode">Console mode flags read or applied by the operation.</param>
    /// <returns>Whether the requested console input mode was applied successfully.</returns>
    [DllImport("kernel32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetConsoleMode(IntPtr consoleHandle, uint mode);

    /// <summary>Disables Windows console selection mode so accidental selection cannot pause the host.</summary>
    private static void DisableQuickEditMode()
    {
        if (!OperatingSystem.IsWindows() || !Environment.UserInteractive || Console.IsInputRedirected)
        {
            return;
        }
        const int standardInputHandle = -10;
        const uint enableQuickEditMode = 0x0040;
        const uint enableExtendedFlags = 0x0080;
        var handle = GetStdHandle(standardInputHandle);
        if (GetConsoleMode(handle, out var mode))
        {
            // QuickEdit selection can block console output and stall the agent.
            // Extended flags must be set when changing QuickEdit; preserve all other modes.
            _ = SetConsoleMode(handle, (mode | enableExtendedFlags) & ~enableQuickEditMode);
        }
    }

    /// <summary>Runs the agent host and converts unhandled startup failures into an exit status.</summary>
    /// <param name="args">Command-line options or script arguments associated with the operation.</param>
    /// <returns>The process exit code, with zero indicating normal completion.</returns>
    private static async Task<int> Main(string[] args)
    {
        if (args is ["--health-check"])
        {
            var root = Environment.GetEnvironmentVariable("Updater__DeploymentRoot") ?? "/run/gurux-updater";
            var version = AgentRuntimeVersion.Resolve(AppContext.BaseDirectory, null, FileVersionInfo.GetVersionInfo(typeof(Settings).Assembly.Location).ProductVersion) ?? "";
            return GXAgentContainerHealth.IsHealthy(root, version) ? 0 : 1;
        }
        try
        { return await RunAsync(args); }
        catch (Exception error) { Console.Error.WriteLine($"Agent failed: {error.Message}"); return 1; }
    }

    /// <summary>Runs the configured agent host through registration, server connection and orderly shutdown.</summary>
    /// <param name="args">Command-line options or script arguments associated with the operation.</param>
    /// <returns>The process exit code for the agent host lifetime.</returns>
    private static async Task<int> RunAsync(string[] args)
    {
        DisableQuickEditMode();
        using var cancellation = new CancellationTokenSource();
        using var notifications = new GXAgentHostNotifications();
        Task? containerHeartbeat = null;
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
            using var logging = LoggerFactory.Create(builder => builder.AddConfiguration(config.GetSection("Logging"))
                .AddConsole(options => options.FormatterName = GXAgentConsoleFormatter.FormatterName)
                .AddConsoleFormatter<GXAgentConsoleFormatter, Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions>(
                    options => config.GetSection("Logging:Console").Bind(options)));
            _logger = logging.CreateLogger("Gurux.DLMS.AMI.Agent");
            string? version = AgentRuntimeVersion.Resolve(AppContext.BaseDirectory, null, FileVersionInfo.GetVersionInfo(typeof(Settings).Assembly.Location).ProductVersion);
            var commandLine = new Settings();
            if (Settings.GetParameters([.. args.Where(a => a != "Configure")], commandLine) != 0)
                {
                return 1;
            }
            var updaterOptions = GXAgentUpdaterOptions.Read(config);
            _settingsFile = updaterOptions.SettingsPath;
            if (!string.IsNullOrWhiteSpace(updaterOptions.DeploymentRoot))
            {
                containerHeartbeat = GXAgentContainerHealth.RunAsync(updaterOptions.DeploymentRoot, version ?? "",
                    () => _worker?.State == AgentWorkerState.Running && _worker.ConnectionState == AgentConnectionState.Connected, cancellation.Token);
            }

            settingsLock = GXAgentInstanceLock.Acquire(_settingsFile);
            GXAgentSettingsStore.ProtectExisting(_settingsFile);
            var completionOptions = GXCompletionOutboxOptions.Read(config);
            var scriptOptions = GXScriptExecutionOptions.Read(config);
            var rawDeliveryOptions = new GXRawDeliveryOptions();
            config.GetSection("RawDelivery").Bind(rawDeliveryOptions);
            rawDeliveryOptions.Validate();
            IServiceCollection Services()
            {
                var services = new ServiceCollection();
                services.AddSingleton(updaterOptions);
                services.AddSingleton(completionOptions);
                services.AddSingleton(scriptOptions);
                services.AddSingleton(rawDeliveryOptions);
                services.AddLogging(builder => builder.AddConfiguration(config.GetSection("Logging"))
                .AddConsole(options => options.FormatterName = GXAgentConsoleFormatter.FormatterName)
                .AddConsoleFormatter<GXAgentConsoleFormatter, Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions>(
                    options => config.GetSection("Logging:Console").Bind(options)));
                return services;
            }
            string? settingsJson = File.Exists(_settingsFile)
                ? await File.ReadAllTextAsync(_settingsFile, cancellation.Token)
                : null;
            if (string.IsNullOrWhiteSpace(settingsJson) || args.Contains("Configure"))
            {
                var configured = ReadConfiguredAgent(config, version);
                if (configured != null)
                {
                    await RegisterConfiguredAgentAsync(Services(), notifications, configured, config["Agent:Name"] ?? "Docker agent", cancellation.Token);
                }
                else
                {
                    await RegisterAgent(Services(), notifications, new GXAgentWorker(),
                        new GXAgentOptions { Address = commandLine.Host ?? "https://localhost:8001", Version = version }, cancellation.Token);
                }
                settingsJson = await File.ReadAllTextAsync(_settingsFile, cancellation.Token);
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
            var serviceAddress = new UriBuilder(options.Address) { UserName = "", Password = "", Query = "", Fragment = "" }.Uri.AbsoluteUri;
            _logger.LogInformation("Using AMI service at {Address}", serviceAddress);
            _logger.LogInformation("Starting Gurux.DLMS.AMI.Agent {Version}", version);
            identityLock = GXAgentInstanceLock.AcquireIdentity(options.Address, options.Id);
            async Task ReplaceTokenAsync()
            {
                await _consoleInput.WaitAsync(cancellation.Token);
                try
                {
                    options = await ReadReplacementTokenAsync(options, Environment.UserInteractive && !Console.IsInputRedirected,
                        ReadSecretAsync, cancellation.Token);
                }
                finally
                {
                    _consoleInput.Release();
                }
            }
            async Task StartRuntimeAsync()
            {
                GXAgentContainerHealth.Resume();
                int failedAttempts = 0;
                while (true)
                {
                    cancellation.Token.ThrowIfCancellationRequested();
                    _worker = new GXAgentWorker();
                    _workerInitialized = false;
                    try
                    {
                        _workerServices = await InitializeWorkerAsync(_worker, Services(), options, notifications);
                        _workerInitialized = true;
                        await _worker.StartAsync(cancellation.Token);
                        if (_worker is GXAgentWorker active && active.AuthenticationRejected)
                        {
                            throw new HttpRequestException("AMI authentication failed (HTTP 401).", null, System.Net.HttpStatusCode.Unauthorized);
                        }
                        options = _worker.Configuration.ToOptions();
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
                if (change.HasFlag(AgentHostChange.AuthenticationRequired) && _worker is GXAgentWorker active && active.AuthenticationRejected)
                {
                    options = _worker.Configuration.ToOptions();
                    await DisconnectAfterDrainAsync();
                    await ReplaceTokenAsync();
                    await StartRuntimeAsync();
                    continue;
                }
                if (_workerInitialized)
                    {
                    options = _worker!.Configuration.ToOptions();
                }
                await SaveSettingsAsync(options, cancellation.Token);
                if (!change.HasFlag(AgentHostChange.VersionAvailable))
                    {
                    continue;
                }
                var updater = _workerServices!.GetRequiredService<IGXAgentUpdateService>();
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
                if (!string.IsNullOrWhiteSpace(updaterOptions.DeploymentRoot))
                {
                    var requestId = (updater as GXAgentUpdateService)?.ContainerRequestId
                        ?? throw new InvalidOperationException("Container updater did not provide a request identity.");
                    _logger.LogInformation("Agent is drained and waiting for its container replacement");
                    await GXAgentContainerUpdate.WaitForFailureAsync(updaterOptions.DeploymentRoot, requestId, cancellation.Token);
                    updaterOptions.RetryAfter = DateTimeOffset.UtcNow.AddMinutes(5);
                    await StartRuntimeAsync();
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
            cancellation.Cancel();
            if (containerHeartbeat != null)
            {
                try { await containerHeartbeat; }
                catch (OperationCanceledException) { }
                catch (Exception error) { _logger?.LogWarning(error, "Container health reporting stopped"); }
            }
            Console.CancelKeyPress -= cancel;
            try
            { await DisconnectAfterDrainAsync(); }
            finally { identityLock?.Dispose(); settingsLock?.Dispose(); }
        }
    }

    /// <summary>Builds agent options from environment and host configuration when an address is supplied.</summary>
    /// <param name="config">Application configuration containing updater settings.</param>
    /// <param name="version">Application release version being verified or published.</param>
    /// <returns>Configured agent options, or null when no server address was configured.</returns>
    internal static GXAgentOptions? ReadConfiguredAgent(IConfiguration config, string? version)
    {
        var token = config["Agent:Token"];
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }
        var options = new GXAgentOptions { Address = config["Agent:Address"] ?? "https://ami.gurux.fi", Token = token, Version = version };
        if (!string.IsNullOrWhiteSpace(config["Agent:Id"]))
        {
            options.Id = Guid.Parse(config["Agent:Id"]!);
        }
        options.Validate(requireAgentId: false);
        return options;
    }

    /// <summary>Initializes or registers an agent using noninteractive host configuration.</summary>
    /// <param name="services">Dependency registrations used to initialize the worker.</param>
    /// <param name="notifications">Reports host persistence, authentication and update requests.</param>
    /// <param name="options">Validated configuration used by the runtime operation.</param>
    /// <param name="name">Display name identifying the agent or runtime service.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A task representing this operation: Initializes or registers an agent using noninteractive host configuration.</returns>
    private static async Task RegisterConfiguredAgentAsync(IServiceCollection services, IGXAgentHostNotifications notifications,
        GXAgentOptions options, string name, CancellationToken token)
    {
        if (options.Id != Guid.Empty)
        {
            await SaveSettingsAsync(options, token);
            return;
        }
        var target = new GXAgentWorker();
        ServiceProvider? provider = null;
        try
        {
            provider = await InitializeWorkerAsync(target, services, options, notifications);
            var id = await target.AddAgentAsync(name, token);
            if (id == Guid.Empty)
            {
                throw new InvalidOperationException("Agent registration returned an empty identifier.");
            }
            options = target.Configuration.ToOptions();
            options.Id = id;
            await SaveSettingsAsync(options, token);
        }
        finally
        {
            using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await target.StopAsync(shutdown.Token);
            await target.DisposeAsync().AsTask().WaitAsync(shutdown.Token);
            if (provider != null)
            {
                await provider.DisposeAsync();
            }
        }
    }

    /// <summary>Requests external installation of the release already staged by the worker.</summary>
    /// <param name="updater">Service owning verified release staging and installation handoff.</param>
    /// <param name="processId">Current process identifier that the updater must wait to exit.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A task representing this operation: Requests external installation of the release already staged by the worker.</returns>
    internal static async Task HandoffUpdateAsync(IGXAgentUpdateService updater, int processId, CancellationToken token)
    {
        await DisconnectAfterDrainAsync();
        await updater.LaunchAsync(processId, token);
    }
    /// <summary>Attempts updater handoff and resumes the worker if launching the updater fails.</summary>
    /// <param name="updater">Service owning verified release staging and installation handoff.</param>
    /// <param name="processId">Current process identifier that the updater must wait to exit.</param>
    /// <param name="recover">Resumes normal host operation if updater handoff fails.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>Whether the external updater accepted handoff of the staged release.</returns>
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
    /// <summary>Persists the validated host configuration through the private settings store.</summary>
    /// <param name="options">Validated configuration used by the runtime operation.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A task representing this operation: Persists the validated host configuration through the private settings store.</returns>
    private static async Task SaveSettingsAsync(GXAgentOptions options, CancellationToken token)
    {
        await GXAgentSettingsStore.SaveAsync(_settingsFile, JsonSerializer.Serialize(options), token);
    }

    /// <summary>Stops the worker with the host shutdown deadline enforced.</summary>
    /// <returns>A task representing this operation: Stops the worker with the host shutdown deadline enforced.</returns>
    private static async Task DisconnectWithDeadlineAsync()
    {
        if (_worker == null)
            {
            return;
        }
        using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await _worker.StopAsync(shutdown.Token);
        await _worker.DisposeAsync().AsTask().WaitAsync(shutdown.Token);
        if (_workerServices != null)
            {
            await _workerServices.DisposeAsync().AsTask().WaitAsync(shutdown.Token);
        }
        _workerServices = null;
        _worker = null;
        _workerInitialized = false;
    }

    /// <summary>Waits for admitted worker work to drain before disconnecting the runtime.</summary>
    /// <returns>A task representing this operation: Waits for admitted worker work to drain before disconnecting the runtime.</returns>
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

    /// <summary>Cancels the host polling loop without allowing callback failures to escape shutdown.</summary>
    /// <param name="cancellation">Cancellation source used to stop the polling lifetime.</param>
    private static void ClosePoller(CancellationTokenSource cancellation)
    {
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                _consoleInput.Wait(cancellation.Token);
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
                    _consoleInput.Release();
                }
                Thread.Sleep(100);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (InvalidOperationException) { /* Console input can be detached while shutting down. */ }
    }
}
