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
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Shared.DIs;
using System.Globalization;

namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Coordinates module repository operations without writing data or publishing notifications.</summary>
/// <remarks>Creates a module binding that remains valid after its original request ends.</remarks>
public sealed class GXModuleResources(string moduleId, IServiceProvider services) : IGXAmiModuleResources
{
    private readonly string moduleId = moduleId;
    private readonly IServiceScopeFactory scopes = services.GetRequiredService<IServiceScopeFactory>();

    /// <inheritdoc />
    public async Task<string> GetLocalizedTextAsync(CultureInfo cultureInfo, string name)
    {
        ArgumentNullException.ThrowIfNull(cultureInfo);
        ArgumentNullException.ThrowIfNull(name);
        await using var scope = scopes.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<ILocalizedResourceRepository>();
        var resource = await repository.ReadAsync(
            cultureInfo.TwoLetterISOLanguageName, ClientHelpers.GetHashCode(name), name);
        return resource?.Value ?? name;
    }

    /// <inheritdoc />
    public Task EnsureCaptureAsync(AmiModuleCapture definition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return ExecuteAsync(async repositories =>
        {
            var policy = await repositories.Policies.EnsureCaptureAsync(definition.Policy, cancellationToken);
            if (policy == null)
            {
                return false;
            }
            await repositories.LogTypes.EnsureAsync(policy, cancellationToken);
            if (definition.DefaultCategory != null)
            {
                await repositories.Enums.EnsureLogCategoryAsync(definition.DefaultCategory, cancellationToken);
            }
            await repositories.Menus.EnsureLogLinkAsync(definition, policy, cancellationToken);
            return true;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveLogMenuAsync(CancellationToken cancellationToken = default)
        => ExecuteAsync(async repositories =>
        {
            await repositories.Menus.RemoveLogLinkAsync(cancellationToken);
            return true;
        }, cancellationToken);

    /// <inheritdoc />
    public Task<int> AddLogsAsync(IReadOnlyList<AmiModuleLog> logs, IReadOnlyList<string> recipients, CancellationToken cancellationToken = default)
        => ExecuteAsync(repositories => repositories.Logs.AddAsync(logs, recipients, cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task RemoveLogsAsync(CancellationToken cancellationToken = default)
        => ExecuteAsync(async repositories =>
        {
            await repositories.Logs.RemoveAsync(cancellationToken);
            return true;
        }, cancellationToken);

    /// <inheritdoc />
    public Task EnsureNotificationDefaultsAsync(CancellationToken cancellationToken = default)
        => ExecuteAsync(async repositories =>
        {
            var type = await repositories.NotificationTypes.EnsureAsync(cancellationToken);
            await Internal.GXNotificationSourceStorage.RegisterAsync(repositories.Connection, repositories.Transaction,
                moduleId, moduleId, cancellationToken);
            await repositories.UserNotifications.EnsureAdministratorsAsync(type, cancellationToken);
            return true;
        }, cancellationToken);

    /// <inheritdoc />
    public Task RemoveNotificationDefaultsAsync(CancellationToken cancellationToken = default)
        => ExecuteAsync(async repositories =>
        {
            await repositories.NotificationTypes.RemoveAsync(cancellationToken);
            return true;
        }, cancellationToken);

    private async Task<T> ExecuteAsync<T>(Func<GXModuleRepositoryScope, Task<T>> operation, CancellationToken token)
    {
        await using var services = scopes.CreateAsyncScope();
        using var suppression = services.ServiceProvider.GetService<IGXAmiSqlEvents>()?.Suppress();
        await using var repositories = await GXModuleRepositoryScope.OpenAsync(moduleId, services.ServiceProvider, token);
        var result = await operation(repositories);
        await repositories.CommitAsync(token);
        return result;
    }
}
