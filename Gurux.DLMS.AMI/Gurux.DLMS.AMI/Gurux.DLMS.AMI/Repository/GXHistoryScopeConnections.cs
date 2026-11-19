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
using System.Diagnostics.CodeAnalysis;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Non-owning provider connections and local transaction leases for nested repositories.</summary>
internal static class GXHistoryScopeConnections
{
    /// <summary>Returns the underlying command when the supplied command is a history lease.</summary>
    /// <param name="command">Database command that may be wrapped by a history lease.</param>
    /// <returns>The original database command beneath any history lease wrapper.</returns>
    internal static DbCommand UnwrapCommand(DbCommand command) => command is LeaseCommand lease ? lease.Inner : command;

    // Gurux ORM selects its SQL dialect from the provider connection's simple type name.
    /// <summary>Creates a non-owning history connection lease matching the underlying database provider.</summary>
    /// <param name="scope">Active history scope owning the underlying connection and transaction.</param>
    /// <returns>The provider-specific connection wrapper bound to the supplied scope.</returns>
    /// <exception cref="NotSupportedException">The underlying database provider has no supported history lease wrapper.</exception>
    internal static DbConnection Create(GXHistoryScope.Scope scope) => scope.Database.Connection.GetType().Name switch
    {
        var name when name.Equals("SqliteConnection", StringComparison.OrdinalIgnoreCase) => new SqliteConnection(scope),
        "SqlConnection" => new SqlConnection(scope),
        "MySqlConnection" => new MySqlConnection(scope),
        "NpgsqlConnection" => new NpgsqlConnection(scope),
        "OracleConnection" => new OracleConnection(scope),
        _ => throw new NotSupportedException("Unsupported history transaction database provider.")
    };

    /// <summary>Non-owning SQLite connection lease bound to the active history transaction.</summary>
    /// <param name="scope">Module or history scope that owns this non-owning adapter.</param>
    private sealed class SqliteConnection(GXHistoryScope.Scope scope) : LeaseConnection(scope);
    /// <summary>Non-owning SQL Server connection lease bound to the active history transaction.</summary>
    /// <param name="scope">Module or history scope that owns this non-owning adapter.</param>
    private sealed class SqlConnection(GXHistoryScope.Scope scope) : LeaseConnection(scope);
    /// <summary>Non-owning MySQL connection lease bound to the active history transaction.</summary>
    /// <param name="scope">Module or history scope that owns this non-owning adapter.</param>
    private sealed class MySqlConnection(GXHistoryScope.Scope scope) : LeaseConnection(scope);
    /// <summary>Non-owning PostgreSQL connection lease bound to the active history transaction.</summary>
    /// <param name="scope">Module or history scope that owns this non-owning adapter.</param>
    private sealed class NpgsqlConnection(GXHistoryScope.Scope scope) : LeaseConnection(scope);
    /// <summary>Non-owning Oracle connection lease bound to the active history transaction.</summary>
    /// <param name="scope">Module or history scope that owns this non-owning adapter.</param>
    private sealed class OracleConnection(GXHistoryScope.Scope scope) : LeaseConnection(scope);

