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

using Gurux.DLMS.AMI.Shared.DIs;
using System.Security.Claims;
using Gurux.DLMS.AMI.Http;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Module.Enums;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Module;

namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Prepares catalog packages and installs their required modules before the requested module.</summary>
internal sealed class GXModuleCatalogInstaller(IModuleRepository repository, IGXModuleService modules, HttpClient client,
    Func<GXModule, CancellationToken, Task> installed,
    Func<string, string, CancellationToken, GXModuleInstallCandidate>? inspect = null)
{
    private sealed record Package(GXModule Module, GXModuleVersion Release, string Zip, GXModuleInstallCandidate Candidate);

    public async Task<bool> InstallAsync(ClaimsPrincipal user, string id, Guid? versionId, CancellationToken cancellationToken)
    {
        string directory = Path.Combine(Path.GetTempPath(), "GuruxCatalogInstall", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var packages = new Dictionary<string, Package>(StringComparer.OrdinalIgnoreCase);
        try
        {
            async Task<Package> Download(GXModule module, GXModuleVersion release, CancellationToken token)
            {
                if (!Uri.TryCreate(release.Url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
                    {
                    throw new InvalidOperationException($"Module '{module.Id}' has an invalid package URL.");
                }
                string zip = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".zip");
                using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);
                await response.EnsureSuccessAsync(token);
                await using (var stream = File.Create(zip))
                    await response.Content.CopyToAsync(stream, token);
                var package = new Package(module, release, zip, (inspect ?? GXModuleService.InspectPackage)(zip, module.Id, token));
                packages[module.Id] = package;
                return package;
            }
            var rootModule = await repository.ReadAsync(id, null, cancellationToken);
            var rootRelease = versionId.HasValue ? rootModule.Versions?.SingleOrDefault(v => v.Id == versionId.Value) :
                GXModuleInstallPlanner.SelectVersion(rootModule.Versions ?? [], new(AmiModuleDependencyKind.Module, id), false);
            if (rootRelease == null || rootRelease.Enabled == false || string.IsNullOrWhiteSpace(rootRelease.Url) || string.IsNullOrWhiteSpace(rootRelease.FileName))
                {
                throw new InvalidOperationException($"Invalid version for module '{id}'.");
            }
            var root = await Download(rootModule, rootRelease, cancellationToken);
            var active = modules.GetManifests().Where(m => m.Enabled && m.State == AmiModuleState.Active &&
                GXReleaseVersionComparer.TryNormalize(m.CompatibilityVersion, out _))
                .ToDictionary(m => m.Id, m => m.CompatibilityVersion, StringComparer.OrdinalIgnoreCase);
            var plan = await GXModuleInstallPlanner.PlanAsync(root.Candidate, active, async (requirement, token) =>
            {
                // Repository reads enforce the installer's visibility and resource policies for every dependency.
                var module = await repository.ReadAsync(requirement.Id, null, token);
                if (GXModuleInstallPlanner.IsExplicitlyDisabled(module))
                    {
                    throw new AmiModuleDependencyException($"Required module '{module.Id}' is disabled. Enable it explicitly before installing '{id}'.");
                }
                var release = GXModuleInstallPlanner.SelectVersion(module.Versions ?? [], requirement, rootRelease.Prerelease);
                return release == null ? null : (await Download(module, release, token)).Candidate;
            }, cancellationToken);
            if (plan.Any(candidate => GXModuleInstallPlanner.IsInstalled(packages[candidate.Id].Module)))
            {
                await modules.QueueModuleUpdatesAsync([.. plan.Select(candidate =>
                {
                    var package = packages[candidate.Id];
                    return new GXModuleUpdatePackage(candidate.Id, package.Release.Number!, package.Zip,
                        package.Module.ReleaseVersion ?? package.Module.Version, package.Module.Name);
                })], cancellationToken);
                return true;
            }
            foreach (var candidate in plan)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var package = packages[candidate.Id];
                if (!string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    var current = await repository.ReadAsync(candidate.Id, null, cancellationToken);
                    if (GXModuleInstallPlanner.IsExplicitlyDisabled(current))
                        {
                        throw new AmiModuleDependencyException($"Required module '{candidate.Id}' is disabled. Enable it explicitly before installing '{id}'.");
                    }
                }
                try
                {
                    var item = (await modules.AddModuleAsync(user, package.Zip, cancellationToken, candidate.Id)).Single();
                    if (!string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase) && modules.GetManifest(candidate.Id)?.State != AmiModuleState.Active)
                        {
                        throw new AmiModuleDependencyException($"Installed module '{candidate.Id}' did not become active.");
                    }
                    item.AvailableVersion = package.Module.AvailableVersion;
                    item.ReleaseVersion = package.Release.Number;
                    await repository.UpsertAsync(item, null, cancellationToken);
                    await installed(item, cancellationToken);
                }
                catch (Exception error) when (error is not OperationCanceledException and not UnauthorizedAccessException
                    and not GXAmiForbiddenException and not GXAmiNotFoundException)
                {
                    throw new InvalidOperationException($"Installing module '{candidate.Id}' failed: {error.Message}", error);
                }
            }
            return false;
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}




