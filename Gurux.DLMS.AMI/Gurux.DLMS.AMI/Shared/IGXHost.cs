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
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Shared
{
    public interface IGXDatabase
    {
        /// <inheritdoc cref="IDbConnection.BeginTransaction()"/>
        IDbTransaction BeginTransaction();

        /// <inheritdoc cref="IDbConnection.BeginTransaction(IsolationLevel)"/>
        IDbTransaction BeginTransaction(IsolationLevel isolationLevel);

        /// <summary>
        /// Accept transaction.
        /// </summary>
        /// <param name="transaction"></param>
        void CommitTransaction(IDbTransaction transaction);

        /// <summary>
        /// Rollback transaction.
        /// </summary>
        /// <param name="transaction"></param>
        void RollbackTransaction(IDbTransaction transaction);

        /// <summary>
        /// Create new table.
        /// </summary>
        /// <typeparam name="T">Table class.</typeparam>
        void CreateTable<T>();

        /// <summary>
        /// Update table columns..
        /// </summary>
        /// <typeparam name="T">Table class.</typeparam>
        void UpdateTable<T>();

        /// <summary>
        /// Update table.
        /// </summary>
        /// <param name="type">Table type.</param>
        void UpdateTable(Type type);

        /// <summary>
        /// Drop selected table.
        /// </summary>
        /// <typeparam name="T">Table type to drop.</typeparam>
        void DropTable<T>();

        /// <summary>
        /// Drop selected table.
        /// </summary>
        /// <param name="type">Table type.</param>
        void DropTable(Type type);

        /// <summary>
        /// Check is table created.
        /// </summary>
        /// <typeparam name="T">Table type.</typeparam>
        /// <returns>True, if table is created.</returns>
        bool TableExist<T>();

        /// <summary>
        /// Check is table created.
        /// </summary>
        /// <param name="type">Table type.</param>
        /// <returns>True, if table is created.</returns>
        bool TableExist(Type type);

        /// <summary>
        /// Check is table empty.
        /// </summary>
        /// <returns>True, if table is empty.</returns>
        bool IsEmpty<T>();

        /// <summary>
        /// Delete value from the database.
        /// </summary>
        /// <typeparam name="T">Table class.</typeparam>
        /// <param name="value">Values to delete.</param>
        void Delete<T>(T value);

        /// <summary>
        /// Delete value from the database.
        /// </summary>
        /// <typeparam name="T">Table class.</typeparam>
        /// <param name="value">Values to delete.</param>
        Task DeleteAsync<T>(T value);

        /// <summary>
        /// Delete value from the database.
        /// </summary>
        /// <typeparam name="T">Table class.</typeparam>
        /// <param name="value">Values to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeleteAsync<T>(T value, CancellationToken cancellationToken);

        /// <summary>
        /// Select item by Id.
        /// </summary>
        /// <typeparam name="T">Table class.</typeparam>
        /// <param name="id">Item's ID.</param>
        T SelectById<T>(object id);

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T">Table class.</typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        T SelectById<T>(object id, Expression<Func<T, object>> columns);

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T">Table class.</typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<T> SelectByIdAsync<T>(object id, CancellationToken cancellationToken);

        /// <summary>
        /// Select item's columns by ID.
        /// </summary>
        /// <typeparam name="T">Table class.</typeparam>
        /// <param name="id">Item's ID.</param>
        /// <param name="columns">Selected columns.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<T> SelectByIdAsync<T>(object id, Expression<Func<T, object>> columns, CancellationToken cancellationToken);


        /// <summary>
        /// Select all rows from the database table.
        /// </summary>
        /// <typeparam name="T">Table class.</typeparam>
        /// <returns>Table rows.</returns>
        List<T> SelectAll<T>();

        /// <summary>
        /// Select all rows from the database table.
        /// </summary>
        /// <typeparam name="T">Table class.</typeparam>
        /// <returns>Table rows.</returns>
        Task<List<T>> SelectAllAsync<T>();

        List<T> Select<T>(Expression<Func<T, object>> expression);

        Task<List<T>> SelectAsync<T>(Expression<Func<T, object>> expression);

        Task<List<T>> SelectAsync<T>(Expression<Func<T, object>> expression, CancellationToken cancellationToken);

        T SingleOrDefault<T>(Expression<Func<T, object>> expression);

        Task<T> SingleOrDefaultAsync<T>(Expression<Func<T, object>> expression);

        Task<T> SingleOrDefaultAsync<T>(Expression<Func<T, object>> expression, CancellationToken cancellationToken);

        /// <summary>
        /// Insert new object.
        /// </summary>
        /// <typeparam name="T">Table class.</typeparam>
        /// <param name="value">Inserted value.</param>
        void Insert<T>(T value);

        /// <summary>
        /// Insert new object.
        /// </summary>
        /// <typeparam name="T">Table class.</typeparam>
        /// <param name="value">Inserted value.</param>
        void Insert<T>(IDbTransaction transaction, T value);

        /// <summary>
        /// Insert new object as async.
        /// </summary>
        /// <typeparam name="T">Table class.</typeparam>
        /// <param name="value">Inserted value.</param>
        Task InsertAsync<T>(T value);

        /// <summary>
        /// Insert new object as async.
        /// </summary>
        /// <typeparam name="T">Table class.</typeparam>
        /// <param name="value">Inserted value.</param>
        Task InsertAsync<T>(IDbTransaction transaction, T value);

        /// <summary>
        /// Update object.
        /// </summary>
        /// <typeparam name="T">Table class.</typeparam>
        /// <param name="value">Inserted value.</param>
        void Update<T>(T value);

        /// <summary>
        /// Update object.
        /// </summary>
        /// <typeparam name="T">Table class.</typeparam>
        /// <param name="value">Inserted value.</param>
        void Update<T>(IDbTransaction transaction, T value);

        /// <summary>
        /// Update object.
        /// </summary>
        /// <typeparam name="T">Table class.</typeparam>
        /// <param name="value">Inserted value.</param>
        /// <param name="columns">Updated columns.</param>
        void Update<T>(T value, Expression<Func<T, object>> columns);

        /// <summary>
        /// Update object.
        /// </summary>
        /// <typeparam name="T">Table class.</typeparam>
        /// <param name="value">Inserted value.</param>
        /// <param name="columns">Updated columns.</param>
        void Update<T>(IDbTransaction transaction, T value, Expression<Func<T, object>> columns);

        /// <summary>
        /// Update object as async.
        /// </summary>
        /// <param name="value"></param>
        Task UpdateAsync<T>(T value);

        /// <summary>
        /// Update object as async.
        /// </summary>
        /// <param name="value"></param>
        Task UpdateAsync<T>(IDbTransaction transaction, T value);

        /// <summary>
        /// Update object as async.
        /// </summary>
        /// <param name="value">Updated value.</param>
        /// <param name="columns">Updated columns.</param>
        Task UpdateAsync<T>(T value, Expression<Func<T, object>> columns);

        /// <summary>
        /// Update object as async.
        /// </summary>
        /// <param name="value">Updated value.</param>
        /// <param name="columns">Updated columns.</param>
        Task UpdateAsync<T>(IDbTransaction transaction, T value, Expression<Func<T, object>> columns);

        /// <summary>
        /// Update object as async.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateAsync<T>(T value, CancellationToken cancellationToken);

        /// <summary>
        /// Update object as async.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateAsync<T>(IDbTransaction transaction, T value, CancellationToken cancellationToken);


        /// <summary>
        /// Update object as async.
        /// </summary>
        /// <param name="value">Updated value.</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateAsync<T>(T value, Expression<Func<T, object>> columns, CancellationToken cancellationToken);

        /// <summary>
        /// Update object as async.
        /// </summary>
        /// <param name="value">Updated value.</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateAsync<T>(IDbTransaction transaction, T value, Expression<Func<T, object>> columns, CancellationToken cancellationToken);

        /// <summary>
        /// Delete ALL data from the table.
        /// </summary>
        void Truncate<T>();
    }
}