    /// <summary>Borrows a history connection while preventing ownership transfer or transaction escape.</summary>
    /// <param name="scope">Module or history scope that owns this non-owning adapter.</param>
    private abstract class LeaseConnection(GXHistoryScope.Scope scope) : DbConnection
    {
        /// <summary>Indicates that disposal has begun and this instance can no longer accept work.</summary>
        private bool _disposed;
        /// <summary>Transaction wrappers created by this connection lease.</summary>
        private readonly List<LeaseTransaction> _transactions = [];
        /// <summary>Marks every unfinished nested transaction lease as having performed writes.</summary>
        internal void MarkWrites() {
            foreach (var lease in _transactions)
            {
                if (!lease.Completed)
                {
                    lease.HasWrites = true;
                }
            }
        }
        /// <summary>History scope owning the borrowed connection or transaction.</summary>
        internal GXHistoryScope.Scope Scope => scope;
        /// <summary>Underlying database object wrapped by the non-owning history lease.</summary>
        private DbConnection Inner => scope.Database.Connection;
        /// <summary>Rejects database use after the history connection lease or its scope has ended.</summary>
        internal void EnsureOpen()
        {
            if (_disposed || scope.Finished)
            {
                throw new ObjectDisposedException(nameof(LeaseConnection));
            }
        }
        /// <inheritdoc />
        [AllowNull] public override string ConnectionString { get => Inner.ConnectionString; set => throw new NotSupportedException(); }
        /// <inheritdoc />
        public override string Database => Inner.Database;
        /// <inheritdoc />
        public override string DataSource => Inner.DataSource;
        /// <inheritdoc />
        public override string ServerVersion => Inner.ServerVersion;
        /// <inheritdoc />
        public override int ConnectionTimeout => Inner.ConnectionTimeout;
        /// <inheritdoc />
        public override ConnectionState State => _disposed || scope.Finished ? ConnectionState.Closed : Inner.State;
        /// <inheritdoc />
        public override void Open() => EnsureOpen();
        /// <inheritdoc />
        public override Task OpenAsync(CancellationToken cancellationToken) { cancellationToken.ThrowIfCancellationRequested(); EnsureOpen(); return Task.CompletedTask; }
        /// <inheritdoc />
        public override void Close() => _disposed = true;
        /// <inheritdoc />
        public override Task CloseAsync() { Close(); return Task.CompletedTask; }
        /// <inheritdoc />
        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException("A history mutation cannot change databases.");
        /// <inheritdoc />
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) { EnsureOpen(); var transaction = new LeaseTransaction(this); _transactions.Add(transaction); return transaction; }
        /// <inheritdoc />
        protected override ValueTask<DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
        { cancellationToken.ThrowIfCancellationRequested(); return ValueTask.FromResult(BeginDbTransaction(isolationLevel)); }
        /// <inheritdoc />
        protected override DbCommand CreateDbCommand() { EnsureOpen(); return new LeaseCommand(this, Inner.CreateCommand()); }
        /// <inheritdoc />
        public override DataTable GetSchema() { EnsureOpen(); return Inner.GetSchema(); }
        /// <inheritdoc />
        public override DataTable GetSchema(string collectionName) { EnsureOpen(); return Inner.GetSchema(collectionName); }
        /// <inheritdoc />
        public override DataTable GetSchema(string collectionName, string?[] restrictionValues) { EnsureOpen(); return Inner.GetSchema(collectionName, restrictionValues); }
        /// <inheritdoc />
        protected override void Dispose(bool disposing) { if (disposing) { _disposed = true; } base.Dispose(disposing); }
        /// <inheritdoc />
        public override ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }

    /// <summary>Exposes the surrounding history transaction without allowing an independent commit.</summary>
    /// <param name="connection">Non-owning connection or module database connection used by this adapter.</param>
    private sealed class LeaseTransaction(LeaseConnection connection) : DbTransaction
    {
        /// <summary>Indicates that this operation has already completed.</summary>
        private bool _completed;
        /// <summary>Whether the transaction wrapper has already been completed.</summary>
        internal bool Completed => _completed;
        /// <summary>Whether the transaction has executed commands treated as mutations.</summary>
        internal bool HasWrites { get; set; }
        /// <summary>History scope owning the borrowed connection or transaction.</summary>
        internal GXHistoryScope.Scope Scope => connection.Scope;
        /// <inheritdoc />
        public override IsolationLevel IsolationLevel => Scope.Transaction.IsolationLevel;
        /// <inheritdoc />
        protected override DbConnection DbConnection => connection;
        /// <inheritdoc />
        public override void Commit()
        {
            connection.EnsureOpen();
            if (_completed)
            {
                throw new InvalidOperationException("The nested transaction has already completed.");
            }

            if (Scope.RollbackOnly)
            {
                throw new InvalidOperationException("The history transaction is rollback-only.");
            }

            _completed = true;
        }
        /// <inheritdoc />
        public override Task CommitAsync(CancellationToken cancellationToken = default)
        { cancellationToken.ThrowIfCancellationRequested(); Commit(); return Task.CompletedTask; }
        /// <inheritdoc />
        public override void Rollback() { Scope.RollbackOnly = true; _completed = true; }
        /// <inheritdoc />
        public override Task RollbackAsync(CancellationToken cancellationToken = default) { Rollback(); return Task.CompletedTask; }
        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_completed) { if (HasWrites) { Scope.RollbackOnly = true; } _completed = true; }
            base.Dispose(disposing);
        }
        /// <inheritdoc />
        public override ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }

    /// <summary>Delegates database commands while retaining history transaction and mutation tracking.</summary>
    /// <param name="connection">Non-owning connection or module database connection used by this adapter.</param>
    /// <param name="inner">Underlying provider command to execute inside the history transaction.</param>
    private sealed class LeaseCommand(LeaseConnection connection, DbCommand inner) : DbCommand
    {
        /// <summary>Underlying database object wrapped by the non-owning history lease.</summary>
        internal DbCommand Inner => inner;
        /// <summary>Transaction currently selected for commands executed through this lease.</summary>
        private DbTransaction? _selected;
        /// <inheritdoc />
        [AllowNull] public override string CommandText { get => inner.CommandText; set => inner.CommandText = value; }
        /// <inheritdoc />
        public override int CommandTimeout { get => inner.CommandTimeout; set => inner.CommandTimeout = value; }
        /// <inheritdoc />
        public override CommandType CommandType { get => inner.CommandType; set => inner.CommandType = value; }
        /// <inheritdoc />
        public override bool DesignTimeVisible { get => inner.DesignTimeVisible; set => inner.DesignTimeVisible = value; }
        /// <inheritdoc />
        public override UpdateRowSource UpdatedRowSource { get => inner.UpdatedRowSource; set => inner.UpdatedRowSource = value; }
        /// <inheritdoc />
        protected override DbConnection? DbConnection
        {
            get => connection;
            set {
                if (value != null && !ReferenceEquals(value, connection))
                {
                    throw new InvalidOperationException("Cannot move a history command to another connection.");
                }
            }
        }
        /// <inheritdoc />
        protected override DbTransaction? DbTransaction
        {
            get => _selected;
            set
            {
                if (value is LeaseTransaction { Completed: true })
                {
                    throw new InvalidOperationException("The nested transaction has already completed.");
                }

                if (value != null && !(value is LeaseTransaction lease && ReferenceEquals(lease.Scope, connection.Scope)) && !ReferenceEquals(value, connection.Scope.Transaction))
                {
                    throw new InvalidOperationException("Cannot enlist a history command in another transaction.");
                }

                _selected = value;
            }
        }
        /// <inheritdoc />
        protected override DbParameterCollection DbParameterCollection => inner.Parameters;
        /// <inheritdoc />
        protected override DbParameter CreateDbParameter() => inner.CreateParameter();
        /// <summary>Validates connection and nested transaction lifetime, tracks writes, and assigns the surrounding history transaction to the command.</summary>
        /// <param name="write">Whether executing the command should mark its nested transaction leases as having writes.</param>
        /// <exception cref="InvalidOperationException">The selected nested transaction has already completed.</exception>
        private void PrepareExecution(bool write = false)
        {
            connection.EnsureOpen();
            if (_selected is LeaseTransaction { Completed: true })
            {
                throw new InvalidOperationException("The nested transaction has already completed.");
            }

            if (write)
            {
                connection.MarkWrites(); if (_selected is LeaseTransaction lease)
                {
                    lease.HasWrites = true;
                }
            }
            inner.Transaction = connection.Scope.Transaction;
        }
        /// <summary>Determines whether the command is treated as a database mutation.</summary>
        /// <returns>True when executing the command is treated as a mutation.</returns>
        private bool WritesData()
        {
            var sql = inner.CommandText.AsSpan().TrimStart();
            return !(sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                sql.StartsWith("EXPLAIN", StringComparison.OrdinalIgnoreCase) ||
                sql.StartsWith("SHOW", StringComparison.OrdinalIgnoreCase) ||
                sql.StartsWith("DESCRIBE", StringComparison.OrdinalIgnoreCase));
        }
        /// <inheritdoc />
        public override void Cancel() => inner.Cancel();
        /// <inheritdoc />
        public override void Prepare() { PrepareExecution(); inner.Prepare(); }
        /// <inheritdoc />
        public override Task PrepareAsync(CancellationToken cancellationToken = default) { PrepareExecution(); return inner.PrepareAsync(cancellationToken); }
        /// <inheritdoc />
        public override int ExecuteNonQuery() { PrepareExecution(true); return inner.ExecuteNonQuery(); }
        /// <inheritdoc />
        public override object? ExecuteScalar() { PrepareExecution(WritesData()); return inner.ExecuteScalar(); }
        /// <inheritdoc />
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) { PrepareExecution(WritesData()); return inner.ExecuteReader(behavior); }
        /// <inheritdoc />
        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) { PrepareExecution(true); return inner.ExecuteNonQueryAsync(cancellationToken); }
        /// <inheritdoc />
        public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken) { PrepareExecution(WritesData()); return inner.ExecuteScalarAsync(cancellationToken); }
        /// <inheritdoc />
        protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        { PrepareExecution(WritesData()); return inner.ExecuteReaderAsync(behavior, cancellationToken); }
        /// <inheritdoc />
        protected override void Dispose(bool disposing) { if (disposing) { inner.Dispose(); } base.Dispose(disposing); }
        /// <inheritdoc />
        public override ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}
