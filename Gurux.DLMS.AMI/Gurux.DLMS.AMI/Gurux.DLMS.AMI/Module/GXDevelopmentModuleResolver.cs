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
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Evaluates local project output metadata without running build or publish targets.</summary>
public sealed class GXDevelopmentModuleResolver(IHostEnvironment environment, IConfiguration configuration,
    ILogger<GXDevelopmentModuleResolver> logger)
{
    /// <summary>Gets or sets the options.</summary>
    public DevelopmentModuleOptions Options => configuration.GetSection("Modules:Development").Get<DevelopmentModuleOptions>() ?? new();

    /// <summary>Performs the resolve operation.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <param name="continueOnError">The continue on error.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "The existing parameter order is retained for source compatibility with modules and callers.")]
    public async Task<IReadOnlyList<ResolvedDevelopmentModule>> ResolveAsync(CancellationToken cancellationToken = default, bool continueOnError = false)
    {
        var options = Options;
        if (!options.Enabled)
        {
            return [];
        }

        if (!environment.IsDevelopment())
        {
            logger.LogWarning("Development module settings are ignored outside the Development environment.");
            return [];
        }
        var result = new List<ResolvedDevelopmentModule>();
        foreach (var source in options.Modules)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(source.Project) == string.IsNullOrWhiteSpace(source.Directory))
                {
                    throw new InvalidOperationException("Each development module must specify exactly one Project or Directory.");
                }

                if (!string.IsNullOrWhiteSpace(source.Directory))
                {
                    string directory = Absolute(source.Directory);
                    if (!Directory.Exists(directory))
                    {
                        throw new DirectoryNotFoundException($"Development module directory '{directory}' does not exist. Build the module and its project references first.");
                    }

                    if (source.AssemblyFile != null)
                    {
                        ValidateAssembly(directory, source.AssemblyFile, null);
                    }

                    result.Add(new(directory, source.AssemblyFile, source.Settings));
                    continue;
                }
                string project = Absolute(source.Project!);
                if (!File.Exists(project) || !project.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    throw new FileNotFoundException($"Development module project '{project}' does not exist or is not a .csproj file.", project);
                }

                string buildConfiguration = source.Configuration ?? options.Configuration;
                string? framework = source.TargetFramework ?? options.TargetFramework;
                string? rid = source.RuntimeIdentifier ?? options.RuntimeIdentifier;
                var properties = await EvaluateAsync(project, buildConfiguration, framework, rid, cancellationToken);
                if (string.IsNullOrWhiteSpace(framework))
                {
                    framework = properties.GetValueOrDefault("TargetFramework");
                    if (string.IsNullOrWhiteSpace(framework))
                    {
                        var frameworks = (properties.GetValueOrDefault("TargetFrameworks") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries);
                        if (frameworks.Length != 1)
                        {
                            throw new InvalidOperationException($"Project '{project}' targets multiple frameworks. Set Modules.Development.Modules[].TargetFramework explicitly ({string.Join(", ", frameworks)}).");
                        }

                        framework = frameworks[0];
                    }
                    properties = await EvaluateAsync(project, buildConfiguration, framework, rid, cancellationToken);
                }
                string? target = properties.GetValueOrDefault("TargetPath");
                if (string.IsNullOrWhiteSpace(target))
                {
                    throw new InvalidOperationException($"MSBuild did not return TargetPath for '{project}'.");
                }

                target = Path.GetFullPath(target, Path.GetDirectoryName(project)!);
                if (!File.Exists(target))
                {
                    throw new FileNotFoundException($"Development build output '{target}' is missing. Build '{project}' with Configuration={buildConfiguration}, TargetFramework={framework}, RuntimeIdentifier={rid ?? properties.GetValueOrDefault("RuntimeIdentifier") ?? "(default)"}, or run scripts/Start-AmiDevelopment.ps1 before starting AMI. The server does not compile modules.", target);
                }

                string outputDirectory = Path.GetDirectoryName(target)!;
                if (source.AssemblyFile != null)
                {
                    ValidateAssembly(outputDirectory, source.AssemblyFile, project);
                }

                result.Add(new(outputDirectory, source.AssemblyFile ?? Path.GetFileName(target), source.Settings));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception error) when (continueOnError)
            {
                logger.LogError(error, "Development module source {Source} was disabled for this run.", source.Project ?? source.Directory);
            }
        }
        return result;
    }

    private string Absolute(string path) => Path.GetFullPath(path, environment.ContentRootPath);
    private static void ValidateAssembly(string directory, string file, string? project)
    {
        string full = Path.GetFullPath(Path.Combine(directory, file));
        if (!full.StartsWith(directory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Development AssemblyFile must remain inside its build directory.");
        }

        if (!File.Exists(full))
        {
            throw new FileNotFoundException($"Development module assembly '{full}' is missing. Build {project ?? "the module project"} first.", full);
        }
    }

    private static async Task<Dictionary<string, string>> EvaluateAsync(string project, string configuration,
        string? framework, string? rid, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.GetDirectoryName(project)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        start.ArgumentList.Add("msbuild");
        start.ArgumentList.Add(project);
        start.ArgumentList.Add("-nologo");
        start.ArgumentList.Add("-getProperty:TargetPath,TargetFramework,TargetFrameworks,RuntimeIdentifier,Configuration");
        AddProperty("Configuration", configuration);
        if (!string.IsNullOrWhiteSpace(framework))
        {
            AddProperty("TargetFramework", framework);
        }

        if (!string.IsNullOrWhiteSpace(rid))
        {
            AddProperty("RuntimeIdentifier", rid);
        }

        void AddProperty(string name, string value)
        {
            if (value.IndexOfAny([';', '\r', '\n']) >= 0)
            {
                throw new InvalidOperationException($"Invalid development MSBuild property {name}.");
            }

            start.ArgumentList.Add("-p:" + name + "=" + value);
        }
        using var process = new Process { StartInfo = start };
        try
        {
            process.Start();
            var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
                throw;
            }
            string output = await stdout;
            string error = await stderr;
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Cannot evaluate development module project '{project}': {error}\n{output}");
            }

            int jsonStart = output.IndexOf('{');
            if (jsonStart < 0)
            {
                throw new InvalidOperationException($"MSBuild returned no property JSON for '{project}'. .NET SDK 8 or later is required.");
            }

            using var document = JsonDocument.Parse(output[jsonStart..]);
            return document.RootElement.GetProperty("Properties").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString() ?? "");
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            throw new InvalidOperationException($"Cannot evaluate '{project}': install a .NET SDK and make dotnet available on PATH. No compilation is performed by the loader.", exception);
        }
    }
}
