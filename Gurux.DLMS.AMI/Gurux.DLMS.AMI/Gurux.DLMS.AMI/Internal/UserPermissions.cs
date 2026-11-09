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

using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Reads and stores application grants independently of OAuth scopes.</summary>
internal static class UserPermissions
{
    internal static async Task<List<string>> ReadRolesAsync(GXDbConnection connection, string userId)
    {
        var args = GXSelectArgs.Select<GXRole>(p => p.Name!, p => p.Removed == null);
        args.Joins.AddInnerJoin<GXRole, GXUserRole>(p => p.Id, p => p.RoleId!);
        args.Where.And<GXUserRole>(p => p.UserId == userId);
        return await connection.SelectAsync<string>(args);
    }
    internal static async Task<List<System.Security.Claims.Claim>> ReadRoleClaimsAsync(GXDbConnection connection,
        string userId, string roleClaimType)
    {
        var args = GXSelectArgs.SelectAll<GXRoleClaim>();
        args.Joins.AddInnerJoin<GXRoleClaim, GXRole>(p => p.RoleId, p => p.Id);
        args.Joins.AddInnerJoin<GXRole, GXUserRole>(p => p.Id, p => p.RoleId!);
        args.Where.And<GXRole>(p => p.Removed == null);
        args.Where.And<GXUserRole>(p => p.UserId == userId);
        return [.. (await connection.SelectAsync<GXRoleClaim>(args))
            .Where(p => !string.IsNullOrEmpty(p.ClaimType) && p.ClaimValue != null &&
                p.ClaimType != "Permission" && p.ClaimType != roleClaimType)
            .Select(p => new System.Security.Claims.Claim(p.ClaimType!, p.ClaimValue!))];
    }
    internal static async Task<List<string>> ReadDirectAsync(GXDbConnection connection, string userId)
    {
        var args = GXSelectArgs.Select<GXPermission>(p => p.Name!);
        args.Joins.AddInnerJoin<GXPermission, GXUserPermission>(p => p.Id, p => p.PermissionId!);
        args.Where.And<GXUserPermission>(p => p.UserId == userId);
        return [.. (await connection.SelectAsync<string>(args)).Distinct(StringComparer.Ordinal)];
    }

    internal static async Task<List<string>> ReadEffectiveAsync(GXDbConnection connection, string userId)
    {
            if ((await ReadRolesAsync(connection, userId)).Contains(Client.Shared.GXRoles.Admin, StringComparer.Ordinal))
            {
                // Administrators implicitly have every registered permission, even without explicit role grants.
                return [.. (await connection.SelectAsync<string>(GXSelectArgs.Select<GXPermission>(p => p.Name!)))
                    .Distinct(StringComparer.Ordinal).OrderBy(p => p, StringComparer.Ordinal)];
            }
        var direct = await ReadDirectAsync(connection, userId);
        var args = GXSelectArgs.Select<GXPermission>(p => p.Name!);
        args.Joins.AddInnerJoin<GXPermission, GXRolePermission>(p => p.Id, p => p.PermissionId!);
        args.Joins.AddInnerJoin<GXRolePermission, GXUserRole>(p => p.RoleId!, p => p.RoleId!);
        args.Joins.AddInnerJoin<GXUserRole, GXRole>(p => p.RoleId!, p => p.Id);
        args.Where.And<GXRole>(p => p.Removed == null);
        args.Where.And<GXUserRole>(p => p.UserId == userId);
        direct.AddRange(await connection.SelectAsync<string>(args));
        return [.. direct.Distinct(StringComparer.Ordinal).OrderBy(p => p, StringComparer.Ordinal)];
    }

    internal static async Task<List<GXPermission>> ValidateAsync(GXDbConnection connection, IEnumerable<string> names)
    {
        var requested = names.Distinct(StringComparer.Ordinal).ToArray();
        if (requested.Length == 0)
        {
            return [];
        }

        var args = GXSelectArgs.SelectAll<GXPermission>(p => requested.Contains(p.Name!));
        var permissions = await connection.SelectAsync<GXPermission>(args);
        var missing = requested.Except(permissions.Select(p => p.Name!), StringComparer.Ordinal).ToArray();
        if (missing.Length != 0)
        {
            throw new ArgumentException($"Unknown permissions: {string.Join(", ", missing)}");
        }

        return permissions;
    }

    internal static async Task ReplaceAsync(GXDbConnection connection, string userId, IEnumerable<string> names)
    {
        var permissions = await ValidateAsync(connection, names);
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            connection.Delete(transaction, GXDeleteArgs.Delete<GXUserPermission>(p => p.UserId == userId));
            if (permissions.Count != 0)
            {
                connection.Insert(transaction, GXInsertArgs.InsertRange(permissions.Select(p => new GXUserPermission { UserId = userId, PermissionId = p.Id }).ToList()));
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
