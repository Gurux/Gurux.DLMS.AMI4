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

using Gurux.DLMS.AMI.Services.Database;
using Gurux.DLMS.AMI.Module.Enums;
using Gurux.DLMS.AMI.Shared.Enums;

using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Shared;
using Gurux.Service.Orm;
using Gurux.DLMS.AMI.Server.Internal;
namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Provides module data cleanup.</summary>
/// <param name="database">The database.</param>
public sealed class GXModuleDataCleanup(IGXDatabase database)
{
    static List<GXModuleTableOwnership> Select(List<GXModuleTableOwnership> owners, AmiModuleUninstallOptions options) => [.. owners.Where(o => options.Mode == ModuleDataRemoval.DeleteDataAndTables || o.Kind == AmiModuleTableKind.Data)];
    /// <summary>Performs the validate operation.</summary>
    /// <param name="moduleId">The module id.</param>
    /// <param name="options">The options.</param>
    /// <param name="token">Token used to cancel the operation.</param>
    public async Task ValidateAsync(string moduleId, AmiModuleUninstallOptions options, CancellationToken token)
    {
        options.Validate();
        if (options.Mode == ModuleDataRemoval.Preserve)
        {
            return;
        }

        await using var db = new GXDbConnection(await database.CreateOpenConnectionAsync(), null);
        var selected = Select(await GXModuleTableRegistry.ReadAsync(db, moduleId, token), options);
        if (selected.Count == 0)
        {
            return;
        }

        var graph = await ModuleTableDependencyReader.ReadAsync(db.Connection, null, token);
        ModuleTableCleanupPlan.Create(graph, selected, options.Mode == ModuleDataRemoval.DeleteDataAndTables);
    }
    /// <summary>Performs the execute operation.</summary>
    /// <param name="moduleId">The module id.</param>
    /// <param name="options">The options.</param>
    /// <param name="token">Token used to cancel the operation.</param>
    public async Task ExecuteAsync(string moduleId, AmiModuleUninstallOptions options, CancellationToken token)
    {
        options.Validate();
        if (options.Mode == ModuleDataRemoval.Preserve)
        {
            return;
        }

        await using var db = new GXDbConnection(await database.CreateOpenConnectionAsync(), null);
        var selected = Select(await GXModuleTableRegistry.ReadAsync(db, moduleId, token), options);
        // One transaction also makes SQLite/SQL Server table drops rollback on errors.
        using var transaction = db.Connection.BeginTransaction(System.Data.IsolationLevel.Serializable);
        await GXNotificationSourceStorage.DeleteModuleAsync(db, transaction, moduleId, token);
        if (selected.Count == 0)
        {
            transaction.Commit();
            return;
        }
        var graph = await ModuleTableDependencyReader.ReadAsync(db.Connection, transaction, token);
        var ordered = ModuleTableCleanupPlan.Create(graph, selected, options.Mode == ModuleDataRemoval.DeleteDataAndTables);
        foreach (var table in ordered)
        {
            using var command = db.Connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = (options.Mode == ModuleDataRemoval.DeleteData ? "DELETE FROM " : "DROP TABLE ") + ModuleTableDependencyReader.Quote(table, graph.SqlServer);
            await command.ExecuteNonQueryAsync(token);
        }
        if (options.Mode == ModuleDataRemoval.DeleteDataAndTables)
        {
            foreach (var owner in selected.Where(o => !o.Dropped))
            {
                owner.Dropped = true;
                await db.UpdateAsync(transaction, GXUpdateArgs.Update(owner, o => o.Dropped), token);
            }
        }

        transaction.Commit();
    }
}


