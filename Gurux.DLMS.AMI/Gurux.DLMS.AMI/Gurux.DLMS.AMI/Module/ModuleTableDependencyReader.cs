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

using System.Data.Common;
namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Provides database table.</summary>
/// <param name="Schema">The schema.</param>
/// <param name="Name">The name.</param>
public sealed record DatabaseTable(string Schema, string Name)
{
    /// <summary>Gets or sets the key.</summary>
    public string Key => GXModuleTableRegistry.Key(Schema, Name);
}
/// <summary>Provides database foreign key.</summary>
/// <param name="Source">The source.</param>
/// <param name="Target">The target.</param>
public sealed record DatabaseForeignKey(string Source, string Target);
/// <summary>Provides database table graph.</summary>
/// <param name="Tables">The tables.</param>
/// <param name="ForeignKeys">The foreign keys.</param>
/// <param name="BlockedTables">The blocked tables.</param>
/// <param name="SqlServer">The sql server.</param>
/// <param name="BlocksDrop">The blocks drop.</param>
public sealed record DatabaseTableGraph(IReadOnlyList<DatabaseTable> Tables, IReadOnlyList<DatabaseForeignKey> ForeignKeys, IReadOnlySet<string> BlockedTables, bool SqlServer, bool BlocksDrop = false);
/// <summary>Provides module table dependency reader.</summary>
public static class ModuleTableDependencyReader
{
    /// <summary>Performs the read operation.</summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="transaction">The database transaction.</param>
    /// <param name="token">Token used to cancel the operation.</param>
    public static async Task<DatabaseTableGraph> ReadAsync(DbConnection connection, DbTransaction? transaction, CancellationToken token)
    {
        async Task<List<string[]>> Query(string sql)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            using var reader = await command.ExecuteReaderAsync(token);
            var rows = new List<string[]>();
            while (await reader.ReadAsync(token))
            {
                var row = new string[reader.FieldCount];
                for (var i = 0; i < row.Length; i++)
                {
                    row[i] = Convert.ToString(reader.GetValue(i)) ?? "";
                }

                rows.Add(row);
            }
            return rows;
        }
        if (connection.GetType().Name.Equals("SqliteConnection", StringComparison.OrdinalIgnoreCase))
        {
            if ((await Query("PRAGMA database_list")).Any(r => r[1] is not ("main" or "temp")))
            {
                throw new InvalidOperationException("Automatic cleanup does not support attached SQLite databases.");
            }

            var tables = (await Query("SELECT name FROM main.sqlite_schema WHERE type='table' AND name NOT LIKE 'sqlite_%'")).Select(r => new DatabaseTable("", r[0])).ToArray();
            var edges = new List<DatabaseForeignKey>();
            foreach (var table in tables)
            {
                foreach (var key in await Query("PRAGMA main.foreign_key_list('" + table.Name.Replace("'", "''") + "')"))
                {
                    edges.Add(new(table.Key, GXModuleTableRegistry.Key("", key[2])));
                }
            }

            var blocked = (await Query("SELECT tbl_name FROM main.sqlite_schema WHERE type='trigger'")).Select(r => GXModuleTableRegistry.Key("", r[0])).ToHashSet();
            foreach (var trigger in await Query("SELECT tbl_name FROM temp.sqlite_schema WHERE type='trigger'"))
            {
                blocked.Add(GXModuleTableRegistry.Key("", trigger[0]));
            }

            return new(tables, edges, blocked, false);
        }
        if (connection.GetType().Name == "SqlConnection")
        {
            if ((await Query("SELECT HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'VIEW DEFINITION')"))[0][0] != "1")
            {
                throw new InvalidOperationException("Safe module cleanup requires database VIEW DEFINITION permission.");
            }

            var rows = await Query("SELECT s.name,t.name,t.temporal_type FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE t.is_ms_shipped=0");
            var tables = rows.Select(r => new DatabaseTable(r[0], r[1])).ToArray();
            var edges = (await Query("SELECT ss.name,st.name,ts.name,tt.name FROM sys.foreign_keys f JOIN sys.tables st ON st.object_id=f.parent_object_id JOIN sys.schemas ss ON ss.schema_id=st.schema_id JOIN sys.tables tt ON tt.object_id=f.referenced_object_id JOIN sys.schemas ts ON ts.schema_id=tt.schema_id")).Select(r => new DatabaseForeignKey(GXModuleTableRegistry.Key(r[0], r[1]), GXModuleTableRegistry.Key(r[2], r[3]))).ToArray();
            var blocked = rows.Where(r => r[2] != "0").Select(r => GXModuleTableRegistry.Key(r[0], r[1])).ToHashSet();
            foreach (var trigger in await Query("SELECT s.name,t.name FROM sys.triggers tr JOIN sys.tables t ON t.object_id=tr.parent_id JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE tr.parent_class=1"))
            {
                blocked.Add(GXModuleTableRegistry.Key(trigger[0], trigger[1]));
            }

            bool blocksDrop = (await Query("SELECT name FROM sys.triggers WHERE parent_class=0 AND is_disabled=0")).Count != 0;
            var visibility = await Query("SELECT HAS_PERMS_BY_NAME(NULL,NULL,'VIEW ANY DEFINITION')");
            if (visibility[0][0] != "1")
            {
                blocksDrop = true;
            }
            else if ((await Query("SELECT name FROM sys.server_triggers WHERE is_disabled=0")).Count != 0)
            {
                blocksDrop = true;
            }

            return new(tables, edges, blocked, true, blocksDrop);
        }
        throw new InvalidOperationException("Automatic module data cleanup supports SQLite and SQL Server only; preserve data on this provider.");
    }
    /// <summary>Performs the quote operation.</summary>
    /// <param name="table">The table.</param>
    /// <param name="sqlServer">The sql server.</param>
    public static string Quote(DatabaseTable table, bool sqlServer) => sqlServer ? "[" + table.Schema.Replace("]", "]]") + "].[" + table.Name.Replace("]", "]]") + "]" : "\"main\".\"" + table.Name.Replace("\"", "\"\"") + "\"";
}

