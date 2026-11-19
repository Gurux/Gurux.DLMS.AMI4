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
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Model;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Database operations sharing one connection for the lifetime of this instance.</summary>
/// <remarks>Use one instance per operation or scope. Concurrent commands on an instance are not supported.</remarks>
public sealed class GXDatabase : IGXDatabase
{
    /// <summary>Creates database connections used by this operation.</summary>
    private readonly IDbConnectionFactory? _factory;
    /// <summary>Determines whether disposal also closes the underlying database connection.</summary>
    private readonly bool _ownsConnection;
    /// <summary>Prevents overlapping connection initialization.</summary>
    private readonly SemaphoreSlim _connectionGate = new(1, 1);
    /// <summary>Database connection leased by the current operation.</summary>
    private GXDbConnection? _connection;
    /// <summary>Indicates that disposal has begun and this instance can no longer accept work.</summary>
    private bool _disposed;

    /// <summary>Initializes a new instance of <see cref="GXDatabase"/>.</summary>
    /// <param name="dbFactory">The db factory.</param>
    public GXDatabase(IDbConnectionFactory dbFactory)
    {
        _factory = dbFactory;
        _ownsConnection = true;
    }

    /// <summary>Wraps a borrowed database connection without taking responsibility for its lifetime.</summary>
    /// <param name="borrowedConnection">Existing connection whose lifetime remains owned by the caller.</param>
    private GXDatabase(DbConnection borrowedConnection)
    {
        _connection = new GXDbConnection(borrowedConnection, null);
        _ownsConnection = false;
    }

    /// <summary>Exposes an existing provider connection through the module database contract without owning its lifetime.</summary>
    /// <param name="connection">Database connection used for this query or persistence operation.</param>
    /// <returns>A database adapter borrowing the supplied connection.</returns>
    internal static IGXDatabase UseConnection(DbConnection connection) => new GXDatabase(connection);

