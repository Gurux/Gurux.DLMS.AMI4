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
    internal static DbCommand UnwrapCommand(DbCommand command) => command is LeaseCommand lease ? lease.Inner : command;

    // Gurux ORM selects its SQL dialect from the provider connection's simple type name.
    internal static DbConnection Create(GXHistoryScope.Scope scope) => scope.Database.Connection.GetType().Name switch
    {
        var name when name.Equals("SqliteConnection", StringComparison.OrdinalIgnoreCase) => new SqliteConnection(scope),
        "SqlConnection" => new SqlConnection(scope),
        "MySqlConnection" => new MySqlConnection(scope),
        "NpgsqlConnection" => new NpgsqlConnection(scope),
        "OracleConnection" => new OracleConnection(scope),
        _ => throw new NotSupportedException("Unsupported history transaction database provider.")
    };

    private sealed class SqliteConnection(GXHistoryScope.Scope scope) : LeaseConnection(scope);
    private sealed class SqlConnection(GXHistoryScope.Scope scope) : LeaseConnection(scope);
    private sealed class MySqlConnection(GXHistoryScope.Scope scope) : LeaseConnection(scope);
    private sealed class NpgsqlConnection(GXHistoryScope.Scope scope) : LeaseConnection(scope);
    private sealed class OracleConnection(GXHistoryScope.Scope scope) : LeaseConnection(scope);

    private abstract class LeaseConnection(GXHistoryScope.Scope scope) : DbConnection
    {
        private bool disposed;
        private readonly List<LeaseTransaction> transactions = [];
        internal void MarkWrites() {
            foreach (var lease in transactions)
            {
                if (!lease.Completed)
                {
                    lease.HasWrites = true;
                }
            }
        }
        internal GXHistoryScope.Scope Scope => scope;
        private DbConnection Inner => scope.Database.Connection;
        internal void EnsureOpen()
        {
            if (disposed || scope.Finished)
            {
                throw new ObjectDisposedException(nameof(LeaseConnection));
            }
        }
        [AllowNull] public override string ConnectionString { get => Inner.ConnectionString; set => throw new NotSupportedException(); }
        public override string Database => Inner.Database;
        public override string DataSource => Inner.DataSource;
        public override string ServerVersion => Inner.ServerVersion;
        public override int ConnectionTimeout => Inner.ConnectionTimeout;
        public override ConnectionState State => disposed || scope.Finished ? ConnectionState.Closed : Inner.State;
        public override void Open() => EnsureOpen();
        public override Task OpenAsync(CancellationToken cancellationToken) { cancellationToken.ThrowIfCancellationRequested(); EnsureOpen(); return Task.CompletedTask; }
        public override void Close() => disposed = true;
        public override Task CloseAsync() { Close(); return Task.CompletedTask; }
        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException("A history mutation cannot change databases.");
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) { EnsureOpen(); var transaction = new LeaseTransaction(this); transactions.Add(transaction); return transaction; }
        protected override ValueTask<DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
        { cancellationToken.ThrowIfCancellationRequested(); return ValueTask.FromResult(BeginDbTransaction(isolationLevel)); }
        protected override DbCommand CreateDbCommand() { EnsureOpen(); return new LeaseCommand(this, Inner.CreateCommand()); }
        public override DataTable GetSchema() { EnsureOpen(); return Inner.GetSchema(); }
        public override DataTable GetSchema(string collectionName) { EnsureOpen(); return Inner.GetSchema(collectionName); }
        public override DataTable GetSchema(string collectionName, string?[] restrictionValues) { EnsureOpen(); return Inner.GetSchema(collectionName, restrictionValues); }
        protected override void Dispose(bool disposing) { if (disposing) { disposed = true; } base.Dispose(disposing); }
        public override ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }

    private sealed class LeaseTransaction(LeaseConnection connection) : DbTransaction
    {
        private bool completed;
        internal bool Completed => completed;
        internal bool HasWrites { get; set; }
        internal GXHistoryScope.Scope Scope => connection.Scope;
        public override IsolationLevel IsolationLevel => Scope.Transaction.IsolationLevel;
        protected override DbConnection DbConnection => connection;
        public override void Commit()
        {
            connection.EnsureOpen();
            if (completed)
            {
                throw new InvalidOperationException("The nested transaction has already completed.");
            }

            if (Scope.RollbackOnly)
            {
                throw new InvalidOperationException("The history transaction is rollback-only.");
            }

            completed = true;
        }
        public override Task CommitAsync(CancellationToken cancellationToken = default)
        { cancellationToken.ThrowIfCancellationRequested(); Commit(); return Task.CompletedTask; }
        public override void Rollback() { Scope.RollbackOnly = true; completed = true; }
        public override Task RollbackAsync(CancellationToken cancellationToken = default) { Rollback(); return Task.CompletedTask; }
        protected override void Dispose(bool disposing)
        {
            if (disposing && !completed) { if (HasWrites) { Scope.RollbackOnly = true; } completed = true; }
            base.Dispose(disposing);
        }
        public override ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }

    private sealed class LeaseCommand(LeaseConnection connection, DbCommand inner) : DbCommand
    {
        internal DbCommand Inner => inner;
        private DbTransaction? selected;
        [AllowNull] public override string CommandText { get => inner.CommandText; set => inner.CommandText = value; }
        public override int CommandTimeout { get => inner.CommandTimeout; set => inner.CommandTimeout = value; }
        public override CommandType CommandType { get => inner.CommandType; set => inner.CommandType = value; }
        public override bool DesignTimeVisible { get => inner.DesignTimeVisible; set => inner.DesignTimeVisible = value; }
        public override UpdateRowSource UpdatedRowSource { get => inner.UpdatedRowSource; set => inner.UpdatedRowSource = value; }
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
        protected override DbTransaction? DbTransaction
        {
            get => selected;
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

                selected = value;
            }
        }
        protected override DbParameterCollection DbParameterCollection => inner.Parameters;
        protected override DbParameter CreateDbParameter() => inner.CreateParameter();
        private void PrepareExecution(bool write = false)
        {
            connection.EnsureOpen();
            if (selected is LeaseTransaction { Completed: true })
            {
                throw new InvalidOperationException("The nested transaction has already completed.");
            }

            if (write)
            {
                connection.MarkWrites(); if (selected is LeaseTransaction lease)
                {
                    lease.HasWrites = true;
                }
            }
            inner.Transaction = connection.Scope.Transaction;
        }
        private bool WritesData()
        {
            var sql = inner.CommandText.AsSpan().TrimStart();
            return !(sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                sql.StartsWith("EXPLAIN", StringComparison.OrdinalIgnoreCase) ||
                sql.StartsWith("SHOW", StringComparison.OrdinalIgnoreCase) ||
                sql.StartsWith("DESCRIBE", StringComparison.OrdinalIgnoreCase));
        }
        public override void Cancel() => inner.Cancel();
        public override void Prepare() { PrepareExecution(); inner.Prepare(); }
        public override Task PrepareAsync(CancellationToken cancellationToken = default) { PrepareExecution(); return inner.PrepareAsync(cancellationToken); }
        public override int ExecuteNonQuery() { PrepareExecution(true); return inner.ExecuteNonQuery(); }
        public override object? ExecuteScalar() { PrepareExecution(WritesData()); return inner.ExecuteScalar(); }
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) { PrepareExecution(WritesData()); return inner.ExecuteReader(behavior); }
        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) { PrepareExecution(true); return inner.ExecuteNonQueryAsync(cancellationToken); }
        public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken) { PrepareExecution(WritesData()); return inner.ExecuteScalarAsync(cancellationToken); }
        protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        { PrepareExecution(WritesData()); return inner.ExecuteReaderAsync(behavior, cancellationToken); }
        protected override void Dispose(bool disposing) { if (disposing) { inner.Dispose(); } base.Dispose(disposing); }
        public override ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}
