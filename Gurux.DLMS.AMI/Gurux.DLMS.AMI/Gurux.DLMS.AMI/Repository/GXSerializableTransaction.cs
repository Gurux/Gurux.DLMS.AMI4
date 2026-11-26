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
using System.Data.Common;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Enums;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Retries deadlock victims with a fresh serializable transaction before returning committed results.</summary>
public static class GXSerializableTransaction
{
    /// <summary>Runs at most three complete attempts, committing before returning to external side effects.</summary>
    /// <typeparam name="T">The result returned only after a successful commit.</typeparam>
    /// <param name="open">Creates a database connection owned by one attempt.</param>
    /// <param name="execute">Runs database work without publishing external side effects.</param>
    /// <param name="token">Cancels connection creation, database work and retry delays.</param>
    /// <returns>The successfully committed callback result.</returns>
    /// <exception cref="GXDatabaseException">The database operation cannot be completed.</exception>
    /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
    public static async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<GXDbConnection>> open,
        Func<GXDbConnection, DbTransaction, CancellationToken, Task<T>> execute, CancellationToken token = default)
    {
        for (int attempt = 0; ; ++attempt)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                await using var connection = await open(token);
                await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, token);
                var result = await execute(connection, transaction, token);
                await transaction.CommitAsync(token);
                return result;
            }
            catch (GXDatabaseException error) when (error.ErrorType == DatabaseErrorType.Deadlock && attempt < 2)
            {
                // Disposal rolls back uncommitted work before a fresh connection is opened.
                // Jitter avoids repeatedly colliding with another server's transaction.
                await Task.Delay(Random.Shared.Next(25, 76) * (1 << attempt), token);
            }
        }
    }
}
