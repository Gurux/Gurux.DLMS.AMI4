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

using Gurux.DLMS.AMI.Server.Internal;
using Gurux.Service.DB;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Common.Enums;
using Gurux.Service.Orm.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;
using System.Diagnostics;

namespace Gurux.DLMS.AMI.Db
{
    /// <summary>
    /// Provides a factory for creating and managing SQL Server database connections with connection pooling.
    /// </summary>
    /// <remarks>Configures connection pooling parameters such as minimum and maximum pool size, and
    /// connection timeout.</remarks>
    public class SqlConnectionFactory
     : IDbConnectionFactory, Gurux.DLMS.AMI.Server.Repository.IGXHistoryConnectionDiagnostics
    {
        private readonly string _connectionString;
        private readonly DatabaseType _databaseType;
        internal DatabaseType ProviderType => _databaseType;
        /// <summary>
        /// Configuration template for an independent SQL query cache on each connection.
        /// </summary>
        public GXQueryCache? QueryCache { get; set; }


        /// <summary>
        /// Gets or sets the maximum number of connections allowed in the connection pool
        /// for this specific connection string.
        /// </summary>
        public int MaxPoolSize { get; set; } = 200;


        /// <summary>
        /// Gets or sets the minimum number of connections allowed in the connection pool 
        /// for this specific connection string.
        /// </summary>
        public int MinPoolSize { get; set; } = 10;

        /// <summary>
        /// SQL logging interceptor is used to log executed SQL queries and their execution times. 
        /// It can be set to an instance of GXSqlLoggingInterceptor, 
        /// which will receive notifications of executed SQL statements and their execution times, allowing for performance monitoring and debugging.
        /// </summary>
        public GXSqlLoggingInterceptor? Interceptor { get; set; }

        /// <summary>
        /// Initializes a new instance of the MsSqlConnectionFactory class using the specified connection string and
        /// configures connection pooling settings.
        /// </summary>
        /// <param name="databaseType">The type of the database.</param>
        /// <param name="connectionString">The connection string used to establish a connection to the database.</param>    
        public SqlConnectionFactory(DatabaseType databaseType, string connectionString)
        {
            _databaseType = databaseType;
            switch (_databaseType)
            {
                case DatabaseType.MySQL:
                    {
                        var builder = new MySqlConnectionStringBuilder(connectionString)
                        {
                            // The schema stores GUIDs as binary(16). Return Guid values so
                            // joined ORM rows share value-based identities instead of byte[] references.
                            OldGuids = true,
                            Pooling = true,
                            MinimumPoolSize = (UInt16)MinPoolSize,
                            MaximumPoolSize = (UInt16)MaxPoolSize,
                            ConnectionTimeout = 15
                        };
                        _connectionString = builder.ConnectionString;
                    }
                    break;
                case DatabaseType.MSSQL:
                    {
                        var builder = new SqlConnectionStringBuilder(connectionString)
                        {
                            Pooling = true,
                            MinPoolSize = MinPoolSize,
                            MaxPoolSize = MaxPoolSize,
                            ConnectTimeout = 15
                        };
                        _connectionString = builder.ConnectionString;
                    }
                    break;
                case DatabaseType.SqLite:
                    _connectionString = new SqliteConnectionStringBuilder(connectionString).ConnectionString;
                    break;
                case DatabaseType.Oracle:
                    {
                        var builder = new OracleConnectionStringBuilder(connectionString)
                        {
                            Pooling = true,
                            MinPoolSize = MinPoolSize,
                            MaxPoolSize = MaxPoolSize
                        };
                        _connectionString = builder.ConnectionString;
                    }
                    break;
                case DatabaseType.PostgreSQL:
                    {
                        var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
                        {
                            Pooling = true,
                            MinPoolSize = MinPoolSize,
                            MaxPoolSize = MaxPoolSize
                        };
                        _connectionString = builder.ConnectionString;
                    }
                    break;
                default:
                    throw new NotSupportedException($"Database type {databaseType} is not supported by {nameof(SqlConnectionFactory)}.");
            }
        }

        /// <inheritdoc/>
        public async Task<DbConnection> CreateOpenConnectionAsync(
                CancellationToken cancellationToken = default)
        {
            DbConnection con = _databaseType switch
            {
                DatabaseType.MySQL => new MySqlConnection(_connectionString),
                DatabaseType.MSSQL => new SqlConnection(_connectionString),
                DatabaseType.SqLite => new SqliteConnection(_connectionString),
                DatabaseType.Oracle => new OracleConnection(_connectionString),
                DatabaseType.PostgreSQL => new Npgsql.NpgsqlConnection(_connectionString),
                _ => throw new NotSupportedException($"Database type {_databaseType} is not supported by {nameof(SqlConnectionFactory)}."),
            };
            await con.OpenAsync(cancellationToken);
            return con;
        }

        void Gurux.DLMS.AMI.Server.Repository.IGXHistoryConnectionDiagnostics.ConfigureHistoryLease(
            GXDbConnection db, bool notifySqlExcution) => ConfigureConnectionDiagnostics(db, notifySqlExcution);

        private void ConfigureConnectionDiagnostics(GXDbConnection db, bool notifySqlExcution)
        {
            var interceptor = Interceptor;
            if (notifySqlExcution && interceptor != null)
                {
                db.OnSqlExecuted += (_, e) => interceptor.PublishOrmCommand(e.Sql, e.Elapsed.TotalMilliseconds);
            }
        }

        /// <inheritdoc/>        
        public async Task<GXDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default, bool notifySqlExcution = true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var historyLease = Gurux.DLMS.AMI.Server.Repository.GXHistoryScope.TryLease(this, notifySqlExcution);
            if (historyLease != null) {
                return historyLease;
            }
            // GXQueryCache must not be shared by concurrent ORM connections: reads can
            // otherwise spuriously return no rows, including active-user authorization checks.
            var profile = QueryCache;
            var cache = profile == null ? null : new GXQueryCache(profile.CacheTime, profile.DatabaseType);
            var ret = new GXDbConnection(await CreateOpenConnectionAsync(cancellationToken), null, cache);
            ConfigureConnectionDiagnostics(ret, notifySqlExcution);
            return ret;
        }
    }
}
