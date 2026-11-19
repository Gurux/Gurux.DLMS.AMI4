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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Shares the history transaction with Identity instead of opening a competing writer.</summary>
internal sealed class GXHistoryIdentityScope : IAsyncDisposable
{
    /// <summary>Context associated with the current operation.</summary>
    private readonly DbContext _context;
    /// <summary>Connection string used when opening the backing database.</summary>
    private readonly string? _connectionString;
    /// <summary>Entity Framework transaction enlisted in the surrounding database operation.</summary>
    private readonly IDbContextTransaction _enlistment;

    /// <summary>Enlists identity persistence in the connection and transaction used by history restoration.</summary>
    /// <param name="context">Context whose identity or transaction is temporarily enlisted.</param>
    /// <param name="connectionString">Original connection string restored after the identity scope ends.</param>
    /// <param name="enlistment">Entity Framework transaction wrapper enlisted in the shared transaction.</param>
    private GXHistoryIdentityScope(DbContext context, string? connectionString, IDbContextTransaction enlistment)
    {
        this._context = context;
        this._connectionString = connectionString;
        this._enlistment = enlistment;
    }

    /// <summary>Enlists an identity DbContext in the active history transaction without transferring connection ownership.</summary>
    /// <param name="context">Optional identity DbContext to enlist in the current history transaction.</param>
    /// <param name="token">Cancels the asynchronous operation and any wait for its synchronization gate.</param>
    /// <returns>A restoration scope, or null when enlistment is unnecessary.</returns>
    /// <exception cref="InvalidOperationException">Identity is already enlisted in a different transaction or cannot join the active history transaction.</exception>
    internal static async Task<GXHistoryIdentityScope?> JoinAsync(DbContext? context, CancellationToken token = default)
    {
        var transaction = GXHistoryScope.CurrentTransaction;
        if (context == null || transaction == null)
        {
            return null;
        }
        if (context.Database.CurrentTransaction?.GetDbTransaction() == transaction)
        {
            return null;
        }
        if (context.Database.CurrentTransaction != null)
        {
            throw new InvalidOperationException("Identity already has a different transaction.");
        }
        var connectionString = context.Database.GetConnectionString();
        // EF disposes its owned connection when switching to the borrowed history connection.
        // Restore configuration afterwards so every provider can create a fresh connection.
        context.Database.SetDbConnection(transaction.Connection, contextOwnsConnection: false);
        try
        {
            var enlistment = await context.Database.UseTransactionAsync(transaction, token)
                ?? throw new InvalidOperationException("Identity could not join the history transaction.");
            return new GXHistoryIdentityScope(context, connectionString, enlistment);
        }
        catch
        {
            context.Database.SetDbConnection(null, contextOwnsConnection: true);
            context.Database.SetConnectionString(connectionString);
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _enlistment.DisposeAsync();
        _context.Database.SetDbConnection(null, contextOwnsConnection: true);
        _context.Database.SetConnectionString(_connectionString);
    }
}
