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
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using Gurux.DLMS.AMI.Shared;
using Gurux.Service.Orm;
using System.Data;
using System.Linq.Expressions;
using System.Text;

namespace Gurux.DLMS.AMI.Server.Internal
{
    public class GXHost : IGXHost, IGXDatabase
    {
        /// <inheritdoc/>
        public GXDbConnection? Connection
        {
            get;
            internal set;
        }

        /// <inheritdoc/>
        public IDbTransaction BeginTransaction()
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            return Connection.BeginTransaction();
        }

        /// <inheritdoc/>
        public IDbTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            return Connection.BeginTransaction(isolationLevel);
        }

        /// <inheritdoc/>
        public void CommitTransaction(IDbTransaction transaction)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            Connection.CommitTransaction(transaction);
        }

        /// <inheritdoc/>
        public void CreateTable<T>()
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            Connection.CreateTable<T>(false, false);
        }

        /// <inheritdoc/>
        public void Delete<T>(T value)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXDeleteArgs args = GXDeleteArgs.DeleteById<T>(value);
            Connection.Delete(args);
        }

        /// <inheritdoc/>
        public Task DeleteAsync<T>(T value)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXDeleteArgs args = GXDeleteArgs.DeleteById<T>(value);
            return Connection.DeleteAsync(args);
        }

        /// <inheritdoc/>
        public Task DeleteAsync<T>(T value, CancellationToken cancellationToken)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXDeleteArgs args = GXDeleteArgs.DeleteById<T>(value);
            return Connection.DeleteAsync(args, cancellationToken);
        }

        /// <inheritdoc/>
        public void DropTable<T>()
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            Connection.DropTable<T>(false);
        }

        /// <inheritdoc/>
        public void DropTable(Type type)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            Connection.DropTable(type, false);
        }

        /// <inheritdoc/>
        public void Insert<T>(T value)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXInsertArgs args = GXInsertArgs.Insert(value);
            Connection.Insert(args);
        }

        /// <inheritdoc/>
        public void Insert<T>(IDbTransaction transaction, T value)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXInsertArgs args = GXInsertArgs.Insert(value);
            Connection.Insert(transaction, args);
        }

        /// <inheritdoc/>
        public Task InsertAsync<T>(T value)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXInsertArgs args = GXInsertArgs.Insert(value);
            return Connection.InsertAsync(args);
        }

        /// <inheritdoc/>
        public Task InsertAsync<T>(IDbTransaction transaction, T value)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXInsertArgs args = GXInsertArgs.Insert(value);
            return Connection.InsertAsync(transaction, args);
        }

        /// <inheritdoc/>
        public bool IsEmpty<T>()
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            return Connection.IsEmpty<T>();
        }

        /// <inheritdoc/>
        public void RollbackTransaction(IDbTransaction transaction)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            Connection.RollbackTransaction(transaction);
        }

        /// <inheritdoc/>
        public List<T> Select<T>(Expression<Func<T, object>> expression)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXSelectArgs args = GXSelectArgs.Select(expression);
            return Connection.Select<T>(args);
        }

        /// <inheritdoc/>
        public List<T> SelectAll<T>()
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            return Connection.SelectAll<T>();
        }

        /// <inheritdoc/>
        public Task<List<T>> SelectAllAsync<T>()
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            return Connection.SelectAllAsync<T>();
        }

        /// <inheritdoc/>
        public Task<List<T>> SelectAsync<T>(Expression<Func<T, object>> expression)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXSelectArgs args = GXSelectArgs.Select(expression);
            return Connection.SelectAsync<T>(args);
        }

        /// <inheritdoc/>
        public Task<List<T>> SelectAsync<T>(Expression<Func<T, object>> expression, CancellationToken cancellationToken)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXSelectArgs args = GXSelectArgs.Select(expression);
            return Connection.SelectAsync<T>(args);
        }

        /// <inheritdoc/>
        public T SelectById<T>(object id)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            if (id is Guid guid)
            {
                return Connection.SelectById<T>(guid);
            }
            if (id is string str)
            {
                return Connection.SelectById<T>(str);
            }
            if (id is UInt32 uint32)
            {
                return Connection.SelectById<T>(uint32);
            }
            if (id is Int32 int32)
            {
                return Connection.SelectById<T>(int32);
            }
            if (id is Int64 int64)
            {
                return Connection.SelectById<T>(int64);
            }
            if (id is UInt64 uint64)
            {
                return Connection.SelectById<T>(uint64);
            }
            throw new ArgumentException(nameof(id));
        }

        /// <inheritdoc/>
        public T SelectById<T>(object id, Expression<Func<T, object>> columns)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            if (id is Guid guid)
            {
                return Connection.SelectById<T>(guid, columns);
            }
            if (id is string str)
            {
                return Connection.SelectById<T>(str, columns);
            }
            if (id is UInt32 uint32)
            {
                return Connection.SelectById<T>(uint32, columns);
            }
            if (id is Int32 int32)
            {
                return Connection.SelectById<T>(int32, columns);
            }
            if (id is Int64 int64)
            {
                return Connection.SelectById<T>(int64, columns);
            }
            if (id is UInt64 uint64)
            {
                return Connection.SelectById<T>(uint64, columns);
            }
            throw new ArgumentException(nameof(id));
        }

        /// <inheritdoc/>
        public Task<T> SelectByIdAsync<T>(object id, CancellationToken cancellationToken)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            if (id is Guid guid)
            {
                return Connection.SelectByIdAsync<T>(guid, cancellationToken);
            }
            if (id is string str)
            {
                return Connection.SelectByIdAsync<T>(str, cancellationToken);
            }
            if (id is UInt32 uint32)
            {
                return Connection.SelectByIdAsync<T>(uint32, cancellationToken);
            }
            if (id is Int32 int32)
            {
                return Connection.SelectByIdAsync<T>(int32, cancellationToken);
            }
            if (id is Int64 int64)
            {
                return Connection.SelectByIdAsync<T>(int64, cancellationToken);
            }
            if (id is UInt64 uint64)
            {
                return Connection.SelectByIdAsync<T>(uint64, cancellationToken);
            }
            throw new ArgumentException(nameof(id));
        }

        /// <inheritdoc/>
        public Task<T> SelectByIdAsync<T>(object id, Expression<Func<T, object>> columns, CancellationToken cancellationToken)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            if (id is Guid guid)
            {
                return Connection.SelectByIdAsync<T>(guid, columns, cancellationToken);
            }
            if (id is string str)
            {
                return Connection.SelectByIdAsync<T>(str, columns, cancellationToken);
            }
            if (id is UInt32 uint32)
            {
                return Connection.SelectByIdAsync<T>(uint32, columns, cancellationToken);
            }
            if (id is Int32 int32)
            {
                return Connection.SelectByIdAsync<T>(int32, columns, cancellationToken);
            }
            if (id is Int64 int64)
            {
                return Connection.SelectByIdAsync<T>(int64, columns, cancellationToken);
            }
            if (id is UInt64 uint64)
            {
                return Connection.SelectByIdAsync<T>(uint64, columns, cancellationToken);
            }
            throw new ArgumentException(nameof(id));
        }

        /// <inheritdoc/>
        public T SingleOrDefault<T>(Expression<Func<T, object>> expression)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXSelectArgs args = GXSelectArgs.Select(expression);
            return Connection.SingleOrDefault<T>(args);
        }

        /// <inheritdoc/>
        public Task<T> SingleOrDefaultAsync<T>(Expression<Func<T, object>> expression)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXSelectArgs args = GXSelectArgs.Select(expression);
            return Connection.SingleOrDefaultAsync<T>(args);
        }

        /// <inheritdoc/>
        public Task<T> SingleOrDefaultAsync<T>(Expression<Func<T, object>> expression, CancellationToken cancellationToken)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXSelectArgs args = GXSelectArgs.Select(expression);
            return Connection.SingleOrDefaultAsync<T>(args, cancellationToken);
        }

        /// <inheritdoc/>
        public bool TableExist<T>()
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            return Connection.TableExist<T>();
        }

        /// <inheritdoc/>
        public bool TableExist(Type type)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            return Connection.TableExist(type);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (Connection == null)
            {
                sb.AppendLine("Invalid DB connection.");
            }
            else
            {
                sb.AppendLine("DB connected to: " + Connection.ConnectionString);
                sb.AppendLine("DB connection state: " + Connection.State);
            }
            return sb.ToString();
        }

        /// <inheritdoc/>
        public void Truncate<T>()
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            Connection.Truncate<T>();
        }

        /// <inheritdoc/>
        public void Update<T>(T value)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXUpdateArgs args = GXUpdateArgs.Update(value);
            Connection.Update(args);
        }

        /// <inheritdoc/>
        public void Update<T>(IDbTransaction transaction, T value)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXUpdateArgs args = GXUpdateArgs.Update(value);
            Connection.Update(transaction, args);
        }

        /// <inheritdoc/>
        public void Update<T>(T value, Expression<Func<T, object>> columns)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXUpdateArgs args = GXUpdateArgs.Update(value, columns);
            Connection.Update(args);
        }

        /// <inheritdoc/>
        public void Update<T>(IDbTransaction transaction, T value, Expression<Func<T, object>> columns)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXUpdateArgs args = GXUpdateArgs.Update(value, columns);
            Connection.Update(transaction, args);
        }

        /// <inheritdoc/>
        public Task UpdateAsync<T>(T value)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXUpdateArgs args = GXUpdateArgs.Update(value);
            return Connection.UpdateAsync(args);
        }

        /// <inheritdoc/>
        public Task UpdateAsync<T>(IDbTransaction transaction, T value)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXUpdateArgs args = GXUpdateArgs.Update(value);
            return Connection.UpdateAsync(transaction, args);
        }

        /// <inheritdoc/>
        public Task UpdateAsync<T>(T value, Expression<Func<T, object>> columns)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXUpdateArgs args = GXUpdateArgs.Update(value, columns);
            return Connection.UpdateAsync(args);
        }

        /// <inheritdoc/>
        public Task UpdateAsync<T>(IDbTransaction transaction, T value, Expression<Func<T, object>> columns)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXUpdateArgs args = GXUpdateArgs.Update(value, columns);
            return Connection.UpdateAsync(transaction, args);
        }

        /// <inheritdoc/>
        public Task UpdateAsync<T>(T value, CancellationToken cancellationToken)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXUpdateArgs args = GXUpdateArgs.Update(value);
            return Connection.UpdateAsync(args, cancellationToken);
        }

        /// <inheritdoc/>
        public Task UpdateAsync<T>(IDbTransaction transaction, T value, CancellationToken cancellationToken)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXUpdateArgs args = GXUpdateArgs.Update(value);
            return Connection.UpdateAsync(transaction, args, cancellationToken);
        }

        /// <inheritdoc/>
        public Task UpdateAsync<T>(T value, Expression<Func<T, object>> columns, CancellationToken cancellationToken)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXUpdateArgs args = GXUpdateArgs.Update(value, columns);
            return Connection.UpdateAsync(args, cancellationToken);
        }

        /// <inheritdoc/>
        public Task UpdateAsync<T>(IDbTransaction transaction, T value, Expression<Func<T, object>> columns, CancellationToken cancellationToken)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            GXUpdateArgs args = GXUpdateArgs.Update(value, columns);
            return Connection.UpdateAsync(transaction, args, cancellationToken);
        }

        /// <inheritdoc/>
        public void UpdateTable<T>()
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            Connection.UpdateTable<T>();
        }

        /// <inheritdoc/>
        public void UpdateTable(Type type)
        {
            if (Connection == null)
            {
                throw new ArgumentNullException(nameof(Connection));
            }
            Connection.UpdateTable(type);
        }
    }
}
