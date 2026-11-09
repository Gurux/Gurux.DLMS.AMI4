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

using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Db;

namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Provides module startup service.</summary>
/// <param name="modules">The modules.</param>
/// <param name="runtime">The runtime.</param>
/// <param name="dbFactory">The db factory.</param>
/// <param name="logger">Reports module startup failures.</param>
public sealed class GXModuleStartupService(IGXModuleService modules, GXModuleRuntime runtime, IDbConnectionFactory dbFactory, ILogger<GXModuleStartupService> logger) : IHostedService
{
    /// <summary>Performs the start operation.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using (var connection = await dbFactory.GetConnectionAsync(cancellationToken))
        {
            if (await ServerSettings.GetDefaultAdmin(connection) == null)
            {
                return;
            }
        }
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await modules.StartConfiguredModulesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to start configured modules.");
        }
    }
    /// <summary>Performs the stop operation.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    public Task StopAsync(CancellationToken cancellationToken) => runtime.StopAllAsync(cancellationToken);
}
