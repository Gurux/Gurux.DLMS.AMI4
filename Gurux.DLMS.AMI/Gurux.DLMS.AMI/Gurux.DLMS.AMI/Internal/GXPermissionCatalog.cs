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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Materializes named permission requirements as shared, assignable rights.</summary>
internal static class GXPermissionCatalog
{
    public static async Task SynchronizeAsync(GXDbConnection connection, IDbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var policies = await connection.SelectAsync<GXPolicy>(transaction,
            GXSelectArgs.Select<GXPolicy>(p => p.Id, p => p.Removed == null), cancellationToken);
        var ids = policies.Select(p => p.Id).ToArray();
        if (ids.Length == 0)
        {
            return;
        }

        var query = GXSelectArgs.SelectAll<GXPolicyRequirement>(r => r.Type == PolicyRequirementType.Permission);
        query.Joins.AddInnerJoin<GXPolicyRequirement, GXPolicy>(r => r.Policy!, p => p.Id);
        query.Where.And<GXPolicy>(p => ids.Contains(p.Id));
        var definitions = await connection.SelectAsync<GXPolicyRequirement>(transaction, query, cancellationToken);
        foreach (var name in definitions.Select(r => r.Value).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.Ordinal))
        {
            if (await connection.SingleOrDefaultAsync<GXPermission>(transaction,
                GXSelectArgs.Select<GXPermission>(p => p.Id, p => p.Name == name), cancellationToken) == null)
            {
                await connection.InsertAsync(transaction, GXInsertArgs.Insert(new GXPermission(name)), cancellationToken);
            }
        }
    }
}
