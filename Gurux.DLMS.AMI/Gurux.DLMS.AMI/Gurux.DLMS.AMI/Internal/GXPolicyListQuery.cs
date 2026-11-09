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
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Common.Model;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>
/// Retrieves policy pages without applying row limits to joined requirements.
/// </summary>
internal static class GXPolicyListQuery
{
    public static async Task<GXPolicy[]> ReadAsync(GXDbConnection connection, ListPolicys? request,
        ListPolicysResponse? response, CancellationToken cancellationToken = default)
    {
        if (request?.Index < 0 || request?.Count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Index and Count must be nonnegative.");
        }

        var args = GXSelectArgs.SelectAll<GXPolicy>(p => p.Removed == null);
        if (request?.Filter != null)
        {
            args.Where.FilterBy(request.Filter);
        }

        if (!string.IsNullOrWhiteSpace(request?.Search))
        {
            string search = request.Search.Trim();
            var name = new GXColumnSchema { Name = nameof(GXPolicy.Name), Type = typeof(string) };
            var displayName = new GXColumnSchema { Name = nameof(GXPolicy.DisplayName), Type = typeof(string) };
            args.Where.And<GXPolicy>(p => GXSql.Contains(name, search, StringComparison.OrdinalIgnoreCase)
                || GXSql.Contains(displayName, search, StringComparison.OrdinalIgnoreCase));
        }
        if (request?.Included?.Any() == true)
        {
            args.Where.And<GXPolicy>(p => request.Included.Contains(p.Id));
        }

        if (request?.Exclude?.Any() == true)
        {
            args.Where.And<GXPolicy>(p => !request.Exclude.Contains(p.Id));
        }

        GXDateTimeFilters.Apply<GXPolicy>(args, request?.DateFilters);
        var total = GXSelectArgs.Select<GXPolicy>(p => GXSql.Count(1));
        total.Where.Append(args.Where);
        long count = await connection.SingleOrDefaultAsync<long>(total, cancellationToken);
        if (response != null)
        {
            response.Count = count;
            response.Policys = [];
        }
        if (count == 0)
        {
            return [];
        }

        string orderBy = string.IsNullOrEmpty(request?.OrderBy) ? nameof(GXPolicy.Name) : request.OrderBy;
        string[] allowed = [nameof(GXPolicy.Name), nameof(GXPolicy.DisplayName), nameof(GXPolicy.RequirementOperator),
            nameof(GXPolicy.RequireAuthenticatedUser), nameof(GXPolicy.SystemDefined), nameof(GXPolicy.Id), nameof(GXPolicy.Updated)];
        if (!allowed.Contains(orderBy, StringComparer.Ordinal))
        {
            throw new ArgumentException("Unsupported policy sort column.", nameof(request));
        }

        System.Linq.Expressions.Expression<Func<GXPolicy, object>> sort = orderBy switch
        {
            nameof(GXPolicy.Name) => p => p.Name,
            nameof(GXPolicy.DisplayName) => p => p.DisplayName!,
            nameof(GXPolicy.RequirementOperator) => p => p.RequirementOperator,
            nameof(GXPolicy.RequireAuthenticatedUser) => p => p.RequireAuthenticatedUser,
            nameof(GXPolicy.SystemDefined) => p => p.SystemDefined,
            nameof(GXPolicy.Updated) => p => p.Updated!,
            _ => p => p.Id
        };
        args.OrderBy.Add(sort);
        if (orderBy != nameof(GXPolicy.Id))
        {
            args.OrderBy.Add<GXPolicy>(p => p.Id);
        }

        args.Descending = request?.Descending == true;
        if (request?.Count > 0)
        {
            args.Index = request.Index;
            args.Count = request.Count;
        }
        var policies = (await connection.SelectAsync<GXPolicy>(args, cancellationToken)).ToArray();
        if (policies.Length > 0)
        {
            var ids = policies.Select(p => p.Id).ToArray();
            var requirements = GXSelectArgs.SelectAll<GXPolicyRequirement>();
            requirements.Columns.Add<GXPolicy>(p => p.Id);
            requirements.Joins.AddInnerJoin<GXPolicyRequirement, GXPolicy>(r => r.Policy!, p => p.Id);
            requirements.Where.And<GXPolicy>(p => ids.Contains(p.Id));
            requirements.OrderBy.Add<GXPolicyRequirement>(r => r.Type);
            requirements.OrderBy.Add<GXPolicyRequirement>(r => r.Id);
            var rows = await connection.SelectAsync<GXPolicyRequirement>(requirements, cancellationToken);
            foreach (var policy in policies)
            {
                policy.Requirements = [.. rows.Where(r => r.Policy?.Id == policy.Id)];
            }
        }
        response?.Policys = policies;

        return policies;
    }
}
