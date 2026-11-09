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
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly List<Action> committed = [];
    private IDisposable? settingsLock;
    private GXModuleRepositoryScope(string moduleId, GXDbConnection connection, DbTransaction transaction, NotificationCache notifications)
    { ModuleId = moduleId; Connection = connection; Transaction = transaction; Notifications = notifications; }

    internal string ModuleId { get; }
    internal Guid ChangeSetId { get; } = GXHistoryScope.IsActive ? GXHistoryScope.ChangeSetId : Guid.NewGuid();
    internal GXDbConnection Connection { get; }
    internal DbTransaction Transaction { get; }
    internal NotificationCache Notifications { get; }
    internal IModulePolicyRepository Policies => PolicyRepository.ForModule(this);
    internal IModuleSettingsRepository Settings => ModuleRepository.ForModule(this);
    internal IModuleLogTypeRepository LogTypes => LogTypeRepository.ForModule(this);
    internal IModuleMenuRepository Menus => MenuRepository.ForModule(this);
    internal IModuleEnumTypeRepository Enums => EnumTypeRepository.ForModule(this);
    internal IModuleLogRepository Logs => LogRepository.ForModule(this);
    internal IModuleNotificationTypeRepository NotificationTypes => NotificationTypeRepository.ForModule(this);
    internal IModuleNotificationRepository UserNotifications => NotificationRepository.ForModule(this);

    internal static async Task<GXModuleRepositoryScope> OpenAsync(string moduleId, IServiceProvider services, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(moduleId) || moduleId.Length > 64 || !MyRegex().IsMatch(moduleId))
        {
            throw new ArgumentException("Invalid module identifier.");
        }
        var notifications = services.GetRequiredService<NotificationCache>();
        var factory = services.GetRequiredService<IDbConnectionFactory>();
        await Gate.WaitAsync(token);
        GXDbConnection? db = null;
        IDisposable? settingsLock = null;
        try
        {
            settingsLock = await AmiModuleSettingsStorage.LockAsync(moduleId, token);
            db = await factory.GetHistoryConnectionAsync(token, false);
            var transaction = await db.BeginTransactionAsync(token);
            return new(moduleId, db, transaction, notifications) { settingsLock = settingsLock };
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
                Gate.Release();
            }
            throw;
        }
    }

    internal void OnCommitted(Action notify) => committed.Add(notify);

    internal async Task CommitAsync(CancellationToken token)
    {
        await Transaction.CommitAsync(token);
        foreach (var notify in committed)
        {
            if (!GXHistoryScope.Defer(notify))
            {
                notify();
            }
        }

        committed.Clear();
    }

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
                settingsLock?.Dispose();
                Gate.Release();
            }
        }
    }

    [System.Text.RegularExpressions.GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
}