    /// <summary>Lazily opens the database connection under a gate and verifies that any supplied transaction belongs to it.</summary>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <param name="transaction">Transaction shared by the query or mutation.</param>
    /// <returns>The open connection retained by this database adapter.</returns>
    /// <exception cref="ObjectDisposedException">The database adapter has been disposed.</exception>
    /// <exception cref="ArgumentException">The supplied transaction belongs to a different connection.</exception>
    [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "Preserves the established argument order used by repository and transaction callers.")]
    private async Task<GXDbConnection> GetConnectionAsync(CancellationToken token = default, IDbTransaction? transaction = null)
    {
        token.ThrowIfCancellationRequested();
        await _connectionGate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_connection == null)
            {
                var created = await _factory!.GetConnectionAsync(token).ConfigureAwait(false);
                try
                {
                    if (created.Connection.State != ConnectionState.Open)
                    {
                        await created.Connection.OpenAsync(token).ConfigureAwait(false);
                    }

                    _connection = created;
                }
                catch
                {
                    await created.DisposeAsync().ConfigureAwait(false);
                    throw;
                }
            }
            if (transaction != null && !ReferenceEquals(transaction.Connection, _connection.Connection))
            {
                throw new ArgumentException("The transaction must belong to this database connection.", nameof(transaction));
            }

            return _connection;
        }
        finally
        {
            _connectionGate.Release();
        }
    }

    /// <summary>Database connection borrowed by this transaction scope.</summary>
    [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "Preserves the established argument order used by repository and transaction callers.")]
    private GXDbConnection Connection => GetConnectionAsync().GetAwaiter().GetResult();
    /// <summary>Synchronously obtains the adapter connection and verifies the supplied transaction's connection identity.</summary>
    /// <param name="transaction">Transaction shared by the query or mutation.</param>
    /// <returns>The connection associated with the supplied transaction.</returns>
    [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "Preserves the established argument order used by repository and transaction callers.")]
    private GXDbConnection ConnectionFor(IDbTransaction? transaction) => GetConnectionAsync(default, transaction).GetAwaiter().GetResult();
    /// <summary>Schema operations using the current database connection.</summary>
    private GXSchemaManager Schema => new(Connection);
    /// <inheritdoc />
    public async Task<IGXDatabase> OpenAsync(CancellationToken cancellationToken = default)
    {
        await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        return this;
    }

    // Explicit raw connections remain caller-owned and independent of this instance's connection.
    /// <inheritdoc />
    public Task<DbConnection> CreateOpenConnectionAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _factory != null ? _factory.CreateOpenConnectionAsync() : throw new InvalidOperationException("A borrowed database cannot create another connection.");
    }

    /// <inheritdoc />
    public IDbTransaction BeginTransaction() => Connection.BeginTransaction();
    /// <inheritdoc />
    public IDbTransaction BeginTransaction(IsolationLevel isolationLevel) => Connection.BeginTransaction(isolationLevel);
    /// <inheritdoc />
    public void CommitTransaction(IDbTransaction transaction)
    {
        _ = ConnectionFor(transaction);
        transaction.Commit();
    }
    /// <inheritdoc />
    public void RollbackTransaction(IDbTransaction transaction)
    {
        _ = ConnectionFor(transaction);
        transaction.Rollback();
    }
    /// <inheritdoc />
    public void CreateTable<T>() => Schema.CreateTable<T>();
    /// <inheritdoc />
    public void CreateTable<T>(bool relations, bool overwrite) => Schema.CreateTable<T>(relations, overwrite);
    /// <inheritdoc />
    public void CreateTable(Type type, bool relations, bool overwrite) => Schema.CreateTable(type, relations, overwrite);
    /// <inheritdoc />
    public void UpdateTable<T>() => Schema.UpdateTable<T>();
    /// <inheritdoc />
    public void UpdateTable(Type type) => Schema.UpdateTable(type);
    /// <inheritdoc />
    public void UpdateTable<T>(bool updateForeignKeys, bool removeUnusedColumns) => Schema.UpdateTable(typeof(T), updateForeignKeys, removeUnusedColumns);
    /// <inheritdoc />
    public void UpdateTable(Type type, bool updateForeignKeys, bool removeUnusedColumns) => Schema.UpdateTable(type, updateForeignKeys, removeUnusedColumns);
    /// <inheritdoc />
    public void DropTable<T>() => Schema.DropTable<T>(false);
    /// <inheritdoc />
    public void DropTable(Type type) => Schema.DropTable(type, false);
    /// <inheritdoc />
    [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "Preserves the established argument order used by repository and transaction callers.")]
    public async Task DropTableAsync<T>() => new GXSchemaManager(await GetConnectionAsync().ConfigureAwait(false)).DropTable<T>(false);
    /// <inheritdoc />
    public bool TableExist<T>() => Schema.TableExist<T>();
    /// <inheritdoc />
    public bool TableExist(Type type) => Schema.TableExist(type);
    /// <inheritdoc />
    public IReadOnlyList<string> GetColumns<T>() => Schema.GetColumns<T>();
    /// <inheritdoc />
    public bool IsEmpty<T>() => Connection.IsEmpty<T>();
    /// <inheritdoc />
    public void Truncate<T>() => Connection.Truncate<T>();

    /// <inheritdoc />
    public void Delete<T>(T value) => Connection.Delete(GXDeleteArgs.Delete(value));
    /// <inheritdoc />
    public Task DeleteAsync<T>(T value, CancellationToken cancellationToken = default)
        => ExecuteAsync(c => c.DeleteAsync(GXDeleteArgs.Delete(value), cancellationToken), cancellationToken);
    /// <inheritdoc />
    public void Insert<T>(T value) => Connection.Insert(GXInsertArgs.Insert(value));
    /// <inheritdoc />
    public void Insert<T>(IDbTransaction transaction, T value) => ConnectionFor(transaction).Insert(transaction, GXInsertArgs.Insert(value));
    /// <inheritdoc />
    public Task InsertAsync<T>(T value, CancellationToken cancellationToken = default)
        => ExecuteAsync(c => c.InsertAsync(GXInsertArgs.Insert(value), cancellationToken), cancellationToken);
    /// <inheritdoc />
    public Task InsertAsync<T>(IDbTransaction transaction, T value, CancellationToken cancellationToken = default)
        => ExecuteAsync(c => c.InsertAsync(transaction, GXInsertArgs.Insert(value), cancellationToken), cancellationToken, transaction);
    /// <inheritdoc />
    public void Update<T>(T value, Expression<Func<T, object>>? columns = default) => Connection.Update(GXUpdateArgs.Update(value, columns));
    /// <inheritdoc />
    public void Update<T>(IDbTransaction transaction, T value, Expression<Func<T, object>>? columns = default)
        => ConnectionFor(transaction).Update(transaction, GXUpdateArgs.Update(value, columns));
    /// <inheritdoc />
    public Task UpdateAsync<T>(T value, Expression<Func<T, object>>? columns = default, CancellationToken cancellationToken = default)
        => ExecuteAsync(c => c.UpdateAsync(GXUpdateArgs.Update(value, columns), cancellationToken), cancellationToken);
    /// <inheritdoc />
    public Task UpdateAsync<T>(IDbTransaction transaction, T value, Expression<Func<T, object>> columns, CancellationToken cancellationToken = default)
        => ExecuteAsync(c => c.UpdateAsync(transaction, GXUpdateArgs.Update(value, columns), cancellationToken), cancellationToken, transaction);

    /// <inheritdoc />
    public List<T> Select<T>(Expression<Func<T, object>> columns, Expression<Func<T, bool>> where) => Connection.Select<T>(CreateSelectArgs(columns, where));
    /// <inheritdoc />
    public List<T> Select<T>(IDbTransaction? transaction, Expression<Func<T, object>> columns, Expression<Func<T, bool>> where)
        => ConnectionFor(transaction).Select<T>(transaction, CreateSelectArgs(columns, where));
    /// <inheritdoc />
    public List<T> SelectAll<T>(IDbTransaction? transaction = default) => ConnectionFor(transaction).SelectAll<T>(transaction);
    /// <inheritdoc />
    public Task<List<T>> SelectAllAsync<T>(IDbTransaction? transaction = default, CancellationToken cancellationToken = default)
        => ExecuteAsync(c => c.SelectAllAsync<T>(transaction, cancellationToken), cancellationToken, transaction);
    /// <inheritdoc />
    public Task<List<T>> SelectAsync<T>(Expression<Func<T, object>> columns, Expression<Func<T, bool>> where, CancellationToken cancellationToken = default)
        => ExecuteAsync(c => c.SelectAsync<T>(CreateSelectArgs(columns, where), cancellationToken), cancellationToken);
    /// <inheritdoc />
    public Task<List<T>> SelectAsync<T>(IDbTransaction transaction, Expression<Func<T, object>> columns, Expression<Func<T, bool>> where, CancellationToken cancellationToken = default)
        => ExecuteAsync(c => c.SelectAsync<T>(transaction, CreateSelectArgs(columns, where), cancellationToken), cancellationToken, transaction);
    /// <inheritdoc />
    public T SingleOrDefault<T>(Expression<Func<T, object>>? columns = default, Expression<Func<T, bool>>? where = default)
        => Connection.SingleOrDefault<T>(CreateSelectArgs(columns, where))!;
    /// <inheritdoc />
    public T SingleOrDefault<T>(IDbTransaction? transaction, Expression<Func<T, object>>? columns = default, Expression<Func<T, bool>>? where = default)
        => ConnectionFor(transaction).SingleOrDefault<T>(transaction!, CreateSelectArgs(columns, where))!;
    /// <inheritdoc />
    public Task<T> SingleOrDefaultAsync<T>(Expression<Func<T, object>>? columns = default, Expression<Func<T, bool>>? where = default, CancellationToken cancellationToken = default)
        => ExecuteAsync(c => c.SingleOrDefaultAsync<T>(CreateSelectArgs(columns, where), cancellationToken), cancellationToken)!;
    /// <inheritdoc />
    public Task<T> SingleOrDefaultAsync<T>(IDbTransaction? transaction, Expression<Func<T, object>>? columns = default, Expression<Func<T, bool>>? where = default, CancellationToken cancellationToken = default)
        => ExecuteAsync(c => c.SingleOrDefaultAsync<T>(transaction, CreateSelectArgs(columns, where), cancellationToken), cancellationToken, transaction)!;

    // A missing predicate selects all rows using the ORM overload without a predicate.
    /// <summary>Builds typed select arguments with optional columns and a predicate.</summary>
    /// <typeparam name="T">Entity type whose query expressions are constructed.</typeparam>
    /// <param name="columns">Typed expression selecting the query columns.</param>
    /// <param name="where">Optional predicate restricting the selected records.</param>
    /// <returns>ORM select arguments for the requested entity type.</returns>
    private static GXSelectArgs CreateSelectArgs<T>(Expression<Func<T, object>>? columns, Expression<Func<T, bool>>? where)
        => where == null ? GXSelectArgs.Select(columns) : GXSelectArgs.Select(columns, where);

    /// <summary>Obtains a validated database connection and invokes the supplied asynchronous operation on it.</summary>
    /// <typeparam name="TResult">Result returned by the supplied asynchronous operation.</typeparam>
    /// <param name="operation">Asynchronous operation executed using the validated connection or module repository scope.</param>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <param name="transaction">Transaction shared by the query or mutation.</param>
    /// <returns>The result produced by the database operation.</returns>
    private async Task<TResult> ExecuteAsync<TResult>(Func<GXDbConnection, Task<TResult>> operation, CancellationToken token, IDbTransaction? transaction = null)
        => await operation(await GetConnectionAsync(token, transaction).ConfigureAwait(false)).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<List<T>> SelectAsync<T>(GXAmiSelectQuery<T> query, CancellationToken token = default) => SelectAsync(null, query, token);
    /// <inheritdoc />
    public Task<List<T>> SelectAsync<T>(IDbTransaction? transaction, GXAmiSelectQuery<T> query, CancellationToken token = default)
        => ExecuteAsync(c => c.SelectAsync<T>(transaction, Select(query), token), token, transaction);
    /// <inheritdoc />
    public Task<T> SingleOrDefaultAsync<T>(GXAmiSelectQuery<T> query, CancellationToken token = default) => SingleOrDefaultAsync(null, query, token);
    /// <inheritdoc />
    public Task<T> SingleOrDefaultAsync<T>(IDbTransaction? transaction, GXAmiSelectQuery<T> query, CancellationToken token = default)
        => ExecuteAsync(c => c.SingleOrDefaultAsync<T>(transaction, Select(query), token), token, transaction)!;
    /// <inheritdoc />
    public Task<int> CountAsync<T>(IDbTransaction? transaction, Expression<Func<T, bool>> where, CancellationToken token = default)
        => ExecuteAsync(c => c.SingleOrDefaultAsync<int>(transaction, GXSelectArgs.Select<T>(x => GXSql.Count(1), where), token), token, transaction);
    /// <inheritdoc />
    public Task<int> CountAsync<T>(GXAmiSelectQuery<T> query, CancellationToken token = default)
        => ExecuteAsync(c => c.SingleOrDefaultAsync<int>(Select(query, true), token), token);
    /// <inheritdoc />
    public Task DeleteAsync<T>(Expression<Func<T, bool>> where, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(where);
        return ExecuteAsync(c => c.DeleteAsync(GXDeleteArgs.Delete(where), cancellationToken), cancellationToken);
    }
    /// <inheritdoc />
    public Task DeleteAsync<T>(IDbTransaction transaction, Expression<Func<T, bool>> where, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(where);
        return ExecuteAsync(c => c.DeleteAsync(transaction, GXDeleteArgs.Delete(where), cancellationToken), cancellationToken, transaction);
    }
    /// <inheritdoc />
    public void Dispose()
    {
        _connectionGate.Wait();
        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_ownsConnection)
            {
                _connection?.Dispose();
            }
        }
        finally
        {
            _connectionGate.Release();
        }
    }
    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _connectionGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_ownsConnection && _connection != null)
            {
                await _connection.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _connectionGate.Release();
        }
    }
    /// <summary>Applies a typed column-selection expression to generic query arguments.</summary>
    /// <typeparam name="T">Entity type whose query expressions are constructed.</typeparam>
    /// <param name="args">Query arguments receiving the typed selection.</param>
    /// <param name="columns">Typed expression selecting the query columns.</param>
    private static void AddColumns<T>(GXSelectArgs args, LambdaExpression? columns)
    {
        if (columns == null)
        {
            args.Columns.Add<T>();
        }
        else
        {
            args.Columns.Add((Expression<Func<T, object>>)columns);
        }
    }
    /// <summary>Applies a typed ordering expression to generic query arguments.</summary>
    /// <typeparam name="T">Entity type whose query expressions are constructed.</typeparam>
    /// <param name="args">Query arguments receiving the typed selection.</param>
    /// <param name="columns">Typed expression selecting the query columns.</param>
    private static void AddOrder<T>(GXSelectArgs args, LambdaExpression columns)
        => args.OrderBy.Add((Expression<Func<T, object>>)columns);
    /// <summary>Adds a typed filter expression to generic query arguments.</summary>
    /// <typeparam name="T">Entity type whose query expressions are constructed.</typeparam>
    /// <param name="args">Query arguments receiving the typed selection.</param>
    /// <param name="predicate">Typed filtering expression added to the query.</param>
    private static void AddWhere<T>(GXSelectArgs args, LambdaExpression predicate)
        => args.Where.And((Expression<Func<T, bool>>)predicate);
    /// <summary>Adds the selected typed inner or left join to query arguments.</summary>
    /// <typeparam name="TFrom">Entity type participating in the typed join.</typeparam>
    /// <typeparam name="TTo">Entity type participating in the typed join.</typeparam>
    /// <param name="args">Query arguments receiving the typed selection.</param>
    /// <param name="foreignKey">Expression selecting the foreign-key side of the join.</param>
    /// <param name="key">Property name used for lookup.</param>
    /// <param name="inner">Whether to create an inner join instead of a left join.</param>
    private static void AddJoin<TFrom, TTo>(GXSelectArgs args, LambdaExpression foreignKey, LambdaExpression key, bool inner)
    {
        if (inner)
        {
            args.Joins.AddInnerJoin((Expression<Func<TFrom, object>>)foreignKey, (Expression<Func<TTo, object>>)key);
        }
        else
        {
            args.Joins.AddLeftJoin((Expression<Func<TFrom, object>>)foreignKey, (Expression<Func<TTo, object>>)key);
        }
    }
    /// <summary>Cached reflection target used to add typed query column selections.</summary>
    private static readonly MethodInfo _columnsMethod = typeof(GXDatabase).GetMethod(nameof(AddColumns), BindingFlags.Static | BindingFlags.NonPublic)!;
    /// <summary>Cached reflection target used to add typed query joins.</summary>
    private static readonly MethodInfo _joinMethod = typeof(GXDatabase).GetMethod(nameof(AddJoin), BindingFlags.Static | BindingFlags.NonPublic)!;
    /// <summary>Cached reflection target used to add typed query predicates.</summary>
    private static readonly MethodInfo _whereMethod = typeof(GXDatabase).GetMethod(nameof(AddWhere), BindingFlags.Static | BindingFlags.NonPublic)!;
    /// <summary>Cached reflection target used to add typed query ordering.</summary>
    private static readonly MethodInfo _orderMethod = typeof(GXDatabase).GetMethod(nameof(AddOrder), BindingFlags.Static | BindingFlags.NonPublic)!;

    /// <summary>Builds ORM query arguments from the module-facing selection contract.</summary>
    /// <typeparam name="T">Entity type whose query expressions are constructed.</typeparam>
    /// <param name="query">Query whose filtering or selected columns are updated.</param>
    /// <param name="count">Whether to build a count query instead of selecting entity data.</param>
    /// <returns>ORM select or count arguments for the requested entity query.</returns>
    private static GXSelectArgs Select<T>(GXAmiSelectQuery<T> query, bool count = false)
    {
        var args = count ? CreateSelectArgs<T>(x => GXSql.Count(1), query.Predicate) : query.Projection == null ? (query.Predicate == null ? GXSelectArgs.SelectAll<T>() : GXSelectArgs.SelectAll(query.Predicate)) : CreateSelectArgs(query.Projection, query.Predicate);
        foreach (var join in query.Joins.Items)
        {
            _joinMethod.MakeGenericMethod(join.From, join.To).Invoke(null, [args, join.ForeignKey, join.Key, join.Inner]);
        }

        if (query.Where.ReplaceKeyFilter)
        {
            args.Where.Clear();
        }

        foreach (var predicate in query.Where.Predicates)
        {
            _whereMethod.MakeGenericMethod(predicate.Parameters[0].Type).Invoke(null, [args, predicate]);
        }

        if (!count)
        {
            foreach (var columns in query.Columns.Items)
            {
                _columnsMethod.MakeGenericMethod(columns.EntityType).Invoke(null, [args, columns.Expression]);
            }

            foreach (var columns in query.OrderBy.Items)
            {
                _orderMethod.MakeGenericMethod(columns.EntityType).Invoke(null, [args, columns.Expression]);
            }

            args.Index = query.Index;
            args.Count = query.Count;
        }
        return args;
    }
}
