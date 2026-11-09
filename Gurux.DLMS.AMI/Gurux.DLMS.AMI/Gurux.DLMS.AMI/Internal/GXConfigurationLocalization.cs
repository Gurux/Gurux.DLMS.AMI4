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
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Preserves translations when configuration link source text changes.</summary>
internal static class GXConfigurationLocalization
{
    internal static async Task<bool> CopyAsync(GXDbConnection connection, IDbTransaction transaction,
        string? previous, string? current, GXUser creator, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(previous) || string.IsNullOrEmpty(current) || previous == current)
        {
            return false;
        }

        var previousHash = ClientHelpers.GetHashCode(previous);
        var currentHash = ClientHelpers.GetHashCode(current);
        GXSelectArgs Query(string hash)
        {
            var query = GXSelectArgs.SelectAll<GXLocalizedResource>(r => r.Hash == hash);
            query.Columns.Add<GXLanguage>(l => l.Id);
            query.Joins.AddInnerJoin<GXLocalizedResource, GXLanguage>(r => r.Language!, l => l.Id);
            return query;
        }
        var resources = await connection.SelectAsync<GXLocalizedResource>(transaction, Query(previousHash), cancellationToken);
        var existing = await connection.SelectAsync<GXLocalizedResource>(transaction, Query(currentHash), cancellationToken);
        var languages = existing.Select(r => r.Language?.Id).ToHashSet();
        bool changed = false;
        foreach (var resource in resources)
        {
            if (string.IsNullOrEmpty(resource.Language?.Id) || !languages.Add(resource.Language.Id))
            {
                continue;
            }

            // Leave the old key intact because other links may still use it.
            var copy = new GXLocalizedResource
            {
                Id = Guid.NewGuid(),
                Hash = currentHash,
                Value = resource.Value == previous ? current : resource.Value,
                Language = new GXLanguage { Id = resource.Language.Id },
                Creator = creator,
                CreationTime = DateTimeOffset.UtcNow,
                Updated = DateTimeOffset.UtcNow,
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                Status = resource.Status,
                Preload = resource.Preload
            };
            await connection.InsertAsync(transaction, GXInsertArgs.Insert(copy), cancellationToken);
            changed = true;
        }
        return changed;
    }
}
