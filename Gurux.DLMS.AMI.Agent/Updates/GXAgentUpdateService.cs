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

using System.Text.Json;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Gurux.DLMS.AMI.Agent;

/// <summary>Stages a complete release outside the live installation and hands installation to Gurux.Updater.</summary>
internal sealed class GXAgentUpdateService : IGXAgentUpdateService
{
    /// <summary>Validated limits and settings used by this component.</summary>
    private readonly GXAgentUpdaterOptions _options;
    /// <summary>Records runtime diagnostics without exposing authentication credentials.</summary>
    private readonly ILogger<GXAgentUpdateService> _logger;
    /// <summary>Creates a client for downloading authenticated release assets.</summary>
    private readonly Func<HttpClient> _downloadClient;
    /// <summary>Serializes validation and staging of downloaded releases.</summary>
    private readonly SemaphoreSlim _prepareGate = new(1, 1);
    /// <summary>Gets the verified release currently staged for external installation.</summary>
    public GXPreparedAgentUpdate? PendingUpdate { get; private set; }
    /// <summary>Gets the queued container deployment request awaiting acknowledgement.</summary>
    internal string? ContainerRequestId { get; private set; }
    /// <summary>Gets or sets the process launcher used for external updater handoff.</summary>
    internal Func<ProcessStartInfo, CancellationToken, Task> StartProcess { get; set; } = StartProcessAsync;

