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
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Installs the updates policy and missing built-in log tabs without rebuilding customized menus.</summary>
internal static class GXUpdatesDefaults
{
    public static GXPolicy CreatePolicy()
    {
        var policy = new GXPolicy(GXUpdatePolicies.Admin)
        {
            Description = "Administrators can view and check available updates.",
            SystemDefined = true,
            RequireAuthenticatedUser = true,
            RequirementOperator = PolicyRequirementOperator.And
        };
        // This page is reserved for administrators, including when permissions are assigned directly.
        policy.Requirements!.Add(new GXPolicyRequirement
        {
            Policy = policy,
            Type = PolicyRequirementType.Role,
            Value = GXRoles.Admin
        });
        return policy;
    }

    public static async Task EnsurePolicyAsync(GXDbConnection connection, IDbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var existing = await connection.SingleOrDefaultAsync<GXPolicy>(transaction,
            GXSelectArgs.Select<GXPolicy>(p => p.Id, p => p.NormalizedName == "UPDATES.ADMIN"), cancellationToken);
        if (existing != null)
        {
            return;
        }

        var policy = CreatePolicy();
        await connection.InsertAsync(transaction, GXInsertArgs.Insert(policy), cancellationToken);
        await connection.InsertAsync(transaction, GXInsertArgs.InsertRange(policy.Requirements!), cancellationToken);
    }   
}
