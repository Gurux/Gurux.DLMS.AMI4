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

using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm.Model;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Module.Testing;

/// <summary>Owns test tables in a disposable Docker database, or a private SQLite file.</summary>
public sealed class DatabaseTestContext : IAsyncDisposable
{
    private readonly string? _file;
    public SqlConnectionFactory Factory { get; }
    public static string Provider => Environment.GetEnvironmentVariable("AMI_TEST_DATABASE") ?? "SQLite";

    public DatabaseTestContext()
    {
        if (Provider == "SQLite")
        {
            _file = Path.Combine(Path.GetTempPath(), $"ami-database-{Guid.NewGuid():N}.db");
            Factory = new SqlConnectionFactory(DatabaseType.SqLite, $"Data Source={_file};Foreign Keys=False;Pooling=False");
            return;
        }
        if (Environment.GetEnvironmentVariable("AMI_TEST_DISPOSABLE") != "true" ||
            !Guid.TryParseExact(Environment.GetEnvironmentVariable("AMI_TEST_RUN_ID"), "N", out _))
            {
            throw new InvalidOperationException("Docker database tests require the disposable database runner.");
        }
        var provider = Provider switch
        {
            "MySQL" or "MariaDB" => DatabaseType.MySQL,
            "PostgreSQL" => DatabaseType.PostgreSQL,
            "MSSQL" => DatabaseType.MSSQL,
            "Oracle" => DatabaseType.Oracle,
            _ => throw new InvalidOperationException("Unsupported test database.")
        };
        var connection = Environment.GetEnvironmentVariable("AMI_TEST_CONNECTION_STRING")
            ?? throw new InvalidOperationException("No disposable database connection was configured.");
        Factory = new SqlConnectionFactory(provider, connection);
    }

    public async Task CreateTablesAsync(bool recipients = true)
    {
        await using var connection = await Factory.GetConnectionAsync();
        var schema = new GXSchemaManager(connection);
        schema.CreateTable(typeof(GXPerformance), false, false);
        if (recipients) {
            CreateRecipientTables(schema);
        }
    }

    public async Task CreateRecipientTablesAsync()
    {
        await using var connection = await Factory.GetConnectionAsync();
        CreateRecipientTables(new GXSchemaManager(connection));
    }

    private static void CreateRecipientTables(GXSchemaManager schema)
    {
        foreach (var type in new[] { typeof(GXUser), typeof(GXRole), typeof(GXUserRole) })
            {
            schema.CreateTable(type, false, false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_file != null) { File.Delete(_file); return; }
        // Only the four fixture-owned tables in the run's disposable container are removed.
        await using var connection = await Factory.GetConnectionAsync();
        var schema = new GXSchemaManager(connection);
        foreach (var type in new[] { typeof(GXUserRole), typeof(GXRole), typeof(GXUser), typeof(GXPerformance) })
            {
            if (schema.TableExist(type)) {
                schema.DropTable(type, false);
            }
        }
    }
}
