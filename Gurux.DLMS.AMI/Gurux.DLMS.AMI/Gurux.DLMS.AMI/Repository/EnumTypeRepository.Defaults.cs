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

using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Repository;

partial class EnumTypeRepository
{
    // These are definitions registered by the agent, localization, scheduler and
    // request logging code. Arbitrary runtime/module categories are not defaults.
    private static GXEnumType[] DefaultDefinitions()
    {
        var defaults = new List<GXEnumType>();
        void Add(string type, string name, int? value = null) =>
            defaults.Add(new GXEnumType { Type = type, Name = name, Value = value });
        Add(TargetType.Agent, nameof(CrudAction.Create), (int)CrudAction.Create);
        Add(TargetType.Agent, nameof(CrudAction.Update), (int)CrudAction.Update);
        foreach (var name in Enum.GetNames<AgentStatus>())
        {
            var value = (int)Enum.Parse<AgentStatus>(name);
            Add(TargetType.AgentStatus, name, value);
            Add(TargetType.SystemLog, name, value);
        }
        // Middleware uses the string overload, which deliberately leaves Value null.
        foreach (var name in Enum.GetNames<CrudAction>())
        {
            Add(TargetType.UserAction, name);
        }
        Add(TargetType.SystemLog, TargetType.Schedule);
        Add(TargetType.SystemLog, TargetType.LocalizedResource);
        Add(TargetType.SystemLog, "slow-rest-request");
        Add(TargetType.LocalizedResource, "Unknown");
        Add(TargetType.LocalizedResource, "Success");
        Add(TargetType.LocalizedResource, "Localized value missing");
        return defaults.ToArray();
    }

    private void RequireDefaultsAdministrator()
    {
        if (User?.IsInRole(GXRoles.Admin) != true)
        {
            throw new UnauthorizedAccessException();
        }
    }

    /// <inheritdoc />
    public async Task EnsureDefaultsAsync(CancellationToken cancellationToken = default)
    {
        await GXHistoryMutation.RunAllAsync<GXEnumType>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices,
    async () =>
    {
        RequireDefaultsAdministrator();
        cancellationToken.ThrowIfCancellationRequested();
        await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        foreach (var definition in DefaultDefinitions())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var existing = await connection.SingleOrDefaultAsync<GXEnumType>(transaction,
                GXSelectArgs.SelectAll<GXEnumType>(e => e.Type == definition.Type && e.Name == definition.Name), cancellationToken);
            if (existing == null)
            {
                await connection.InsertAsync(transaction, GXInsertArgs.Insert(definition), cancellationToken);
            }
        }
        await transaction.CommitAsync(cancellationToken);
    }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ResetToDefaultsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        ids = ids.ToArray();
        await GXHistoryMutation.RunIdsAsync<GXEnumType, int>(_dbFactory, User, new HttpContextAccessor().HttpContext?.RequestServices, ids,
    async () =>
    {
        RequireDefaultsAdministrator();
        ArgumentNullException.ThrowIfNull(ids);
        var selectedIds = ids.Distinct().ToArray();
        if (selectedIds.Length == 0)
        {
            throw new ArgumentException("Select at least one default enumeration entry.", nameof(ids));
        }
        cancellationToken.ThrowIfCancellationRequested();
        var defaults = DefaultDefinitions().ToDictionary(e => (e.Type, e.Name));
        await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var selected = await connection.SelectAsync<GXEnumType>(transaction,
            GXSelectArgs.SelectAll<GXEnumType>(e => selectedIds.Contains(e.Id)), cancellationToken);
        if (selected.Count != selectedIds.Length || selected.Any(e => !defaults.ContainsKey((e.Type, e.Name))))
        {
            throw new ArgumentException("Only known default enumeration entries can be reset.", nameof(ids));
        }
        foreach (var entry in selected)
        {
            entry.Value = defaults[(entry.Type, entry.Name)].Value;
            await connection.UpdateAsync(transaction, GXUpdateArgs.Update(entry, e => e.Value!), cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }, cancellationToken);
    }
}
