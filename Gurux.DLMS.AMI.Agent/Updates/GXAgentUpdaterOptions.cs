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

using Microsoft.Extensions.Configuration;

namespace Gurux.DLMS.AMI.Agent;

/// <summary>Configures authenticated release staging and external updater handoff.</summary>
internal sealed class GXAgentUpdaterOptions
{
    /// <summary>Gets or sets shared deployment queue storage for container installations.</summary>
    public string? DeploymentRoot { get; set; }
    /// <summary>Gets or sets the file containing the host deployment request authentication key.</summary>
    public string? RequestKeyFile { get; set; }
    /// <summary>Gets or sets the external updater executable or managed assembly path.</summary>
    public string Executable { get; set; } = "";
    /// <summary>Gets or sets the service restarted by the external updater.</summary>
    public string? ServiceName { get; set; }
    /// <summary>Gets the executable application path used for updater handoff.</summary>
    public string? ApplicationPath { get; set; }
    /// <summary>Gets or sets the endpoint used to verify application health after replacement.</summary>
    public string? HealthUrl { get; set; }
    /// <summary>Gets or sets the endpoint reporting the running application version.</summary>
    public string? VersionUrl { get; set; }
    /// <summary>Gets or sets the settings file passed to the restarted agent.</summary>
    public string SettingsPath { get; set; } = GXAgentSettingsStore.DefaultPath;
    /// <summary>Gets or sets the trusted public key for release manifest signature verification.</summary>
    public string PublicKeyPem { get; set; } = "";
    /// <summary>Gets or sets the maximum duration allowed for release downloads.</summary>
    public TimeSpan DownloadTimeout { get; set; } = TimeSpan.FromMinutes(10);
    /// <summary>Gets or sets the maximum allowed compressed release size.</summary>
    public long MaxPackageBytes { get; set; } = 512L * 1024 * 1024;
    /// <summary>Gets or sets the maximum allowed size of expanded package entries.</summary>
    public long MaxExpandedBytes { get; set; } = 2L * 1024 * 1024 * 1024;
    /// <summary>Gets or sets how long unused staged releases are retained.</summary>
    public TimeSpan StagingRetention { get; set; } = TimeSpan.FromDays(7);
    /// <summary>UTC ticks of the earliest permitted update retry.</summary>
    private long _retryAfterTicks;
    /// <summary>Gets the earliest time at which another application update may be attempted.</summary>
    internal DateTimeOffset RetryAfter
    {
        get => new(Interlocked.Read(ref _retryAfterTicks), TimeSpan.Zero);
        set => Interlocked.Exchange(ref _retryAfterTicks, value.UtcTicks);
    }
    /// <summary>Gets or sets the application directory replaced by the external updater.</summary>
    public string InstallationDirectory { get; set; } = AppContext.BaseDirectory;
    /// <summary>Gets or sets the private directory used to stage verified releases.</summary>
    public string StagingDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Gurux", "DLMS.AMI", "application-updates");

    /// <summary>Resolves updater settings from application configuration, environment and installation defaults.</summary>
    /// <param name="config">Application configuration containing updater settings.</param>
    /// <returns>Updater settings resolved from configuration, environment and installation defaults.</returns>
    internal static GXAgentUpdaterOptions Read(IConfiguration config) => new()
    {
        DeploymentRoot = config["Updater:DeploymentRoot"],
        RequestKeyFile = config["Updater:RequestKeyFile"],
        Executable = ResolveExecutable(config),
        ServiceName = config["Updater:ServiceName"],
        ApplicationPath = config["Updater:ApplicationPath"],
        HealthUrl = config["Updater:HealthUrl"],
        VersionUrl = config["Updater:VersionUrl"],
        PublicKeyPem = ReadPublicKey(config),
        DownloadTimeout = TimeSpan.Parse(config["Updater:DownloadTimeout"] ?? "00:10:00", System.Globalization.CultureInfo.InvariantCulture),
        MaxPackageBytes = long.Parse(config["Updater:MaxPackageBytes"] ?? "536870912", System.Globalization.CultureInfo.InvariantCulture),
        MaxExpandedBytes = long.Parse(config["Updater:MaxExpandedBytes"] ?? "2147483648", System.Globalization.CultureInfo.InvariantCulture),
        StagingRetention = TimeSpan.Parse(config["Updater:StagingRetention"] ?? "7.00:00:00", System.Globalization.CultureInfo.InvariantCulture),
        SettingsPath = Path.GetFullPath(config["Agent:SettingsPath"] ?? Environment.GetEnvironmentVariable("GURUX_AGENT_SETTINGS") ?? GXAgentSettingsStore.DefaultPath),
        StagingDirectory = config["Updater:StagingDirectory"] ?? new GXAgentUpdaterOptions().StagingDirectory
    };

    /// <summary>Resolves the configured updater path with environment and bundled installation fallbacks.</summary>
    /// <param name="config">Application configuration containing updater settings.</param>
    /// <returns>The configured or bundled external updater path.</returns>
    internal static string ResolveExecutable(IConfiguration config)
    {
        var configured = config["Updater:Executable"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }
        configured = Environment.GetEnvironmentVariable("GURUX_UPDATER_PATH");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "gurux-updater", "Gurux.Updater.Tool.dll"));
    }

    /// <summary>Loads the configured release verification key or the embedded trusted key.</summary>
    /// <param name="config">Application configuration containing updater settings.</param>
    /// <returns>The configured or embedded trusted release verification key in PEM format.</returns>
    private static string ReadPublicKey(IConfiguration config)
    {
        var configured = config["Updater:PublicKeyPem"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }
        using var stream = typeof(GXAgentUpdaterOptions).Assembly.GetManifestResourceStream("Gurux.DLMS.AMI.Agent.agent-release-public.pem")
            ?? throw new InvalidOperationException("The bundled agent release public key is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
