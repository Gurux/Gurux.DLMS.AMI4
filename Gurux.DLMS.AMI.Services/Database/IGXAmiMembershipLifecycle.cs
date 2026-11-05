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
namespace Gurux.DLMS.AMI.Services.Database;

/// <summary>Optional module-owned membership cleanup in the host's deletion transaction.</summary>
public interface IGXAmiMembershipLifecycle
{
    /// <summary>Removes module-owned membership references before the host deletes user groups.</summary>
    /// <remarks>The host owns the database and transaction; the provider must not dispose or complete them.</remarks>
    /// <param name="database">Host database instance used by the deletion operation.</param>
    /// <param name="transaction">Host transaction in which membership cleanup must run.</param>
    /// <param name="groupIds">Identifiers of the user groups being deleted.</param>
    /// <param name="cancellationToken">Token used to cancel cleanup.</param>
    /// <returns>A task that completes when module-owned references to the deleted groups have been removed.</returns>
    Task UserGroupsDeletingAsync(IGXDatabase database, IDbTransaction transaction, IReadOnlyList<Guid> groupIds, CancellationToken cancellationToken);
}