    /// <summary>Initializes verified release preparation and external installation handoff.</summary>
    /// <param name="options">Validated configuration used by the runtime operation.</param>
    /// <param name="logger">Records operation diagnostics without exposing authentication secrets.</param>
    /// <param name="downloadClient">Optional factory creating authenticated release download clients.</param>
    public GXAgentUpdateService(GXAgentUpdaterOptions options, ILogger<GXAgentUpdateService> logger, Func<HttpClient>? downloadClient = null)
    {
        this._options = options;
        this._logger = logger;
        this._downloadClient = downloadClient ?? (() => new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }));
        CleanupStaging();
    }

    /// <summary>Gets the configuration error preventing safe updater handoff, if any.</summary>
    public string? UnavailableReason
    {
        get
        {
            if (_options.RetryAfter > DateTimeOffset.UtcNow)
                {
                return "Updater launch failed; waiting before retrying the pending update.";
            }
            try
            { ValidateConfiguration(); return null; }
            catch (Exception error) when (error is InvalidOperationException or ArgumentException or IOException or NotSupportedException)
            { return error.Message; }
        }
    }

    /// <summary>Gets the executable application path used for updater handoff.</summary>
    private string ApplicationPath
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_options.ApplicationPath))
            {
                return Path.GetFullPath(_options.ApplicationPath);
            }
            string executable = Path.Combine(_options.InstallationDirectory,
                OperatingSystem.IsWindows() ? "Gurux.DLMS.AMI.Agent.exe" : "Gurux.DLMS.AMI.Agent");
            return Path.GetFullPath(string.IsNullOrWhiteSpace(_options.ServiceName) && File.Exists(executable)
                ? executable : Path.Combine(_options.InstallationDirectory, "Gurux.DLMS.AMI.Agent.dll"));
        }
    }

    /// <summary>Checks that a resolved filesystem path remains inside the expected directory.</summary>
    /// <param name="path">File or directory on which the operation acts.</param>
    /// <param name="directory">Expected parent directory used to constrain resolved paths.</param>
    /// <returns>Whether the resolved path is contained by the expected directory.</returns>
    private static bool Under(string path, string directory)
    {
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    /// <summary>Rejects updater settings that cannot safely stage or hand off an application release.</summary>
    private void ValidateConfiguration()
    {
        if (_options.DownloadTimeout <= TimeSpan.Zero || _options.DownloadTimeout > TimeSpan.FromHours(24) ||
            _options.MaxPackageBytes <= 0 || _options.MaxExpandedBytes <= 0 || _options.StagingRetention < TimeSpan.FromDays(1))
            {
            throw new InvalidOperationException("Updater limits must be positive, timeout at most 24 hours, and retention at least one day.");
        }
        try
        {
            using var key = RSA.Create();
            key.ImportFromPem(_options.PublicKeyPem);
            if (key.KeySize < 2048)
                {
                throw new InvalidOperationException("Release signing key must be at least 2048 bits.");
            }
        }
        catch (Exception error) when (error is ArgumentException or CryptographicException)
        { throw new InvalidOperationException("Configure Updater:PublicKeyPem with the trusted release signing public key.", error); }
        if (!string.IsNullOrWhiteSpace(_options.DeploymentRoot))
        {
            var heartbeat = Path.Combine(_options.DeploymentRoot, "agent-heartbeat");
            if (!Path.IsPathFullyQualified(_options.DeploymentRoot) || string.IsNullOrWhiteSpace(_options.RequestKeyFile) ||
                !Path.IsPathFullyQualified(_options.RequestKeyFile) || !File.Exists(_options.RequestKeyFile) ||
                !File.Exists(heartbeat) || DateTime.UtcNow - File.GetLastWriteTimeUtc(heartbeat) >= TimeSpan.FromSeconds(30))
            {
                throw new InvalidOperationException("The independent container updater is not ready.");
            }
            var retry = GXAgentContainerUpdate.RetryAfter(_options.DeploymentRoot);
            if (retry > DateTimeOffset.UtcNow)
            {
                _options.RetryAfter = retry;
                throw new InvalidOperationException("A previous container update failed; waiting before retrying.");
            }
            if (Under(_options.StagingDirectory, _options.InstallationDirectory))
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
        if (!string.IsNullOrWhiteSpace(_options.VersionUrl))
            {
            throw new InvalidOperationException("Use Updater:HealthUrl for local package installation; --version-url is not supported in local mode.");
        }
        if (!string.IsNullOrWhiteSpace(_options.HealthUrl) &&
            (!Uri.TryCreate(_options.HealthUrl, UriKind.Absolute, out var health) || health.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(health.UserInfo)))
            {
            throw new InvalidOperationException("Updater:HealthUrl must be an absolute HTTP(S) URL without credentials.");
        }
        if (OperatingSystem.IsLinux() && !string.IsNullOrWhiteSpace(_options.ServiceName) && !File.Exists("/usr/bin/systemd-run"))
            {
            throw new InvalidOperationException("Service updates require systemd-run to launch the updater outside the agent service.");
        }
        if (string.IsNullOrWhiteSpace(_options.Executable) || !Path.IsPathFullyQualified(_options.Executable) || !File.Exists(_options.Executable))
            {
            throw new InvalidOperationException("Configure Updater:Executable with the absolute path of an installed external Gurux.Updater tool.");
        }
        if (Under(_options.Executable, _options.InstallationDirectory))
            {
            throw new InvalidOperationException("Gurux.Updater must be installed outside the agent installation directory.");
        }
        if (Under(_options.StagingDirectory, _options.InstallationDirectory) || Path.GetFullPath(_options.StagingDirectory) == Path.GetFullPath(_options.InstallationDirectory))
            {
            throw new InvalidOperationException("Update staging must be outside the live installation directory.");
        }
        if (!Under(ApplicationPath, _options.InstallationDirectory) || !File.Exists(ApplicationPath))
            {
            throw new InvalidOperationException("Configure Updater:ApplicationPath with the installed agent application.");
        }
    }

    /// <summary>Downloads, verifies and stages an application release before host shutdown begins.</summary>
    /// <param name="version">Application release version being verified or published.</param>
    /// <param name="urls">Candidate package download locations supplied by the server.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A task representing this operation: Downloads, verifies and stages an application release before host shutdown begins.</returns>
    public async Task PrepareAsync(string version, IEnumerable<string> urls, CancellationToken token)
    {
        ValidateConfiguration();
        if (string.IsNullOrWhiteSpace(version) || version.Length > 128 || version.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('.' or '-' or '_' or '+')))
            {
            throw new ArgumentException("Invalid requested agent version.", nameof(version));
        }
        await _prepareGate.WaitAsync(token);
        try
        {
            if (PendingUpdate?.Version == version)
                {
                return;
            }
            CleanupStaging();
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(_options.DownloadTimeout);
            var transferToken = deadline.Token;
            if (!string.IsNullOrWhiteSpace(_options.DeploymentRoot))
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
                string stage = Path.Combine(Path.GetFullPath(_options.StagingDirectory), Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(stage);
                try
                {
                    string package = Path.Combine(stage, "agent.zip");
                    // Do not send the AMI personal access token to a release download host.
                    using var lease = new FileStream(Path.Combine(stage, ".active"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    using var http = _downloadClient();
                    var manifestUri = new UriBuilder(uri) { Path = uri.AbsolutePath + ".manifest.json" }.Uri;
                    using var metadata = new MemoryStream();
                    await DownloadAsync(http, manifestUri, metadata, 64 * 1024, transferToken);
                    var manifest = GXAgentReleaseManifest.Verify(metadata.ToArray(), version, _options.PublicKeyPem);
                    await using (var file = File.Create(package))
                        await DownloadAsync(http, uri, file, _options.MaxPackageBytes, transferToken);
                    await using var input = File.OpenRead(package);
                    string digest = Convert.ToHexString(await SHA256.HashDataAsync(input, transferToken)).ToLowerInvariant();
                    if (digest != manifest.Sha256)
                        {
                        throw new InvalidDataException("Package digest does not match the signed release manifest.");
                    }
                    package = await ValidatePackageAsync(package, stage, version, transferToken);
                    await using (var prepared = File.OpenRead(package))
                    {
                        digest = Convert.ToHexString(await SHA256.HashDataAsync(prepared, transferToken)).ToLowerInvariant();
                    }
                    PendingUpdate = new GXPreparedAgentUpdate(version, package, digest);
                    _logger.LogInformation("Agent release {Version} staged for external installation", version);
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
        finally { _prepareGate.Release(); }
    }

    /// <summary>Validates package paths, expanded size, application version and release signature.</summary>
    /// <param name="package">Downloaded archive to validate before staging.</param>
    /// <param name="stage">Private staging directory where verified package contents are prepared.</param>
    /// <param name="version">Application release version being verified or published.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>The validated staged application path prepared for external installation.</returns>
    private async Task<string> ValidatePackageAsync(string package, string stage, string version, CancellationToken token)
    {
        using var archive = ZipFile.OpenRead(package);
        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        bool structured = archive.Entries.Any(e => e.FullName.Replace('\\', '/') == "gx-agent/agent/Gurux.DLMS.AMI.Agent.dll");
        bool wrapped = structured || archive.Entries.Any(e => e.FullName.Replace('\\', '/') == "gx-agent/Gurux.DLMS.AMI.Agent.dll");
        long expanded = 0;
        if (archive.Entries.Count > 10000)
            {
            throw new InvalidDataException("Agent package contains too many entries.");
        }
        foreach (var entry in archive.Entries)
        {
            token.ThrowIfCancellationRequested();
            if (entry.Length > _options.MaxExpandedBytes - expanded)
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
            if (wrapped)
            {
                if (!name.StartsWith("gx-agent/", StringComparison.Ordinal))
                {
                    throw new InvalidDataException("Agent package contains files outside gx-agent/.");
                }
                name = name["gx-agent/".Length..];
                if (structured)
                {
                    if (name.StartsWith("agent/", StringComparison.Ordinal))
                    {
                        name = name["agent/".Length..];
                    }
                    else if (name.StartsWith("gurux-updater/", StringComparison.Ordinal) ||
                        name is "start.sh" or "start.bat")
                    {
                        // The running updater and root launchers remain outside the live agent directory.
                        continue;
                    }
                    else
                    {
                        throw new InvalidDataException("Agent package contains unexpected files outside agent/.");
                    }
                }
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
        if (string.IsNullOrWhiteSpace(_options.ServiceName) && !entries.ContainsKey(Path.GetFileName(ApplicationPath)))
            {
            throw new InvalidDataException("Standalone update package must include its agent application entry point.");
        }
        string metadata = Path.Combine(stage, "candidate.dll");
        await using (var output = File.Create(metadata))
        await using (var input = entries["Gurux.DLMS.AMI.Agent.dll"].Open())
            await CopyBoundedAsync(input, output, Math.Min(_options.MaxExpandedBytes, entries["Gurux.DLMS.AMI.Agent.dll"].Length), token);
        string? actual = FileVersionInfo.GetVersionInfo(metadata).ProductVersion?.Split('+')[0];
        File.Delete(metadata);
        if (actual != version.Split('+')[0])
            {
            throw new InvalidDataException("Agent package version does not match the requested version.");
        }
        if (!wrapped)
        {
            return package;
        }
        // The signed download is verified before this conversion. The external updater
        // expects application files at the ZIP root and verifies the converted ZIP hash.
        string installationPackage = Path.Combine(stage, "install.zip");
        using (var output = ZipFile.Open(installationPackage, ZipArchiveMode.Create))
        {
            foreach (var entry in entries)
            {
                var target = output.CreateEntry(entry.Key);
                target.ExternalAttributes = entry.Value.ExternalAttributes;
                target.LastWriteTime = entry.Value.LastWriteTime;
                await using var source = entry.Value.Open();
                await using var destination = target.Open();
                await CopyBoundedAsync(source, destination, entry.Value.Length, token);
            }
        }
        return installationPackage;
    }

    /// <summary>Verifies immutable container release metadata before queuing replacement.</summary>
    /// <param name="version">Application release version being verified or published.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A task representing this operation: Verifies immutable container release metadata before queuing replacement.</returns>
    private async Task ValidateContainerReleaseAsync(string version, CancellationToken token)
    {
        var tag = Uri.EscapeDataString("Gurux.DLMS.AMI.Agent/v" + version);
        var uri = new Uri($"https://github.com/Gurux/Gurux.DLMS.AMI4/releases/download/{tag}/Gurux.DLMS.AMI.Agent-{version}.compose.json");
        using var http = _downloadClient();
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
                Services = new() { ["agent"] = "agent" }, AllowedImageRepositories = ["guruxorg/dlms-ami-agent"],
                Platform = platform, WasmRole = "agent"
            }
        }, new Gurux.Updater.Model.GXUpdateRelease { Version = version, Compose = compose }, current);
        if (compose.ContainsWasm)
        {
            throw new InvalidDataException("An agent image cannot declare an AMI browser deployment.");
        }
    }

    /// <summary>Downloads and validates content required to prepare an application or script release.</summary>
    /// <param name="http">Configured HTTP client used for server communication.</param>
    /// <param name="uri">Validated release download address.</param>
    /// <param name="output">Destination stream receiving bounded downloaded bytes.</param>
    /// <param name="limit">Maximum number of bytes accepted from the input stream.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A task representing this operation: Downloads and validates content required to prepare an application or script release.</returns>
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

    /// <summary>Copies a download stream while enforcing the configured package size limit.</summary>
    /// <param name="input">Source stream containing downloaded package bytes.</param>
    /// <param name="output">Destination stream receiving bounded downloaded bytes.</param>
    /// <param name="limit">Maximum number of bytes accepted from the input stream.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A task representing this operation: Copies a download stream while enforcing the configured package size limit.</returns>
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

    /// <summary>Removes expired staged releases while preserving the pending update.</summary>
    internal void CleanupStaging()
    {
        try
        { CleanupStagingCore(); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { _logger.LogWarning(error, "Update stage cleanup failed; agent operation continues"); }
    }

    /// <summary>Deletes expired staging entries after checking they are not the pending release.</summary>
    private void CleanupStagingCore()
    {
        // Startup cleanup must never act on an invalid live-install staging configuration.
        if (_options.StagingRetention < TimeSpan.FromDays(1) || Under(_options.StagingDirectory, _options.InstallationDirectory) ||
            string.Equals(Path.GetFullPath(_options.StagingDirectory), Path.GetFullPath(_options.InstallationDirectory),
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
            return;
        }
        if (!Directory.Exists(_options.StagingDirectory))
            {
            return;
        }
        foreach (var folder in new DirectoryInfo(_options.StagingDirectory).EnumerateDirectories())
        {
            if (!Guid.TryParseExact(folder.Name, "N", out _) || folder.Attributes.HasFlag(FileAttributes.ReparsePoint) ||
                folder.LastWriteTimeUtc >= DateTime.UtcNow - _options.StagingRetention ||
                string.Equals(folder.FullName, Path.GetDirectoryName(PendingUpdate?.PackagePath), StringComparison.OrdinalIgnoreCase))
                {
                continue;
            }
            try
            {
                using (new FileStream(Path.Combine(folder.FullName, ".active"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                { }
                // Delete only files owned by this adapter, never recurse into unexpected contents or links.
                if (folder.EnumerateDirectories().Any() || folder.EnumerateFiles().Any(f => f.Attributes.HasFlag(FileAttributes.ReparsePoint) || f.Name is not ("agent.zip" or "install.zip" or "candidate.dll" or ".active")))
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
            { _logger.LogWarning(error, "Could not remove expired update stage {Stage}", folder.Name); }
        }
    }

    /// <summary>Builds the external updater command using the staged release and current process identity.</summary>
    /// <param name="processId">Current process identifier that the updater must wait to exit.</param>
    /// <returns>Process settings containing the verified release and current process handoff arguments.</returns>
    internal ProcessStartInfo CreateStartInfo(int processId)
    {
        ValidateConfiguration();
        var pending = PendingUpdate ?? throw new InvalidOperationException("No agent update has been staged.");
        if (processId <= 0)
            {
            throw new ArgumentOutOfRangeException(nameof(processId));
        }
        var command = new List<string>();
        bool toolAssembly = Path.GetExtension(_options.Executable).Equals(".dll", StringComparison.OrdinalIgnoreCase);
        string executable = toolAssembly ? "dotnet" : _options.Executable;
        if (toolAssembly)
            {
            command.Add(_options.Executable);
        }
        command.AddRange(["update", "--local", pending.PackagePath, "--destination", Path.GetFullPath(_options.InstallationDirectory),
            "--application", ApplicationPath, "--sha256", pending.Sha256]);
        if (!string.IsNullOrWhiteSpace(_options.ServiceName))
            {
            command.AddRange(["--service", _options.ServiceName]);
        }
        else
            {
            command.AddRange(["--process-id", processId.ToString(System.Globalization.CultureInfo.InvariantCulture), "--wait-seconds", "120"]);
        }
        if (!string.IsNullOrWhiteSpace(_options.HealthUrl))
            {
            command.AddRange(["--health-url", _options.HealthUrl]);
        }
        // A local package has no remote release version metadata; the CLI does not accept --version-url in local mode.
        if (!string.IsNullOrWhiteSpace(_options.VersionUrl))
            {
            throw new InvalidOperationException("Use Updater:HealthUrl for local package installation; the updater does not support --version-url in local mode.");
        }
        if (OperatingSystem.IsLinux() && !string.IsNullOrWhiteSpace(_options.ServiceName))
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
        info.Environment["GURUX_AGENT_SETTINGS"] = Path.GetFullPath(_options.SettingsPath);
        foreach (var argument in command)
            {
            info.ArgumentList.Add(argument);
        }
        return info;
    }

    /// <summary>Hands the verified update to the external updater or authenticated container queue.</summary>
    /// <param name="processId">Current process identifier that the updater must wait to exit.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A task representing this operation: Hands the verified update to the external updater or authenticated container queue.</returns>
    public async Task LaunchAsync(int processId, CancellationToken token)
    {
        if (!string.IsNullOrWhiteSpace(_options.DeploymentRoot))
        {
            ValidateConfiguration();
            var pending = PendingUpdate ?? throw new InvalidOperationException("No verified update is staged.");
            GXAgentContainerHealth.MarkDrained();
            var current = AgentRuntimeVersion.Resolve(AppContext.BaseDirectory, null,
                FileVersionInfo.GetVersionInfo(typeof(GXAgentUpdateService).Assembly.Location).ProductVersion) ?? "";
            await GXAgentContainerHealth.WriteAsync(_options.DeploymentRoot, current, false, token);
            ContainerRequestId = await GXAgentContainerUpdate.QueueAsync(_options.DeploymentRoot, _options.RequestKeyFile!, pending.Version, token);
            return;
        }
        var info = CreateStartInfo(processId);
        await StartProcess(info, token);
    }

    /// <summary>Starts the configured external updater and rejects unsuccessful immediate termination.</summary>
    /// <param name="info">Process start settings or transport description used by the operation.</param>
    /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
    /// <returns>A task representing this operation: Starts the configured external updater and rejects unsuccessful immediate termination.</returns>
    private static async Task StartProcessAsync(ProcessStartInfo info, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var process = Process.Start(info) ?? throw new InvalidOperationException("Could not start Gurux.Updater.");
        // Observe process exit independently; cancellation only limits the startup wait.
        var exit = process.WaitForExitAsync(CancellationToken.None);
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
