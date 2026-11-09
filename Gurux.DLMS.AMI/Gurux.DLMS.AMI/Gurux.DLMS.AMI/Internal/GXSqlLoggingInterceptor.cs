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
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Publishes ORM and EF Core SQL commands to installed SQL recording modules.</summary>
public sealed class GXSqlLoggingInterceptor(GXSqlEvents events) : DbCommandInterceptor
{
    /// <summary>Receives completed commands from Gurux ORM connections.</summary>
    public void PublishOrmCommand(string sql, double durationMs)
    {
        events.Publish(sql, durationMs, "ORM");
    }

    /// <inheritdoc/>
    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        events.Publish(command.CommandText, eventData.Duration.TotalMilliseconds, "EF Core");
        return result;
    }

    /// <inheritdoc/>
    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        events.Publish(command.CommandText, eventData.Duration.TotalMilliseconds, "EF Core");
        return result;
    }

    /// <inheritdoc/>
    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        events.Publish(command.CommandText, eventData.Duration.TotalMilliseconds, "EF Core");
        return result;
    }

    /// <inheritdoc/>
    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
        DbDataReader result, CancellationToken cancellationToken = default)
    {
        events.Publish(command.CommandText, eventData.Duration.TotalMilliseconds, "EF Core");
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc/>
    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
        int result, CancellationToken cancellationToken = default)
    {
        events.Publish(command.CommandText, eventData.Duration.TotalMilliseconds, "EF Core");
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc/>
    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
        object? result, CancellationToken cancellationToken = default)
    {
        events.Publish(command.CommandText, eventData.Duration.TotalMilliseconds, "EF Core");
        return ValueTask.FromResult(result);
    }
}
