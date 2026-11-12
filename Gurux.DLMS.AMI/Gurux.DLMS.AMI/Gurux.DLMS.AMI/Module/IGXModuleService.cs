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

using Gurux.DLMS.AMI.Module.Enums;
using Gurux.DLMS.AMI.Client;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Module;

/// <summary>Module package management and asynchronous runtime lifecycle.</summary>
public interface IGXModuleService
{
    /// <summary>Performs the add module operation.</summary>
    /// <param name="user">The user.</param>
    /// <param name="compressedFile">The compressed file.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <param name="expectedId">The expected id.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "The existing parameter order is retained for source compatibility with modules and callers.")]
    Task<List<GXModule>> AddModuleAsync(ClaimsPrincipal? user, string compressedFile, CancellationToken cancellationToken = default, string? expectedId = null);
    /// <summary>Performs the enable module operation.</summary>
    /// <param name="user">The user.</param>
    /// <param name="module">The module.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task EnableModuleAsync(ClaimsPrincipal? user, GXModule module, CancellationToken cancellationToken = default);
    /// <summary>Performs the disable module operation.</summary>
    /// <param name="user">The user.</param>
    /// <param name="module">The module.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<AmiModuleState> DisableModuleAsync(ClaimsPrincipal? user, GXModule module, CancellationToken cancellationToken = default);
    /// <summary>Performs the update module settings operation.</summary>
    /// <param name="module">The module.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task UpdateModuleSettingsAsync(GXModule module, CancellationToken cancellationToken = default);
    /// <summary>Performs the delete module operation.</summary>
    /// <param name="module">The module.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <param name="uninstallOptions">The uninstall options.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "The existing parameter order is retained for source compatibility with modules and callers.")]
    Task DeleteModuleAsync(GXModule module, CancellationToken cancellationToken = default, AmiModuleUninstallOptions? uninstallOptions = null);
    /// <summary>Performs the execute operation.</summary>
    /// <param name="module">The module.</param>
    /// <param name="settings">The settings.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task ExecuteAsync(GXModule module, string? settings, CancellationToken cancellationToken = default);
    /// <summary>Performs the refresh operation.</summary>
    /// <param name="user">The user.</param>
    /// <param name="developmentPath">The development path.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<List<GXModule>> RefreshAsync(ClaimsPrincipal? user, string developmentPath, CancellationToken cancellationToken = default);
    /// <summary>Retains inspected module packages for application before the next startup.</summary>
    Task QueueModuleUpdatesAsync(IReadOnlyList<Gurux.DLMS.AMI.Server.Module.GXModuleUpdatePackage> packages, CancellationToken cancellationToken = default);
    /// <summary>Performs the start configured modules operation.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task StartConfiguredModulesAsync(CancellationToken cancellationToken = default);
    /// <summary>Performs the get removal order operation.</summary>
    /// <param name="ids">The ids.</param>
    IReadOnlyList<string> GetRemovalOrder(IEnumerable<string> ids);
    /// <summary>Performs the get manifests operation.</summary>
    IReadOnlyList<AmiModuleManifest> GetManifests();
    /// <summary>Performs the get manifest operation.</summary>
    /// <param name="id">The item identifier.</param>
    AmiModuleManifest? GetManifest(string id);
    /// <summary>Performs the read asset operation.</summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="contentId">The content id.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<byte[]> ReadAssetAsync(string id, string contentId, string fileName, CancellationToken cancellationToken = default);
}
