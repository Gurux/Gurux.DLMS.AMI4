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

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Repository;

public partial class LogTypeRepository
{
    /// <inheritdoc />
    public async Task EnsureDefaultsAsync(CancellationToken cancellationToken = default)
    {
        await GXHistoryMutation.RunAllAsync<GXLogType>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices,
    async () =>
    {
        await _authorizationService.EnsureAnyPolicyAsync(User, GXLogTypePolicies.Add);
        await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var changed = new List<GXLogType>();
        foreach (var descriptor in GXLogStore.Descriptors)
        {
            var name = "BUILTIN:" + descriptor.Source.ToUpperInvariant();
            var existing = await connection.SingleOrDefaultAsync<GXLogType>(transaction,
                GXSelectArgs.Select<GXLogType>(t => t.Id, t => t.NormalizedName == name), cancellationToken);
            if (existing == null)
            {
                changed.Add(await GXLogStore.EnsureTypeAsync(connection, descriptor, transaction, cancellationToken));
            }
        }
        await transaction.CommitAsync(cancellationToken);
        await NotifyDefaultChangesAsync(changed, NotificationAction.Add, cancellationToken);
    }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ResetToDefaultsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        ids = ids.ToArray();
        await GXHistoryMutation.RunIdsAsync<GXLogType, Guid>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, ids,
    async () =>
    {
        ArgumentNullException.ThrowIfNull(ids);
        var selected = ids.Distinct().ToArray();
        if (selected.Length == 0 || selected.Contains(Guid.Empty))
        {
            throw new ArgumentException("Existing built-in log type identifiers are required.", nameof(ids));
        }
        await _authorizationService.EnsureAnyPolicyAsync(User, GXLogTypePolicies.Edit);
        await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var rows = await connection.SelectAsync<GXLogType>(transaction,
            GXSelectArgs.SelectAll<GXLogType>(t => selected.Contains(t.PublicId)), cancellationToken);
        var defaults = GXLogStore.Descriptors.ToDictionary(d => "BUILTIN:" + d.Source.ToUpperInvariant(), StringComparer.Ordinal);
        if (rows.Count != selected.Length || rows.Any(t => t.NormalizedName == null || !defaults.ContainsKey(t.NormalizedName)))
        {
            throw new ArgumentException("Only known built-in log types can be reset.", nameof(ids));
        }
        var policies = new Dictionary<long, Guid>();
        foreach (var row in rows)
        {
            var policyName = defaults[row.NormalizedName!].ViewPolicy.ToUpperInvariant();
            var policy = await connection.SingleOrDefaultAsync<GXPolicy>(transaction,
                GXSelectArgs.Select<GXPolicy>(p => p.Id, p => p.NormalizedName == policyName && p.Removed == null), cancellationToken)
                ?? throw new InvalidOperationException("Ensure default policies before resetting built-in log types.");
            policies[row.Id] = policy.Id;
        }
        foreach (var row in rows)
        {
            row.Name = "builtin:" + defaults[row.NormalizedName!].Source;
            row.Enabled = true;
            row.Level = 0;
            row.Description = null;
            row.Removed = null;
            row.Updated = DateTimeOffset.UtcNow;
            row.ConcurrencyStamp = Guid.NewGuid().ToString();
            await connection.UpdateAsync(transaction, GXUpdateArgs.Update(row, t => new
            {
                t.Name, t.Enabled, t.Level, t.Description, t.Removed, t.Updated, t.ConcurrencyStamp
            }), cancellationToken);
            await connection.DeleteAsync(transaction,
                GXDeleteArgs.Delete<GXLogTypePolicy>(p => p.LogTypeId == row.Id), cancellationToken);
            await connection.InsertAsync(transaction, GXInsertArgs.Insert(new GXLogTypePolicy
            {
                LogTypeId = row.Id, PolicyId = policies[row.Id]
            }), cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        await NotifyDefaultChangesAsync(rows, NotificationAction.Edit, cancellationToken);
    }, cancellationToken);
    }

    private async Task NotifyDefaultChangesAsync(IEnumerable<GXLogType> rows, NotificationAction action, CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var users = await GetNotifiedUsers(row.PublicId, action);
            _notificationCache.Add(users, new GXTarget(action == NotificationAction.Add ? GXLogTypePolicies.Add : GXLogTypePolicies.Edit, row.PublicId, row.Name));
        }
    }
}
