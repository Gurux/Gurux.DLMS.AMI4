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
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Shared;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Model;
namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Provides module table registry.</summary>
/// <param name="database">The database.</param>
public sealed class GXModuleTableRegistry(IGXDatabase database)
{
    private static readonly SemaphoreSlim RegistrationGate = new(1, 1);
    /// <summary>Performs the key operation.</summary>
    /// <param name="schema">The schema.</param>
    /// <param name="name">The name.</param>
    public static string Key(string schema, string name) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(schema.ToUpperInvariant() + "\0" + name.ToUpperInvariant())));
    internal static void EnsureSchema(GXDbConnection db)
    {
        var s = new GXSchemaManager(db);
        if (!s.TableExist<GXModuleTableOwnership>())
        {
            s.CreateTable<GXModuleTableOwnership>(false, false);
        }
    }
    internal static async Task<string> DefaultSchemaAsync(DbConnection connection, CancellationToken token)
    {
        if (connection.GetType().Name.Equals("SqliteConnection", StringComparison.OrdinalIgnoreCase))
        {
            return "";
        }

        if (connection.GetType().Name == "SqlConnection")
        {
            using var c = connection.CreateCommand();
            c.CommandText = "SELECT COALESCE(SCHEMA_NAME(), 'dbo')";
            return Convert.ToString(await c.ExecuteScalarAsync(token))!;
        }
        return "";
    }
    /// <summary>Performs the register operation.</summary>
    /// <param name="moduleId">The module id.</param>
    /// <param name="tables">The tables.</param>
    /// <param name="token">Token used to cancel the operation.</param>
    public async Task RegisterAsync(string moduleId, IReadOnlyList<AmiModuleTableRegistration> tables, CancellationToken token)
    {
        moduleId = moduleId.ToUpperInvariant();
        if (tables.Count == 0)
        {
            return;
        }

        foreach (var table in tables)
        {
            if (!table.Type.IsClass || table.Type.IsAbstract || !Enum.IsDefined(table.Kind) || table.Type.Assembly == typeof(IGXDatabase).Assembly || table.Type.Namespace?.StartsWith("Gurux.DLMS.AMI.Server", StringComparison.Ordinal) == true)
            {
                throw new InvalidOperationException("Modules cannot own host or shared tables.");
            }
        }

        await RegistrationGate.WaitAsync(token);
        try
        {
            await using var db = new GXDbConnection(await database.CreateOpenConnectionAsync(), null);
            EnsureSchema(db);
            var schema = await DefaultSchemaAsync(db.Connection, token);
            using var tx = db.BeginTransaction(System.Data.IsolationLevel.Serializable);
            var nameMethod = typeof(GXDbConnection).GetMethod(nameof(GXDbConnection.GetTableName))!;
            var protectedNames = typeof(IGXDatabase).Assembly.GetTypes().Where(t => t.IsClass && t.Namespace?.StartsWith("Gurux.DLMS.AMI.Shared.DTOs", StringComparison.Ordinal) == true).Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            protectedNames.Add(nameof(GXModuleTableOwnership));
            foreach (var declaration in tables)
            {
                // Public ORM API supplies the mapped table name; no private reflection.
                var name = (string)nameMethod.MakeGenericMethod(declaration.Type).Invoke(db, null)!;
                if (protectedNames.Contains(name))
                {
                    throw new InvalidOperationException("A declared table belongs to the host: " + name);
                }

                var actualSchema = schema;
                if (db.Connection.GetType().Name == "SqlConnection")
                {
                    using var resolve = db.Connection.CreateCommand();
                    resolve.Transaction = (System.Data.Common.DbTransaction)tx;
                    resolve.CommandText = "SELECT OBJECT_SCHEMA_NAME(OBJECT_ID(@name, 'U')), OBJECT_NAME(OBJECT_ID(@name, 'U'))";
                    var parameter = resolve.CreateParameter();
                    parameter.ParameterName = "@name";
                    parameter.Value = name;
                    resolve.Parameters.Add(parameter);
                    using var reader = await resolve.ExecuteReaderAsync(token);
                    if (await reader.ReadAsync(token) && !reader.IsDBNull(0))
                    {
                        actualSchema = reader.GetString(0);
                        name = reader.GetString(1);
                    }
                }
                if (protectedNames.Contains(name))
                {
                    throw new InvalidOperationException("The resolved table belongs to the host: " + name);
                }

                var id = Key(actualSchema, name);
                var previous = await db.SingleOrDefaultAsync<GXModuleTableOwnership>(tx, GXSelectArgs.SelectAll<GXModuleTableOwnership>(r => r.Id == id), token);
                if (previous != null && previous.ModuleId != moduleId)
                {
                    throw new InvalidOperationException("Table " + name + " is already owned by module " + previous.ModuleId + ".");
                }

                if (previous == null)
                {
                    await db.InsertAsync(tx, GXInsertArgs.Insert(new GXModuleTableOwnership { Id = id, ModuleId = moduleId, SchemaName = actualSchema, TableName = name, Kind = declaration.Kind }), token);
                }
                else
                {
                    previous.Kind = declaration.Kind;
                    previous.Dropped = false;
                    await db.UpdateAsync(tx, GXUpdateArgs.Update(previous, r => new { r.Kind, r.Dropped }), token);
                }
            }
            tx.Commit();
        }
        finally
        {
            RegistrationGate.Release();
        }
    }
    internal static async Task<List<GXModuleTableOwnership>> ReadAsync(GXDbConnection db, string moduleId, CancellationToken token)
    {
        moduleId = moduleId.ToUpperInvariant();
        if (!new GXSchemaManager(db).TableExist<GXModuleTableOwnership>())
        {
            return [];
        }

        return await db.SelectAsync<GXModuleTableOwnership>(GXSelectArgs.SelectAll<GXModuleTableOwnership>(r => r.ModuleId == moduleId), token);
    }
}



