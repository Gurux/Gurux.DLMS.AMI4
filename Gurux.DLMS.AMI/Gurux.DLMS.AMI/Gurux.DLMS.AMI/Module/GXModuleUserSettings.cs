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
using Gurux.DLMS.AMI.Services.Database;
using Gurux.DLMS.AMI.Server.Repository;

namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Module binding for repository-managed user data, including background operations.</summary>
public sealed class GXModuleUserSettings(string moduleId, IServiceProvider services) : IGXAmiModuleUserSettings
{
    private readonly IServiceScopeFactory scopes = services.GetRequiredService<IServiceScopeFactory>();

    /// <inheritdoc />
    public Task<T?> ReadAsync<T>(string userId, string name, CancellationToken cancellationToken = default)
        => ExecuteAsync(s => UserPropertiesRepository.ForModule(s).ReadAsync<T>(userId, name, cancellationToken), cancellationToken);
    /// <inheritdoc />
    public Task<IReadOnlyList<AmiModuleUserSetting<T>>> ListAsync<T>(string userId, CancellationToken cancellationToken = default)
        => ExecuteAsync(s => UserPropertiesRepository.ForModule(s).ListAsync<T>(userId, cancellationToken), cancellationToken);
    /// <inheritdoc />
    public Task UpsertAsync<T>(string userId, string name, T value, bool exclusiveToUser = false, CancellationToken cancellationToken = default)
        => ExecuteAsync(async s => { await UserPropertiesRepository.ForModule(s).UpsertAsync(userId, name, value, exclusiveToUser, cancellationToken); return true; }, cancellationToken);
    /// <inheritdoc />
    public Task DeleteIfUnchangedAsync(string userId, string name, string version, CancellationToken cancellationToken = default)
        => ExecuteAsync(async s => { await UserPropertiesRepository.ForModule(s).DeleteIfUnchangedAsync(userId, name, version, cancellationToken); return true; }, cancellationToken);
    /// <inheritdoc />
    public Task DeleteAsync(string userId, string name, CancellationToken cancellationToken = default)
        => ExecuteAsync(async s => { await UserPropertiesRepository.ForModule(s).DeleteAsync(userId, name, cancellationToken); return true; }, cancellationToken);

    private async Task<T> ExecuteAsync<T>(Func<GXModuleRepositoryScope, Task<T>> operation, CancellationToken token)
    {
        await using var services = scopes.CreateAsyncScope();
        using var suppression = services.ServiceProvider.GetService<IGXAmiSqlEvents>()?.Suppress();
        await using var scope = await GXModuleRepositoryScope.OpenAsync(moduleId, services.ServiceProvider, token);
        var result = await operation(scope);
        await scope.CommitAsync(token);
        return result;
    }
}
