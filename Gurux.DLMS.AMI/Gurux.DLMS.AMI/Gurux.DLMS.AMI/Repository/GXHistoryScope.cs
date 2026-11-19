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

/// <summary>Joins repository-local database transactions into one atomic history mutation.</summary>
internal static class GXHistoryScope
{
    /// <summary>Async-local operation state restored when the enclosing operation finishes.</summary>
    private static readonly AsyncLocal<Scope?> _current = new();
    /// <summary>Whether the async-local history scope exists and has not finished.</summary>
    internal static bool IsActive => _current.Value is { Finished: false };
    /// <summary>Database factory belonging to the active history scope, if one exists.</summary>
    internal static IDbConnectionFactory? CurrentFactory => _current.Value is { Finished: false } scope ? scope.Factory : null;
    /// <summary>Transaction belonging to the active history scope, if one exists.</summary>
    internal static DbTransaction? CurrentTransaction => _current.Value is { Finished: false } scope ? scope.Transaction : null;
    /// <summary>Identifier grouping changes made in this transaction or history scope.</summary>
    internal static Guid ChangeSetId => _current.Value?.ChangeSetId ?? Guid.Empty;
    /// <summary>Returns the underlying command when the supplied command is a history lease.</summary>
    /// <param name="command">Database command that may be wrapped by a history lease.</param>
    /// <returns>The original database command beneath any history lease wrapper.</returns>
    internal static DbCommand UnwrapCommand(DbCommand command) => GXHistoryScopeConnections.UnwrapCommand(command);

    /// <summary>Retains one history transaction, its change identifier, and deferred completion callbacks.</summary>
    /// <param name="factory">Factory that created the history database connection.</param>
    /// <param name="database">Connection shared by every mutation in the history scope.</param>
    /// <param name="transaction">Transaction shared by every mutation in the history scope.</param>
    internal sealed class Scope(IDbConnectionFactory factory, GXDbConnection database, DbTransaction transaction)
    {
        /// <summary>Database connection factory associated with the active history scope.</summary>
        internal IDbConnectionFactory Factory { get; } = factory;
        /// <summary>Database wrapper shared by the active history scope.</summary>
        internal GXDbConnection Database { get; } = database;
        /// <summary>Database transaction governing changes in this scope.</summary>
        internal DbTransaction Transaction { get; } = transaction;
        /// <summary>Identifier grouping changes made in this transaction or history scope.</summary>
        internal Guid ChangeSetId { get; } = Guid.NewGuid();
        /// <summary>Whether the surrounding transaction must roll back instead of committing.</summary>
        internal bool RollbackOnly { get; set; }
        /// <summary>Whether the history scope has finished and rejects further operations.</summary>
        internal bool Finished { get; set; }
        /// <summary>Callbacks to execute after the transaction commits successfully.</summary>
        internal List<Action> Committed { get; } = [];
        /// <summary>Callbacks to execute after the transaction rolls back.</summary>
        internal List<Action> RolledBack { get; } = [];
        /// <summary>Creates a provider-specific non-owning connection lease and preserves the factory's SQL diagnostic settings.</summary>
        /// <param name="notifySqlExcution">Whether the borrowed connection should retain SQL execution notifications.</param>
        /// <returns>A connection wrapper bound to this unfinished history scope.</returns>
        /// <exception cref="ObjectDisposedException">The history scope has already finished.</exception>
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
        var current = _current.Value;
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
        if (_current.Value is not { Finished: false } scope)
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
        if (_current.Value is not { Finished: false } scope)
        {
            return false;
        }

        scope.RolledBack.Add(action);
        return true;
    }

    /// <summary>Runs an asynchronous action using a connection bound to the shared history transaction.</summary>
    /// <param name="factory">Creates or leases the database connection shared by the history transaction.</param>
    /// <param name="action">Callback executed inside the protected operation or transaction scope.</param>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>A task that completes after the action and transaction scope finish.</returns>
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
        var previous = _current.Value;
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
        _current.Value = scope;
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
            _current.Value = previous;
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
            _current.Value = previous;
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

    /// <summary>Acquires a SQL Server transaction-owned application lock before history snapshot reads to serialize writers across processes.</summary>
    /// <param name="connection">Database connection used for this query or persistence operation.</param>
    /// <param name="transaction">Transaction shared by the query or mutation.</param>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>A task that completes after locking, or immediately for providers that do not use the SQL Server lock.</returns>
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
