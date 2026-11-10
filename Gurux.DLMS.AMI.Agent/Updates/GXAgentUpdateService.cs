using System.Text.Json;
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
using System.IO.Compression;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Gurux.DLMS.AMI.Agent;

/// <summary>Stages a complete release outside the live installation and hands installation to Gurux.Updater.</summary>
internal sealed class GXAgentUpdateService : IGXAgentUpdateService
{
    private readonly GXAgentUpdaterOptions options;
    private readonly ILogger<GXAgentUpdateService> logger;
    private readonly Func<HttpClient> downloadClient;
    private readonly SemaphoreSlim prepareGate = new(1, 1);
    public GXPreparedAgentUpdate? PendingUpdate { get; private set; }
    internal string? ContainerRequestId { get; private set; }
    internal Func<ProcessStartInfo, CancellationToken, Task> StartProcess { get; set; } = StartProcessAsync;

    public GXAgentUpdateService(GXAgentUpdaterOptions options, ILogger<GXAgentUpdateService> logger, Func<HttpClient>? downloadClient = null)
    {
        this.options = options;
        this.logger = logger;
        this.downloadClient = downloadClient ?? (() => new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }));
        CleanupStaging();
    }

    public string? UnavailableReason
    {
        get
        {
            if (options.RetryAfter > DateTimeOffset.UtcNow)
                {
                return "Updater launch failed; waiting before retrying the pending update.";
            }
            try
            { ValidateConfiguration(); return null; }
            catch (Exception error) when (error is InvalidOperationException or ArgumentException or IOException or NotSupportedException)
            { return error.Message; }
        }
    }

    private string ApplicationPath
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(options.ApplicationPath))
            {
                return Path.GetFullPath(options.ApplicationPath);
            }
            string executable = Path.Combine(options.InstallationDirectory,
                OperatingSystem.IsWindows() ? "Gurux.DLMS.AMI.Agent.exe" : "Gurux.DLMS.AMI.Agent");
            return Path.GetFullPath(string.IsNullOrWhiteSpace(options.ServiceName) && File.Exists(executable)
                ? executable : Path.Combine(options.InstallationDirectory, "Gurux.DLMS.AMI.Agent.dll"));
        }
    }

    private static bool Under(string path, string directory)
    {
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    private void ValidateConfiguration()
    {
        if (options.DownloadTimeout <= TimeSpan.Zero || options.DownloadTimeout > TimeSpan.FromHours(24) ||
            options.MaxPackageBytes <= 0 || options.MaxExpandedBytes <= 0 || options.StagingRetention < TimeSpan.FromDays(1))
            {
            throw new InvalidOperationException("Updater limits must be positive, timeout at most 24 hours, and retention at least one day.");
        }
        try
        {
            using var key = RSA.Create();
            key.ImportFromPem(options.PublicKeyPem);
            if (key.KeySize < 2048)
                {
                throw new InvalidOperationException("Release signing key must be at least 2048 bits.");
            }
        }
        catch (Exception error) when (error is ArgumentException or CryptographicException)
        { throw new InvalidOperationException("Configure Updater:PublicKeyPem with the trusted release signing public key.", error); }
        if (!string.IsNullOrWhiteSpace(options.DeploymentRoot))
        {
            var heartbeat = Path.Combine(options.DeploymentRoot, "agent-heartbeat");
            if (!Path.IsPathFullyQualified(options.DeploymentRoot) || string.IsNullOrWhiteSpace(options.RequestKeyFile) ||
                !Path.IsPathFullyQualified(options.RequestKeyFile) || !File.Exists(options.RequestKeyFile) ||
                !File.Exists(heartbeat) || DateTime.UtcNow - File.GetLastWriteTimeUtc(heartbeat) >= TimeSpan.FromSeconds(30))
            {
                throw new InvalidOperationException("The independent container updater is not ready.");
            }
            var retry = GXAgentContainerUpdate.RetryAfter(options.DeploymentRoot);
            if (retry > DateTimeOffset.UtcNow)
            {
                options.RetryAfter = retry;
                throw new InvalidOperationException("A previous container update failed; waiting before retrying.");
            }
            if (Under(options.StagingDirectory, options.InstallationDirectory))
            {
                throw new InvalidOperationException("Container update verification must use persistent staging outside the application.");
            }
            return;
        }
        if (string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase) ||
            !OperatingSystem.IsWindows() && File.Exists("/.dockerenv"))
            {
            throw new InvalidOperationException("Container agents are updated by replacing their container image.");
        }
        if (!string.IsNullOrWhiteSpace(options.VersionUrl))
            {
            throw new InvalidOperationException("Use Updater:HealthUrl for local package installation; --version-url is not supported in local mode.");
        }
        if (!string.IsNullOrWhiteSpace(options.HealthUrl) &&
            (!Uri.TryCreate(options.HealthUrl, UriKind.Absolute, out var health) || health.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(health.UserInfo)))
            {
            throw new InvalidOperationException("Updater:HealthUrl must be an absolute HTTP(S) URL without credentials.");
        }
        if (OperatingSystem.IsLinux() && !string.IsNullOrWhiteSpace(options.ServiceName) && !File.Exists("/usr/bin/systemd-run"))
            {
            throw new InvalidOperationException("Service updates require systemd-run to launch the updater outside the agent service.");
        }
        if (string.IsNullOrWhiteSpace(options.Executable) || !Path.IsPathFullyQualified(options.Executable) || !File.Exists(options.Executable))
            {
            throw new InvalidOperationException("Configure Updater:Executable with the absolute path of an installed external Gurux.Updater tool.");
        }
        if (Under(options.Executable, options.InstallationDirectory))
            {
            throw new InvalidOperationException("Gurux.Updater must be installed outside the agent installation directory.");
        }
        if (Under(options.StagingDirectory, options.InstallationDirectory) || Path.GetFullPath(options.StagingDirectory) == Path.GetFullPath(options.InstallationDirectory))
            {
            throw new InvalidOperationException("Update staging must be outside the live installation directory.");
        }
        if (!Under(ApplicationPath, options.InstallationDirectory) || !File.Exists(ApplicationPath))
            {
            throw new InvalidOperationException("Configure Updater:ApplicationPath with the installed agent application.");
        }
    }

    public async Task PrepareAsync(string version, IEnumerable<string> urls, CancellationToken token)
    {
        ValidateConfiguration();
        if (string.IsNullOrWhiteSpace(version) || version.Length > 128 || version.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('.' or '-' or '_' or '+')))
            {
            throw new ArgumentException("Invalid requested agent version.", nameof(version));
        }
        await prepareGate.WaitAsync(token);
        try
        {
            if (PendingUpdate?.Version == version)
                {
                return;
            }
            CleanupStaging();
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(options.DownloadTimeout);
            var transferToken = deadline.Token;
            if (!string.IsNullOrWhiteSpace(options.DeploymentRoot))
            {
                await ValidateContainerReleaseAsync(version, transferToken);
            }
            Exception? last = null;
            foreach (var address in urls)
            {
                token.ThrowIfCancellationRequested();
                if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != "https" || !string.IsNullOrEmpty(uri.UserInfo))
                    {
                    throw new InvalidDataException("Agent update URL must be an absolute HTTPS URL without credentials.");
                }
                string stage = Path.Combine(Path.GetFullPath(options.StagingDirectory), Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(stage);
                try
                {
                    string package = Path.Combine(stage, "agent.zip");
                    // Do not send the AMI personal access token to a release download host.
                    using var lease = new FileStream(Path.Combine(stage, ".active"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    using var http = downloadClient();
                    var manifestUri = new UriBuilder(uri) { Path = uri.AbsolutePath + ".manifest.json" }.Uri;
                    using var metadata = new MemoryStream();
                    await DownloadAsync(http, manifestUri, metadata, 64 * 1024, transferToken);
                    var manifest = GXAgentReleaseManifest.Verify(metadata.ToArray(), version, options.PublicKeyPem);
                    await using (var file = File.Create(package))
                        await DownloadAsync(http, uri, file, options.MaxPackageBytes, transferToken);
                    await using var input = File.OpenRead(package);
                    string digest = Convert.ToHexString(await SHA256.HashDataAsync(input, transferToken)).ToLowerInvariant();
                    if (digest != manifest.Sha256)
                        {
                        throw new InvalidDataException("Package digest does not match the signed release manifest.");
                    }
                    await ValidatePackageAsync(package, stage, version, transferToken);
                    PendingUpdate = new GXPreparedAgentUpdate(version, package, digest);
                    logger.LogInformation("Agent release {Version} staged for external installation", version);
                    return;
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    Directory.Delete(stage, recursive: true);
                    throw;
                }
                catch (OperationCanceledException error)
                {
                    Directory.Delete(stage, recursive: true);
                    throw new InvalidOperationException("Agent release download exceeded its time limit.", error);
                }
                catch (Exception error) when (error is HttpRequestException or IOException or InvalidDataException or InvalidOperationException)
                {
                    last = error;
                    Directory.Delete(stage, recursive: true);
                }
            }
            throw new InvalidOperationException("No valid complete agent application package was available.", last);
        }
        finally { prepareGate.Release(); }
    }

    private async Task ValidatePackageAsync(string package, string stage, string version, CancellationToken token)
    {
        using var archive = ZipFile.OpenRead(package);
        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        long expanded = 0;
        if (archive.Entries.Count > 10000)
            {
            throw new InvalidDataException("Agent package contains too many entries.");
        }
        foreach (var entry in archive.Entries)
        {
            token.ThrowIfCancellationRequested();
            if (entry.Length > options.MaxExpandedBytes - expanded)
                {
                throw new InvalidDataException("Agent package expanded size exceeds its limit.");
            }
            expanded += entry.Length;
            string name = entry.FullName.Replace('\\', '/');
            if (name.StartsWith('/') || name.Contains(':') || name.Split('/').Any(p => p is "." or "..") ||
                ((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000)
                {
                throw new InvalidDataException("Agent package contains an unsafe path or symbolic link.");
            }
            if (name.EndsWith('/'))
                {
                continue;
            }
            if (!entries.TryAdd(name, entry))
                {
                throw new InvalidDataException("Agent package contains duplicate paths.");
            }
        }
        foreach (string name in new[] { "Gurux.DLMS.AMI.Agent.dll", "Gurux.DLMS.AMI.Agent.deps.json", "Gurux.DLMS.AMI.Agent.runtimeconfig.json" })
            {
            if (!entries.ContainsKey(name))
                {
                throw new InvalidDataException($"Agent package is missing {name} at its root.");
            }
        }
        if (entries.ContainsKey("settings.json") || entries.ContainsKey("appsettings.json") || entries.ContainsKey("Gurux.DLMS.AMI.Agent.Worker.dll") || entries.ContainsKey("Gurux.DLMS.AMI.Agent.Shared.dll"))
            {
            throw new InvalidDataException("Agent package contains deployment/registration settings or obsolete split application assemblies.");
        }
        if (string.IsNullOrWhiteSpace(options.ServiceName) && !entries.ContainsKey(Path.GetFileName(ApplicationPath)))
            {
            throw new InvalidDataException("Standalone update package must include its agent application entry point.");
        }
        string metadata = Path.Combine(stage, "candidate.dll");
        await using (var output = File.Create(metadata))
        await using (var input = entries["Gurux.DLMS.AMI.Agent.dll"].Open())
            await CopyBoundedAsync(input, output, Math.Min(options.MaxExpandedBytes, entries["Gurux.DLMS.AMI.Agent.dll"].Length), token);
        string? actual = FileVersionInfo.GetVersionInfo(metadata).ProductVersion?.Split('+')[0];
        File.Delete(metadata);
        if (actual != version.Split('+')[0])
            {
            throw new InvalidDataException("Agent package version does not match the requested version.");
        }
    }

    private async Task ValidateContainerReleaseAsync(string version, CancellationToken token)
    {
        var tag = Uri.EscapeDataString("Gurux.DLMS.AMI.Agent/v" + version);
        var uri = new Uri($"https://github.com/Gurux/Gurux.DLMS.AMI4/releases/download/{tag}/Gurux.DLMS.AMI.Agent-{version}.compose.json");
        using var http = downloadClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Gurux.DLMS.AMI.Agent/1.0");
        using var metadata = new MemoryStream();
        await DownloadAsync(http, uri, metadata, 65536, token);
        metadata.Position = 0;
        using var document = await System.Text.Json.JsonDocument.ParseAsync(metadata, cancellationToken: token);
        var root = document.RootElement;
        if (root.GetProperty("schemaVersion").GetInt32() != 1 || root.GetProperty("version").GetString() != version)
        {
            throw new InvalidDataException("Agent container metadata does not match the requested release.");
        }
        var compose = root.GetProperty("compose").Deserialize<Gurux.Updater.Model.GXComposeRelease>()
            ?? throw new InvalidDataException("Agent release has no container metadata.");
        var platform = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture switch
        {
            System.Runtime.InteropServices.Architecture.Arm64 => "linux/arm64",
            System.Runtime.InteropServices.Architecture.Arm => "linux/arm/v7",
            _ => "linux/amd64"
        };
        var current = AgentRuntimeVersion.Resolve(AppContext.BaseDirectory, null,
            FileVersionInfo.GetVersionInfo(typeof(GXAgentUpdateService).Assembly.Location).ProductVersion) ?? "0.0.0";
        Gurux.Updater.Services.GXComposeDeploymentService.ValidateRelease(new Gurux.Updater.Model.GXDeploymentOptions
        {
            Compose = new Gurux.Updater.Model.GXComposeOptions
            {
                Services = new() { ["agent"] = "agent" }, AllowedImageRepositories = new() { "guruxorg/dlms-ami-agent" },
                Platform = platform, WasmRole = "agent"
            }
        }, new Gurux.Updater.Model.GXUpdateRelease { Version = version, Compose = compose }, current);
        if (compose.ContainsWasm)
        {
            throw new InvalidDataException("An agent image cannot declare an AMI browser deployment.");
        }
    }

    private static async Task DownloadAsync(HttpClient http, Uri uri, Stream output, long limit, CancellationToken token)
    {
        for (int redirects = 0; redirects <= 5; ++redirects)
        {
            if (uri.Scheme != "https" || !string.IsNullOrEmpty(uri.UserInfo))
                {
                throw new InvalidDataException("Release redirects must use HTTPS without credentials.");
            }
            using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);
            if ((int)response.StatusCode is 301 or 302 or 303 or 307 or 308)
            {
                uri = new Uri(uri, response.Headers.Location ?? throw new InvalidDataException("Missing redirect location."));
                continue;
            }
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > limit)
                {
                throw new InvalidDataException("Release download exceeds its size limit.");
            }
            await using var input = await response.Content.ReadAsStreamAsync(token);
            await CopyBoundedAsync(input, output, limit, token);
            return;
        }
        throw new InvalidDataException("Too many release redirects.");
    }

    private static async Task CopyBoundedAsync(Stream input, Stream output, long limit, CancellationToken token)
    {
        byte[] buffer = new byte[81920];
        long total = 0;
        int count;
        while ((count = await input.ReadAsync(buffer, token)) != 0)
        {
            if (count > limit - total)
                {
                throw new InvalidDataException("Release content exceeds its size limit.");
            }
            await output.WriteAsync(buffer.AsMemory(0, count), token);
            total += count;
        }
    }

    internal void CleanupStaging()
    {
        try
        { CleanupStagingCore(); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { logger.LogWarning(error, "Update stage cleanup failed; agent operation continues"); }
    }

    private void CleanupStagingCore()
    {
        // Startup cleanup must never act on an invalid live-install staging configuration.
        if (options.StagingRetention < TimeSpan.FromDays(1) || Under(options.StagingDirectory, options.InstallationDirectory) ||
            string.Equals(Path.GetFullPath(options.StagingDirectory), Path.GetFullPath(options.InstallationDirectory),
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
            return;
        }
        if (!Directory.Exists(options.StagingDirectory))
            {
            return;
        }
        foreach (var folder in new DirectoryInfo(options.StagingDirectory).EnumerateDirectories())
        {
            if (!Guid.TryParseExact(folder.Name, "N", out _) || folder.Attributes.HasFlag(FileAttributes.ReparsePoint) ||
                folder.LastWriteTimeUtc >= DateTime.UtcNow - options.StagingRetention ||
                string.Equals(folder.FullName, Path.GetDirectoryName(PendingUpdate?.PackagePath), StringComparison.OrdinalIgnoreCase))
                {
                continue;
            }
            try
            {
                using (new FileStream(Path.Combine(folder.FullName, ".active"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                { }
                // Delete only files owned by this adapter, never recurse into unexpected contents or links.
                if (folder.EnumerateDirectories().Any() || folder.EnumerateFiles().Any(f => f.Attributes.HasFlag(FileAttributes.ReparsePoint) || f.Name is not ("agent.zip" or "candidate.dll" or ".active")))
                    {
                    continue;
                }
                foreach (var file in folder.EnumerateFiles())
                    {
                    file.Delete();
                }
                folder.Delete();
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            { logger.LogWarning(error, "Could not remove expired update stage {Stage}", folder.Name); }
        }
    }

    internal ProcessStartInfo CreateStartInfo(int processId)
    {
        ValidateConfiguration();
        var pending = PendingUpdate ?? throw new InvalidOperationException("No agent update has been staged.");
        if (processId <= 0)
            {
            throw new ArgumentOutOfRangeException(nameof(processId));
        }
        var command = new List<string>();
        bool toolAssembly = Path.GetExtension(options.Executable).Equals(".dll", StringComparison.OrdinalIgnoreCase);
        string executable = toolAssembly ? "dotnet" : options.Executable;
        if (toolAssembly)
            {
            command.Add(options.Executable);
        }
        command.AddRange(["update", "--local", pending.PackagePath, "--destination", Path.GetFullPath(options.InstallationDirectory),
            "--application", ApplicationPath, "--sha256", pending.Sha256]);
        if (!string.IsNullOrWhiteSpace(options.ServiceName))
            {
            command.AddRange(["--service", options.ServiceName]);
        }
        else
            {
            command.AddRange(["--process-id", processId.ToString(System.Globalization.CultureInfo.InvariantCulture), "--wait-seconds", "120"]);
        }
        if (!string.IsNullOrWhiteSpace(options.HealthUrl))
            {
            command.AddRange(["--health-url", options.HealthUrl]);
        }
        // A local package has no remote release version metadata; the CLI does not accept --version-url in local mode.
        if (!string.IsNullOrWhiteSpace(options.VersionUrl))
            {
            throw new InvalidOperationException("Use Updater:HealthUrl for local package installation; the updater does not support --version-url in local mode.");
        }
        if (OperatingSystem.IsLinux() && !string.IsNullOrWhiteSpace(options.ServiceName))
        {
            // Launch in a separate systemd unit so stopping the agent service cannot kill its updater child.
            command.Insert(0, executable);
            command.InsertRange(0, ["--quiet", "--collect", "--unit", "gurux-agent-update-" + Guid.NewGuid().ToString("N"), "--"]);
            executable = "/usr/bin/systemd-run";
        }
        var info = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = Path.GetDirectoryName(pending.PackagePath)!
        };
        info.Environment["GURUX_AGENT_SETTINGS"] = Path.GetFullPath(options.SettingsPath);
        foreach (var argument in command)
            {
            info.ArgumentList.Add(argument);
        }
        return info;
    }

    public async Task LaunchAsync(int processId, CancellationToken token)
    {
        if (!string.IsNullOrWhiteSpace(options.DeploymentRoot))
        {
            ValidateConfiguration();
            var pending = PendingUpdate ?? throw new InvalidOperationException("No verified update is staged.");
            GXAgentContainerHealth.MarkDrained();
            var current = AgentRuntimeVersion.Resolve(AppContext.BaseDirectory, null,
                FileVersionInfo.GetVersionInfo(typeof(GXAgentUpdateService).Assembly.Location).ProductVersion) ?? "";
            await GXAgentContainerHealth.WriteAsync(options.DeploymentRoot, current, false, token);
            ContainerRequestId = await GXAgentContainerUpdate.QueueAsync(options.DeploymentRoot, options.RequestKeyFile!, pending.Version, token);
            return;
        }
        var info = CreateStartInfo(processId);
        await StartProcess(info, token);
    }

    private static async Task StartProcessAsync(ProcessStartInfo info, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var process = Process.Start(info) ?? throw new InvalidOperationException("Could not start Gurux.Updater.");
        var exit = process.WaitForExitAsync();
        if (Path.GetFileName(info.FileName) == "systemd-run")
        {
            await exit.WaitAsync(token);
            if (process.ExitCode != 0)
                {
                throw new InvalidOperationException("systemd-run could not start the updater outside the agent service.");
            }
        }
        else if (await Task.WhenAny(exit, Task.Delay(300, token)) == exit && process.ExitCode != 0)
            {
            throw new InvalidOperationException($"Gurux.Updater failed to start (exit code {process.ExitCode}).");
        }
    }
}
