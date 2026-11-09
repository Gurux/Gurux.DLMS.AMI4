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

using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Gurux.DLMS.AMI.Db;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Optional factory hook preserving connection diagnostics on non-owning history leases.</summary>
internal interface IGXHistoryConnectionDiagnostics
{
    void ConfigureHistoryLease(GXDbConnection db, bool notifySqlExcution);
}

/// <summary>Joins repository-local database transactions into one atomic history mutation.</summary>
internal static class GXHistoryScope
{
    private static readonly AsyncLocal<Scope?> Current = new();
    internal static bool IsActive => Current.Value is { Finished: false };
    internal static IDbConnectionFactory? CurrentFactory => Current.Value is { Finished: false } scope ? scope.Factory : null;
    internal static DbTransaction? CurrentTransaction => Current.Value is { Finished: false } scope ? scope.Transaction : null;
    internal static Guid ChangeSetId => Current.Value?.ChangeSetId ?? Guid.Empty;
    internal static DbCommand UnwrapCommand(DbCommand command) => GXHistoryScopeConnections.UnwrapCommand(command);

    internal sealed class Scope(IDbConnectionFactory factory, GXDbConnection database, DbTransaction transaction)
    {
        internal IDbConnectionFactory Factory { get; } = factory;
        internal GXDbConnection Database { get; } = database;
        internal DbTransaction Transaction { get; } = transaction;
        internal Guid ChangeSetId { get; } = Guid.NewGuid();
        internal bool RollbackOnly { get; set; }
        internal bool Finished { get; set; }
        internal List<Action> Committed { get; } = [];
        internal List<Action> RolledBack { get; } = [];
        internal GXDbConnection Lease(bool notifySqlExcution = true)
        {
            if (Finished)
            {
                throw new ObjectDisposedException(nameof(GXHistoryScope));
            }

            var lease = new GXDbConnection(GXHistoryScopeConnections.Create(this));
            if (Factory is IGXHistoryConnectionDiagnostics diagnostics)
            {
                diagnostics.ConfigureHistoryLease(lease, notifySqlExcution);
            }

            return lease;
        }
    }

    /// <summary>Returns a non-owning ORM lease on this factory's active mutation, if any.</summary>
    internal static GXDbConnection? TryLease(IDbConnectionFactory factory, bool notifySqlExcution = true)
    {
        var current = Current.Value;
        if (current == null)
        {
            return null;
        }

        if (!ReferenceEquals(current.Factory, factory))
        {
            throw new InvalidOperationException("A history mutation cannot enlist a different database factory.");
        }

        return current.Lease(notifySqlExcution);
    }

    /// <summary>Defers a notification until the outermost transaction has committed.</summary>
    internal static bool Defer(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (Current.Value is not { Finished: false } scope)
        {
            return false;
        }

        scope.Committed.Add(action);
        return true;
    }

    /// <summary>Registers compensation for provisional external work if the outer transaction rolls back.</summary>
    /// <remarks>Compensations run once in reverse registration order after database rollback. They do not run after commit.</remarks>
    internal static bool OnRollback(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (Current.Value is not { Finished: false } scope)
        {
            return false;
        }

        scope.RolledBack.Add(action);
        return true;
    }

    internal static Task RunAsync(IDbConnectionFactory factory, Func<GXDbConnection, Task> action,
        CancellationToken token = default)
        => RunAsync(factory, async connection => { await action(connection); return true; }, token);

    /// <summary>Runs a mutation once, committing entity and history changes together.</summary>
    internal static async Task<TResult> RunAsync<TResult>(IDbConnectionFactory factory,
        Func<GXDbConnection, Task<TResult>> action, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(action);
        token.ThrowIfCancellationRequested();
        var previous = Current.Value;
        if (previous != null)
        {
            try
            {
                await using var joined = TryLease(factory)!;
                var result = await action(joined);
                token.ThrowIfCancellationRequested();
                return result;
            }
            catch
            {
                previous.RollbackOnly = true;
                throw;
            }
        }
        var database = await factory.GetConnectionAsync(token);
        // GXDbConnection.DisposeAsync does not await provider disposal; own the real connection here.
        await using var physicalConnection = database.Connection;
        await using var transaction = await database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        var scope = new Scope(factory, database, transaction);
        Current.Value = scope;
        TResult value;
        try
        {
            await AcquireMutationLockAsync(physicalConnection, transaction, token);
            await using var lease = scope.Lease();
            value = await action(lease);
            token.ThrowIfCancellationRequested();
            if (scope.RollbackOnly)
            {
                throw new InvalidOperationException("The history mutation is rollback-only because a nested transaction failed.");
            }

            await transaction.CommitAsync(token);
            scope.Finished = true;
            scope.RolledBack.Clear();
        }
        catch
        {
            scope.Finished = true;
            scope.Committed.Clear();
            try
            { await transaction.RollbackAsync(CancellationToken.None); }
            catch (Exception rollbackError) { Trace.TraceError("History transaction rollback failed: {0}", rollbackError); }
            // Recovery actions run outside the failed scope and may open their own connections.
            Current.Value = previous;
            for (var index = scope.RolledBack.Count - 1; index >= 0; --index)
            {
                try { scope.RolledBack[index](); }
                catch (Exception error) { Trace.TraceError("History rollback compensation failed: {0}", error); }
            }
            scope.RolledBack.Clear();
            throw;
        }
        finally
        {
            Current.Value = previous;
        }
        // The database is committed. A delivery failure cannot turn it into a failed mutation.
        // Durable delivery users enqueue an outbox item inside the transaction before deferring dispatch.
        foreach (var notify in scope.Committed)
        {
            try
            { notify(); }
            catch (Exception error) { Trace.TraceError("History post-commit delivery failed: {0}", error); }
        }
        scope.Committed.Clear();
        return value;
    }

    private static async Task AcquireMutationLockAsync(DbConnection connection, DbTransaction transaction,
        CancellationToken token)
    {
        if (connection.GetType().Name != "SqlConnection")
        {
            return;
        }

        // Serializable snapshots take shared/range locks before the repository writes.
        // Serialize history writers BEFORE those reads to avoid lock conversion cycles,
        // including across server processes. Replaying a mutation is unsafe: repositories
        // can assign IDs to input objects and perform provisional external work.
        // Transaction ownership releases the lock automatically on commit or rollback.
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = N'Gurux.DLMS.AMI.HistoryMutation',
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 15000;
            SELECT @result;
            """;
        int result;
        try
        {
            result = Convert.ToInt32(await command.ExecuteScalarAsync(token));
        }
        catch (DbException error) when (token.IsCancellationRequested)
        {
            // SqlClient can report an attention during sp_getapplock as SqlException.
            throw new OperationCanceledException("History mutation lock wait was cancelled.", error, token);
        }
        token.ThrowIfCancellationRequested();
        if (result == -1)
        {
            throw new TimeoutException("Timed out waiting for another history mutation to finish.");
        }
        if (result < 0)
        {
            throw new InvalidOperationException($"Could not acquire the history mutation lock (SQL Server result {result}).");
        }
    }
}
