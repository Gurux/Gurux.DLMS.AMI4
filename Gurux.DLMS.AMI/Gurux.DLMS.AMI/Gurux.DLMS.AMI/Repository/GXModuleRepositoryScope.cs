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

using System.Data.Common;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Module;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>One trusted module repository transaction and its deferred notifications.</summary>
internal sealed partial class GXModuleRepositoryScope : IAsyncDisposable
{
    // Category identifiers are global, so different modules must share this gate.
    /// <summary>Serializes operations that mutate shared state in this component.</summary>
    private static readonly SemaphoreSlim _gate = new(1, 1);
    /// <summary>Callbacks deferred until the surrounding transaction commits.</summary>
    private readonly List<Action> _committed = [];
    /// <summary>Held settings lease released after normalization and persistence finish.</summary>
    private IDisposable? _settingsLock;
    /// <summary>Retains module identity, database transaction, and notifications for atomic repository operations.</summary>
    /// <param name="moduleId">Stable identity restricting the module-owned repository operations.</param>
    /// <param name="connection">Non-owning connection or module database connection used by this adapter.</param>
    /// <param name="transaction">Transaction shared by every mutation in the history scope.</param>
    /// <param name="notifications">Publishes change notifications after successful persistence.</param>
    private GXModuleRepositoryScope(string moduleId, GXDbConnection connection, DbTransaction transaction, NotificationCache notifications)
    { ModuleId = moduleId; Connection = connection; Transaction = transaction; Notifications = notifications; }

    /// <summary>Identifier of the module that owns this repository scope.</summary>
    internal string ModuleId { get; }
    /// <summary>Identifier grouping changes made in this transaction or history scope.</summary>
    internal Guid ChangeSetId { get; } = GXHistoryScope.IsActive ? GXHistoryScope.ChangeSetId : Guid.NewGuid();
    /// <summary>Database connection borrowed by this transaction scope.</summary>
    internal GXDbConnection Connection { get; }
    /// <summary>Database transaction governing changes in this scope.</summary>
    internal DbTransaction Transaction { get; }
    /// <summary>Notification cache used after a successful commit.</summary>
    internal NotificationCache Notifications { get; }
    /// <summary>Policy operations restricted to the owning module.</summary>
    internal IModulePolicyRepository Policies => PolicyRepository.ForModule(this);
    /// <summary>Settings operations restricted to the owning module.</summary>
    internal IModuleSettingsRepository Settings => ModuleRepository.ForModule(this);
    /// <summary>Log-type operations restricted to the owning module.</summary>
    internal IModuleLogTypeRepository LogTypes => LogTypeRepository.ForModule(this);
    /// <summary>Menu operations restricted to the owning module.</summary>
    internal IModuleMenuRepository Menus => MenuRepository.ForModule(this);
    /// <summary>Enumeration-type operations restricted to the owning module.</summary>
    internal IModuleEnumTypeRepository Enums => EnumTypeRepository.ForModule(this);
    /// <summary>Log operations restricted to the owning module.</summary>
    internal IModuleLogRepository Logs => LogRepository.ForModule(this);
    /// <summary>Notification-type operations restricted to the owning module.</summary>
    internal IModuleNotificationTypeRepository NotificationTypes => NotificationTypeRepository.ForModule(this);
    /// <summary>User notification operations restricted to the owning module.</summary>
    internal IModuleNotificationRepository UserNotifications => NotificationRepository.ForModule(this);

    /// <summary>Validates module identity, acquires settings locks, and opens a module-scoped history transaction.</summary>
    /// <param name="moduleId">Stable identity of the module owning the stored records.</param>
    /// <param name="services">Optional request-scoped services used for authorized aggregate reads and identity enlistment.</param>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>The repository scope owning the acquired transaction and synchronization leases.</returns>
    /// <exception cref="ArgumentException">The module identity is empty, oversized, or contains unsupported characters.</exception>
    internal static async Task<GXModuleRepositoryScope> OpenAsync(string moduleId, IServiceProvider services, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(moduleId) || moduleId.Length > 64 || !MyRegex().IsMatch(moduleId))
        {
            throw new ArgumentException("Invalid module identifier.");
        }
        var notifications = services.GetRequiredService<NotificationCache>();
        var factory = services.GetRequiredService<IDbConnectionFactory>();
        await _gate.WaitAsync(token);
        GXDbConnection? db = null;
        IDisposable? settingsLock = null;
        try
        {
            settingsLock = await AmiModuleSettingsStorage.LockAsync(moduleId, token);
            db = await factory.GetHistoryConnectionAsync(token, false);
            var transaction = await db.BeginTransactionAsync(token);
            return new(moduleId, db, transaction, notifications) { _settingsLock = settingsLock };
        }
        catch
        {
            try
            {
                if (db != null)
                {
                    await db.DisposeAsync();
                }
            }
            finally
            {
                settingsLock?.Dispose();
                _gate.Release();
            }
            throw;
        }
    }

    /// <summary>Defers a notification until the surrounding database transaction commits.</summary>
    /// <param name="notify">Callback deferred until the transaction commits.</param>
    internal void OnCommitted(Action notify) => _committed.Add(notify);

    /// <summary>Commits module repository writes and publishes notifications immediately or defers them to the enclosing history transaction.</summary>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>A task that completes after the commit and notification scheduling.</returns>
    internal async Task CommitAsync(CancellationToken token)
    {
        await Transaction.CommitAsync(token);
        foreach (var notify in _committed)
        {
            if (!GXHistoryScope.Defer(notify))
            {
                notify();
            }
        }

        _committed.Clear();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            await Transaction.DisposeAsync();
        }
        finally
        {
            try
            {
                await Connection.DisposeAsync();
            }
            finally
            {
                _settingsLock?.Dispose();
                _gate.Release();
            }
        }
    }

    /// <summary>Matches allowed module identifiers used to create repository transaction scopes.</summary>
    /// <returns>The generated module identifier validation expression.</returns>
    [System.Text.RegularExpressions.GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
}
