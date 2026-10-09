//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General License for more details.
//
// This code is licensed under the GNU General License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Provides a database abstraction layer for CRUD operations, schema management,
    /// and transaction handling.
    /// Implementations hide provider-specific details so repository code can stay
    /// independent of the underlying database engine.
    /// Each instance uses one connection and must be disposed after its operation or scope.
    /// Transactions belong to that connection. Concurrent commands on the same instance are not supported.
    /// </summary>
    public interface IGXDatabase : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Creates and opens a new database connection.
        /// </summary>
        /// <returns>An open <see cref="DbConnection"/> instance.</returns>
        Task<DbConnection> CreateOpenConnectionAsync();

        /// <inheritdoc cref="IDbConnection.BeginTransaction()"/>
        IDbTransaction BeginTransaction();

        /// <inheritdoc cref="IDbConnection.BeginTransaction(IsolationLevel)"/>
        IDbTransaction BeginTransaction(IsolationLevel isolationLevel);

        /// <summary>
        /// Commits the specified transaction.
        /// </summary>
        /// <param name="transaction">Transaction to commit.</param>
        void CommitTransaction(IDbTransaction transaction);

        /// <summary>
        /// Rolls back the specified transaction.
        /// </summary>
        /// <param name="transaction">Transaction to roll back.</param>
        void RollbackTransaction(IDbTransaction transaction);

        /// <summary>
        /// Creates a table for the specified entity type.
        /// </summary>
        /// <typeparam name="T">Entity type mapped to a table.</typeparam>
        void CreateTable<T>();

        /// <summary>
        /// Updates the table schema for the specified entity type.
        /// </summary>
        /// <typeparam name="T">Entity type mapped to a table.</typeparam>
        void UpdateTable<T>();

        /// <summary>
        /// Updates the table schema for the specified runtime type.
        /// </summary>
        /// <param name="type">Entity type mapped to a table.</param>
        void UpdateTable(Type type);

        /// <summary>
        /// Drops the table for the specified entity type.
        /// </summary>
        /// <typeparam name="T">Entity type mapped to a table.</typeparam>
        void DropTable<T>();

        /// <summary>
        /// Drops the table for the specified runtime type.
        /// </summary>
        /// <param name="type">Entity type mapped to a table.</param>
        void DropTable(Type type);

        /// <summary>
        /// Drops the table for the specified entity type asynchronously.
        /// </summary>
        /// <typeparam name="T">Entity type mapped to a table.</typeparam>
        Task DropTableAsync<T>();

        /// <summary>
        /// Determines whether the table for the specified entity type exists.
        /// </summary>
        /// <typeparam name="T">Entity type mapped to a table.</typeparam>
        /// <returns><c>true</c> if the table exists; otherwise <c>false</c>.</returns>
        bool TableExist<T>();

        /// <summary>
        /// Determines whether the table for the specified runtime type exists.
        /// </summary>
        /// <param name="type">Entity type mapped to a table.</param>
        /// <returns><c>true</c> if the table exists; otherwise <c>false</c>.</returns>
        bool TableExist(Type type);

        /// <summary>
        /// Determines whether the table for the specified entity type contains any rows.
        /// </summary>
        /// <typeparam name="T">Entity type mapped to a table.</typeparam>
        /// <returns><c>true</c> if the table is empty; otherwise <c>false</c>.</returns>
        bool IsEmpty<T>();

        /// <summary>
        /// Deletes an entity from the database.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="value">Entity instance to delete.</param>
        void Delete<T>(T value);

        /// <summary>
        /// Deletes an entity from the database asynchronously.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="value">Entity instance to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeleteAsync<T>(T value, CancellationToken cancellationToken = default);


        /// <summary>
        /// Selects all rows for the specified entity type.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="transaction">Optional transaction to use for the query.</param>
        /// <returns>List of entities.</returns>
        List<T> SelectAll<T>(IDbTransaction? transaction = default);

        /// <summary>
        /// Selects all rows for the specified entity type asynchronously.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="transaction">Optional transaction to use for the query.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of entities.</returns>
        Task<List<T>> SelectAllAsync<T>(IDbTransaction? transaction = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Selects rows that match the specified filter and returns only selected columns.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="columns">Expression that defines selected columns.</param>
        /// <param name="where">Expression that defines filter conditions.</param>
        /// <returns>List of matching entities.</returns>
        List<T> Select<T>(Expression<Func<T, object>> columns, Expression<Func<T, bool>> where);

        /// <summary>
        /// Selects rows that match the specified filter using a transaction and returns selected columns.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="transaction">Transaction to use for the query.</param>
        /// <param name="columns">Expression that defines selected columns.</param>
        /// <param name="where">Expression that defines filter conditions.</param>
        /// <returns>List of matching entities.</returns>
        List<T> Select<T>(IDbTransaction? transaction, Expression<Func<T, object>> columns, Expression<Func<T, bool>> where);

        /// <summary>
        /// Selects rows that match the specified filter asynchronously and returns selected columns.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="columns">Expression that defines selected columns.</param>
        /// <param name="where">Expression that defines filter conditions.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of matching entities.</returns>
        Task<List<T>> SelectAsync<T>(Expression<Func<T, object>> columns, Expression<Func<T, bool>> where, CancellationToken cancellationToken = default);

        /// <summary>
        /// Selects rows that match the specified filter asynchronously using a transaction and returns selected columns.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="transaction">Transaction to use for the query.</param>
        /// <param name="columns">Expression that defines selected columns.</param>
        /// <param name="where">Expression that defines filter conditions.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of matching entities.</returns>
        Task<List<T>> SelectAsync<T>(IDbTransaction transaction, Expression<Func<T, object>> columns, Expression<Func<T, bool>> where, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a single entity that matches the filter, or the default value when no match is found.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="columns">Optional expression that defines selected columns.</param>
        /// <param name="where">Optional expression that defines filter conditions.</param>
        /// <returns>A matching entity or the default value of <typeparamref name="T"/>.</returns>
        T SingleOrDefault<T>(Expression<Func<T, object>>? columns = default,
            Expression<Func<T, bool>>? where = default);

        /// <summary>
        /// Returns a single entity that matches the filter using a transaction,
        /// or the default value when no match is found.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="transaction">Optional transaction to use for the query.</param>
        /// <param name="columns">Optional expression that defines selected columns.</param>
        /// <param name="where">Optional expression that defines filter conditions.</param>
        /// <returns>A matching entity or the default value of <typeparamref name="T"/>.</returns>
        T SingleOrDefault<T>(IDbTransaction? transaction,
            Expression<Func<T, object>>? columns = default,
            Expression<Func<T, bool>>? where = default);

        /// <summary>
        /// Returns a single entity that matches the filter asynchronously,
        /// or the default value when no match is found.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="columns">Optional expression that defines selected columns.</param>
        /// <param name="where">Optional expression that defines filter conditions.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A matching entity or the default value of <typeparamref name="T"/>.</returns>
        Task<T> SingleOrDefaultAsync<T>(Expression<Func<T, object>>? columns = default,
            Expression<Func<T, bool>>? where = default,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a single entity that matches the filter asynchronously using a transaction,
        /// or the default value when no match is found.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="transaction">Optional transaction to use for the query.</param>
        /// <param name="columns">Optional expression that defines selected columns.</param>
        /// <param name="where">Optional expression that defines filter conditions.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A matching entity or the default value of <typeparamref name="T"/>.</returns>
        Task<T> SingleOrDefaultAsync<T>(IDbTransaction? transaction,
        Expression<Func<T, object>>? columns = default,
        Expression<Func<T, bool>>? where = default,
        CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts a new entity into the database.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="value">Entity instance to insert.</param>
        void Insert<T>(T value);

        /// <summary>
        /// Inserts a new entity into the database using a transaction.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="transaction">Transaction to use for the insert operation.</param>
        /// <param name="value">Entity instance to insert.</param>
        void Insert<T>(IDbTransaction transaction, T value);

        /// <summary>
        /// Inserts a new entity into the database asynchronously.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="value">Entity instance to insert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task InsertAsync<T>(T value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts a new entity into the database asynchronously using a transaction.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="transaction">Transaction to use for the insert operation.</param>
        /// <param name="value">Entity instance to insert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task InsertAsync<T>(IDbTransaction transaction, T value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an entity in the database.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="value">Entity instance containing updated values.</param>
        /// <param name="columns">Optional expression that defines columns to update.</param>
        void Update<T>(T value, Expression<Func<T, object>>? columns = default);

        /// <summary>
        /// Updates an entity in the database using a transaction.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="transaction">Transaction to use for the update operation.</param>
        /// <param name="value">Entity instance containing updated values.</param>
        /// <param name="columns">Optional expression that defines columns to update.</param>
        void Update<T>(IDbTransaction transaction, T value, Expression<Func<T, object>>? columns = default);

        /// <summary>
        /// Updates an entity in the database asynchronously.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="value">Entity instance containing updated values.</param>
        /// <param name="columns">Optional expression that defines columns to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateAsync<T>(T value, Expression<Func<T, object>>? columns = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an entity in the database asynchronously using a transaction.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="transaction">Transaction to use for the update operation.</param>
        /// <param name="value">Entity instance containing updated values.</param>
        /// <param name="columns">Expression that defines columns to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateAsync<T>(IDbTransaction transaction, T value, Expression<Func<T, object>> columns, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all rows from the table mapped to the specified entity type.
        /// </summary>
        /// <typeparam name="T">Entity type mapped to a table.</typeparam>
        void Truncate<T>();

        /// <summary>Opens this database instance's connection and returns the same instance.</summary>
        Task<IGXDatabase> OpenAsync(CancellationToken cancellationToken = default);
        /// <summary>Returns the existing table's column names.</summary>
        IReadOnlyList<string> GetColumns<T>();
        /// <summary>Creates the entity table with explicit relationship and overwrite options.</summary>
        void CreateTable<T>(bool relations, bool overwrite);
        /// <summary>Creates the runtime entity type's table.</summary>
        void CreateTable(Type type, bool relations, bool overwrite);
        /// <summary>Updates the entity schema with explicit foreign-key and column-removal options.</summary>
        void UpdateTable<T>(bool updateForeignKeys, bool removeUnusedColumns);
        /// <summary>Updates the runtime entity type's schema.</summary>
        void UpdateTable(Type type, bool updateForeignKeys, bool removeUnusedColumns);
        /// <summary>Selects entities using an AMI query description.</summary>
        Task<List<T>> SelectAsync<T>(AmiSelectQuery<T> query, CancellationToken token = default);
        /// <summary>Selects entities within this connection's transaction.</summary>
        Task<List<T>> SelectAsync<T>(IDbTransaction? transaction, AmiSelectQuery<T> query, CancellationToken token = default);
        /// <summary>Returns the matching entity or its default value.</summary>
        Task<T> SingleOrDefaultAsync<T>(AmiSelectQuery<T> query, CancellationToken token = default);
        /// <summary>Returns the matching entity within a transaction or its default value.</summary>
        Task<T> SingleOrDefaultAsync<T>(IDbTransaction? transaction, AmiSelectQuery<T> query, CancellationToken token = default);
        /// <summary>Counts matching entities within a transaction.</summary>
        Task<int> CountAsync<T>(IDbTransaction? transaction, Expression<Func<T, bool>> where, CancellationToken token = default);
        /// <summary>Counts entities using the query filters, without ordering or paging.</summary>
        Task<int> CountAsync<T>(AmiSelectQuery<T> query, CancellationToken token = default);
        /// <summary>Inserts an entity with the query's column exclusions.</summary>
        Task InsertAsync<T>(AmiInsertQuery<T> query, CancellationToken token = default);
        /// <summary>Inserts an entity within a transaction.</summary>
        Task InsertAsync<T>(IDbTransaction? transaction, AmiInsertQuery<T> query, CancellationToken token = default);
        /// <summary>Updates selected columns and returns the affected row count.</summary>
        Task<int> UpdateAsync<T>(AmiUpdateQuery<T> query, CancellationToken token = default);
        /// <summary>Updates selected columns within a transaction and returns the affected row count.</summary>
        Task<int> UpdateAsync<T>(IDbTransaction? transaction, AmiUpdateQuery<T> query, CancellationToken token = default);
        /// <summary>Deletes the specified entity or entities matching the filter.</summary>
        Task DeleteAsync<T>(AmiDeleteQuery<T> query, CancellationToken token = default);
        /// <summary>Deletes matching entities within a transaction.</summary>
        Task DeleteAsync<T>(IDbTransaction? transaction, AmiDeleteQuery<T> query, CancellationToken token = default);
    }
}
