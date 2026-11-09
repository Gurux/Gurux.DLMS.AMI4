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
using Gurux.Service.Orm;
namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Enlists repository connections in an existing history transaction, including custom factories.</summary>
internal static class GXHistoryConnections
{
    internal static Task<GXDbConnection> GetHistoryConnectionAsync(this IDbConnectionFactory factory,
        CancellationToken cancellationToken = default, bool notifySqlExcution = true)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var lease = GXHistoryScope.TryLease(factory, notifySqlExcution);
        return lease != null ? Task.FromResult(lease) : factory.GetConnectionAsync(cancellationToken, notifySqlExcution);
    }
}
