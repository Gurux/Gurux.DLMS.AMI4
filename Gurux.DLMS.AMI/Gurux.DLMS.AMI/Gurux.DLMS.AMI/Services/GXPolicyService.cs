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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Services
{
    /// <summary>
    /// Provides access to authorization policies.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    public sealed class GXPolicyService(IDbConnectionFactory dbFactory) : IGXPolicyService
    {
        private readonly IDbConnectionFactory _dbFactory = dbFactory;

        /// <inheritdoc />
        public async Task<GXPolicy?> FindAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            string normalizedName = name.ToUpperInvariant();
            await using var connection = await _dbFactory.GetConnectionAsync();
            var args = GXSelectArgs.SelectAll<GXPolicy>(w => w.NormalizedName == normalizedName && w.Removed == null);
            args.OrderBy.Add<GXPolicy>(x => x.Name);
            var policy = await connection.SingleOrDefaultAsync<GXPolicy>(args, cancellationToken);
            if (policy != null)
            {
                // A single-row limit on a collection join drops requirements on MySQL.
                // Read the complete rule independently of the parent policy lookup.
                policy.Requirements = await connection.SelectAsync<GXPolicyRequirement>(
                    GXSelectArgs.SelectAll<GXPolicyRequirement>(r => r.Policy == policy), cancellationToken);
            }
            return policy;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<GXPolicy>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            await using var connection = await _dbFactory.GetConnectionAsync();
            var args = GXSelectArgs.SelectAll<GXPolicy>(w => w.Removed == null);
            args.OrderBy.Add<GXPolicy>(x => x.Name);
            args.Columns.Add<GXPolicyRequirement>();
            args.Joins.AddLeftJoin<GXPolicy, GXPolicyRequirement>(x => x.Id, x => x.Policy!);
            return await connection.SelectAsync<GXPolicy>(args, cancellationToken);
        }
    }
}
