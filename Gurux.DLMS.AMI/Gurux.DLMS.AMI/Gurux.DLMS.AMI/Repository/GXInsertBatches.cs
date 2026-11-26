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
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Inserts bounded row batches while preserving the caller's transaction and ORM identity assignment.</summary>
public static class GXInsertBatches
{
    /// <summary>Maximum number of rows allowed in one SQL Server VALUES insert.</summary>
    private const int MaximumRowsPerInsert = 1000;

    /// <summary>Inserts all rows in bounded statements and applies the same column configuration to every batch.</summary>
    /// <typeparam name="T">The mapped entity type to insert.</typeparam>
    /// <param name="connection">The ORM connection that executes the insert statements.</param>
    /// <param name="transaction">The caller-owned transaction encompassing every batch.</param>
    /// <param name="rows">The entities whose identifiers and references are retained.</param>
    /// <param name="configure">Optional exclusions or column configuration applied to each statement.</param>
    /// <param name="cancellationToken">Cancels before the next batch is constructed or inserted.</param>
    /// <remarks>The caller remains responsible for committing or rolling back the complete operation.</remarks>
    /// <exception cref="ArgumentNullException">A required connection, transaction or row sequence is null.</exception>
    /// <exception cref="OperationCanceledException">Cancellation was requested before a batch.</exception>
    /// <exception cref="GXDatabaseException">The database rejects an insert statement.</exception>
    public static void Insert<T>(GXDbConnection connection, IDbTransaction transaction,
        IEnumerable<T> rows, Action<GXInsertArgs>? configure = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(rows);
        foreach (var batch in rows.Chunk(MaximumRowsPerInsert))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var args = GXInsertArgs.InsertRange(batch);
            configure?.Invoke(args);
            connection.Insert(transaction, args);
        }
    }
}
