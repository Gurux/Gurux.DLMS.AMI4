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

using Gurux.Service.Orm;
using System.Data.Common;

namespace Gurux.DLMS.AMI.Db
{
    /// <summary>
    /// This interface is used to create database connections. 
    /// It is used by the repositories to get a connection to the database. 
    /// The implementation of this interface is responsible for creating and opening the connection to the database.
    /// </summary>
    public interface IDbConnectionFactory
    {
        /// <summary>
        /// This method is used to get a connection to the database. The connection is not opened. The caller is responsible for opening the connection.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <param name="notifySqlExcution">Is SQL execution notified. This can be used with debugging.</param>
        /// <returns>A task representing the asynchronous operation, containing the database connection.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "The existing parameter order is retained for source compatibility with modules and callers.")]
        Task<GXDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default, bool notifySqlExcution = true);

        /// <summary>
        /// This method is used to create and open a connection to the database.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation, containing the open database connection.</returns>
        Task<DbConnection> CreateOpenConnectionAsync(
            CancellationToken cancellationToken = default);
    }
}
