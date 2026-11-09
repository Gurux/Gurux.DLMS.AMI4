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

using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using Gurux.DLMS.AMI.Shared.DTOs.View;
using Gurux.Service.Orm;
using System.Data;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Stores resource alternatives as policy memberships, preserving denied references on reads.</summary>
internal static class GXResourcePolicyStorage
{
    public static Task<List<string>> ReadAsync(GXDbConnection connection, Type resourceType, Guid id,
        CancellationToken cancellationToken = default) => ReadCoreAsync(connection, resourceType, id, cancellationToken);

    public static Task<List<string>> ReadAsync(GXDbConnection connection, Type resourceType, long id,
        CancellationToken cancellationToken = default) => ReadCoreAsync(connection, resourceType, id, cancellationToken);

    public static Task<List<string>> ReadAsync(GXDbConnection connection, Type resourceType, string id,
        CancellationToken cancellationToken = default) => ReadCoreAsync(connection, resourceType, id, cancellationToken);

    public static Task<List<string>> ReadAsync(GXDbConnection connection, IDbTransaction? transaction,
        Type resourceType, Guid id, CancellationToken cancellationToken = default)
        => ReadCoreAsync(connection, resourceType, id, cancellationToken, transaction);
    [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "Internal transaction overload preserves shared storage argument conventions.")]
    private static async Task<List<string>> ReadCoreAsync(GXDbConnection connection, Type resourceType, object id,
        CancellationToken cancellationToken = default, IDbTransaction? transaction = null)
    {
        id = await ResolveIdAsync(connection, resourceType, id, cancellationToken, transaction);
        List<Guid> ids;
        if (resourceType == typeof(GXScript))
        {
            ids = [.. (await connection.SelectAsync<GXScriptPolicy>(transaction, GXSelectArgs.Select<GXScriptPolicy>(l => l.PolicyId, l => l.ScriptId == (Guid)id), cancellationToken)).Select(l => l.PolicyId)];
        }
        else if (resourceType == typeof(GXTrigger))
        {
            ids = [.. (await connection.SelectAsync<GXTriggerPolicy>(transaction, GXSelectArgs.Select<GXTriggerPolicy>(l => l.PolicyId, l => l.TriggerId == (Guid)id), cancellationToken)).Select(l => l.PolicyId)];
        }
        else if (resourceType == typeof(GXWorkflow))
        {
            ids = [.. (await connection.SelectAsync<GXWorkflowPolicy>(transaction, GXSelectArgs.Select<GXWorkflowPolicy>(l => l.PolicyId, l => l.WorkflowId == (Guid)id), cancellationToken)).Select(l => l.PolicyId)];
        }
        else if (resourceType == typeof(GXMenu))
        {
            ids = [.. (await connection.SelectAsync<GXMenuPolicy>(transaction, GXSelectArgs.Select<GXMenuPolicy>(l => l.PolicyId, l => l.MenuId == (Guid)id), cancellationToken)).Select(l => l.PolicyId)];
        }
        else if (resourceType == typeof(GXModule))
        {
            ids = [.. (await connection.SelectAsync<GXModulePolicy>(transaction, GXSelectArgs.Select<GXModulePolicy>(l => l.PolicyId, l => l.ModuleId == (string)id), cancellationToken)).Select(l => l.PolicyId)];
        }

        else if (resourceType == typeof(GXMenuLink))
        {
            ids = [.. (await connection.SelectAsync<GXMenuLinkPolicy>(transaction, GXSelectArgs.Select<GXMenuLinkPolicy>(l => l.PolicyId, l => l.MenuLinkId == (Guid)id), cancellationToken)).Select(l => l.PolicyId)];
        }
        else if (resourceType == typeof(GXNotificationType))
        {
            ids = [.. (await connection.SelectAsync<GXNotificationTypePolicy>(transaction, GXSelectArgs.Select<GXNotificationTypePolicy>(l => l.PolicyId, l => l.NotificationTypeId == (Guid)id), cancellationToken)).Select(l => l.PolicyId)];
        }
        else if (resourceType == typeof(GXBlockGroup))
        {
            ids = [.. (await connection.SelectAsync<GXBlockGroupPolicy>(transaction, GXSelectArgs.Select<GXBlockGroupPolicy>(l => l.PolicyId, l => l.BlockGroupId == (Guid)id), cancellationToken)).Select(l => l.PolicyId)];
        }
        else if (resourceType == typeof(GXLogType))
        {
            ids = [.. (await connection.SelectAsync<GXLogTypePolicy>(transaction, GXSelectArgs.Select<GXLogTypePolicy>(l => l.PolicyId, l => l.LogTypeId == (long)id), cancellationToken)).Select(l => l.PolicyId)];
        }
        else if (resourceType == typeof(GXAppearance))
        {
            ids = [.. (await connection.SelectAsync<GXAppearancePolicy>(transaction, GXSelectArgs.Select<GXAppearancePolicy>(l => l.PolicyId, l => l.AppearanceId == (Guid)id), cancellationToken)).Select(l => l.PolicyId)];
        }
        else if (resourceType == typeof(GXView))
        {
            ids = [.. (await connection.SelectAsync<GXViewPolicy>(transaction, GXSelectArgs.Select<GXViewPolicy>(l => l.PolicyId, l => l.ViewId == (Guid)id), cancellationToken)).Select(l => l.PolicyId)];
        }
        else if (resourceType == typeof(GXViewColumn))
        {
            ids = [.. (await connection.SelectAsync<GXViewColumnPolicy>(transaction, GXSelectArgs.Select<GXViewColumnPolicy>(l => l.PolicyId, l => l.ViewColumnId == (Guid)id), cancellationToken)).Select(l => l.PolicyId)];
        }
        else
        {
            throw new ArgumentException("Unsupported resource type.", nameof(resourceType));
        }

        if (ids.Count == 0)
        {
            return [];
        }

        var rows = await connection.SelectAsync<GXPolicy>(transaction, GXSelectArgs.Select<GXPolicy>(p => new { p.Id, p.Name }, p => ids.Contains(p.Id)), cancellationToken);
        var named = rows.ToDictionary(p => p.Id, p => p.Name);
        return [.. ids.Select(policyId => named.TryGetValue(policyId, out var name) && !string.IsNullOrWhiteSpace(name)
            ? name! : ("#missing-policy:" + policyId).PadRight(GXPolicyNames.MaximumNameLength + 1, '!')).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal)];
    }

    [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "Preserves the established argument order used by repository and transaction callers.")]
    public static Task SaveAsync(GXDbConnection connection, Type resourceType, Guid id, IEnumerable<string>? policies,
        CancellationToken cancellationToken = default, IDbTransaction? transaction = null)
        => SaveCoreAsync(connection, resourceType, id, policies, cancellationToken, transaction);

    [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "Preserves the established argument order used by repository and transaction callers.")]
    public static Task SaveAsync(GXDbConnection connection, Type resourceType, long id, IEnumerable<string>? policies,
        CancellationToken cancellationToken = default, IDbTransaction? transaction = null)
        => SaveCoreAsync(connection, resourceType, id, policies, cancellationToken, transaction);

    [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "Preserves the established argument order used by repository and transaction callers.")]
    public static Task SaveAsync(GXDbConnection connection, Type resourceType, string id, IEnumerable<string>? policies,
        CancellationToken cancellationToken = default, IDbTransaction? transaction = null)
        => SaveCoreAsync(connection, resourceType, id, policies, cancellationToken, transaction);

    [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "Preserves the established argument order used by repository and transaction callers.")]
    private static async Task SaveCoreAsync(GXDbConnection connection, Type resourceType, object id, IEnumerable<string>? policies,
        CancellationToken cancellationToken = default, IDbTransaction? transaction = null)
    {
        if (id is Guid guid && guid == Guid.Empty || id is long number && number <= 0)
        {
            throw new ArgumentException("The resource must be persisted before its policies.", nameof(id));
        }

        id = await ResolveIdAsync(connection, resourceType, id, cancellationToken, transaction);
        var names = GXPolicyNames.Normalize(policies);
        await using var owned = transaction == null ? await connection.BeginTransactionAsync(cancellationToken) : null;
        var current = transaction ?? owned!;
        try
        {
            var ids = new List<Guid>();
            foreach (var name in names)
            {
                var normalized = name.ToUpperInvariant();
                var policy = await connection.SingleOrDefaultAsync<GXPolicy>(current,
                    GXSelectArgs.Select<GXPolicy>(p => p.Id, p => p.NormalizedName == normalized && p.Removed == null), cancellationToken) ?? throw new ArgumentException($"Policy '{name}' is unknown or removed.");

                if (!ids.Contains(policy.Id))
                {
                    ids.Add(policy.Id);
                }
            }
            if (resourceType == typeof(GXScript))
            {
                await connection.DeleteAsync(current, GXDeleteArgs.Delete<GXScriptPolicy>(l => l.ScriptId == (Guid)id), cancellationToken);
                if (ids.Count != 0)
                {
                    await connection.InsertAsync(current, GXInsertArgs.InsertRange(ids.Select(policyId => new GXScriptPolicy { ScriptId = (Guid)id, PolicyId = policyId }).ToList()), cancellationToken);
                }
            }
            else if (resourceType == typeof(GXTrigger))
            {
                await connection.DeleteAsync(current, GXDeleteArgs.Delete<GXTriggerPolicy>(l => l.TriggerId == (Guid)id), cancellationToken);
                if (ids.Count != 0)
                {
                    await connection.InsertAsync(current, GXInsertArgs.InsertRange(ids.Select(policyId => new GXTriggerPolicy { TriggerId = (Guid)id, PolicyId = policyId }).ToList()), cancellationToken);
                }
            }
            else if (resourceType == typeof(GXWorkflow))
            {
                await connection.DeleteAsync(current, GXDeleteArgs.Delete<GXWorkflowPolicy>(l => l.WorkflowId == (Guid)id), cancellationToken);
                if (ids.Count != 0)
                {
                    await connection.InsertAsync(current, GXInsertArgs.InsertRange(ids.Select(policyId => new GXWorkflowPolicy { WorkflowId = (Guid)id, PolicyId = policyId }).ToList()), cancellationToken);
                }
            }
            else if (resourceType == typeof(GXMenu))
            {
                await connection.DeleteAsync(current, GXDeleteArgs.Delete<GXMenuPolicy>(l => l.MenuId == (Guid)id), cancellationToken);
                if (ids.Count != 0)
                {
                    await connection.InsertAsync(current,
                    GXInsertArgs.InsertRange(ids.Select(policyId => new GXMenuPolicy { MenuId = (Guid)id, PolicyId = policyId }).ToList()), cancellationToken);
                }
            }
            else if (resourceType == typeof(GXModule))
            {
                await connection.DeleteAsync(current, GXDeleteArgs.Delete<GXModulePolicy>(l => l.ModuleId == (string)id), cancellationToken);
                if (ids.Count != 0)
                {
                    await connection.InsertAsync(current,
                        GXInsertArgs.InsertRange(ids.Select(policyId => new GXModulePolicy { ModuleId = (string)id, PolicyId = policyId }).ToList()), cancellationToken);
                }
            }
            else if (resourceType == typeof(GXMenuLink))
            {
                await connection.DeleteAsync(current, GXDeleteArgs.Delete<GXMenuLinkPolicy>(l => l.MenuLinkId == (Guid)id), cancellationToken);
                if (ids.Count != 0)
                {
                    await connection.InsertAsync(current,
                    GXInsertArgs.InsertRange(ids.Select(policyId => new GXMenuLinkPolicy { MenuLinkId = (Guid)id, PolicyId = policyId }).ToList()), cancellationToken);
                }
            }
            else if (resourceType == typeof(GXBlockGroup))
            {
                await connection.DeleteAsync(current, GXDeleteArgs.Delete<GXBlockGroupPolicy>(l => l.BlockGroupId == (Guid)id), cancellationToken);
                if (ids.Count != 0)
                {
                    await connection.InsertAsync(current,
                    GXInsertArgs.InsertRange(ids.Select(policyId => new GXBlockGroupPolicy { BlockGroupId = (Guid)id, PolicyId = policyId }).ToList()), cancellationToken);
                }
            }
            else if (resourceType == typeof(GXNotificationType))
            {
                await connection.DeleteAsync(current, GXDeleteArgs.Delete<GXNotificationTypePolicy>(l => l.NotificationTypeId == (Guid)id), cancellationToken);
                if (ids.Count != 0)
                {
                    await connection.InsertAsync(current, GXInsertArgs.InsertRange(ids.Select(policyId =>
                        new GXNotificationTypePolicy { NotificationTypeId = (Guid)id, PolicyId = policyId }).ToList()), cancellationToken);
                }
            }
            else if (resourceType == typeof(GXLogType))
            {
                await connection.DeleteAsync(current, GXDeleteArgs.Delete<GXLogTypePolicy>(l => l.LogTypeId == (long)id), cancellationToken);
                if (ids.Count != 0)
                {
                    await connection.InsertAsync(current,
                    GXInsertArgs.InsertRange(ids.Select(policyId => new GXLogTypePolicy { LogTypeId = (long)id, PolicyId = policyId }).ToList()), cancellationToken);
                }
            }
            else if (resourceType == typeof(GXAppearance))
            {
                await connection.DeleteAsync(current, GXDeleteArgs.Delete<GXAppearancePolicy>(l => l.AppearanceId == (Guid)id), cancellationToken);
                if (ids.Count != 0)
                {
                    await connection.InsertAsync(current,
                    GXInsertArgs.InsertRange(ids.Select(policyId => new GXAppearancePolicy { AppearanceId = (Guid)id, PolicyId = policyId }).ToList()), cancellationToken);
                }
            }
            else if (resourceType == typeof(GXView))
            {
                await connection.DeleteAsync(current, GXDeleteArgs.Delete<GXViewPolicy>(l => l.ViewId == (Guid)id), cancellationToken);
                if (ids.Count != 0)
                {
                    await connection.InsertAsync(current,
                    GXInsertArgs.InsertRange(ids.Select(policyId => new GXViewPolicy { ViewId = (Guid)id, PolicyId = policyId }).ToList()), cancellationToken);
                }
            }
            else if (resourceType == typeof(GXViewColumn))
            {
                await connection.DeleteAsync(current, GXDeleteArgs.Delete<GXViewColumnPolicy>(l => l.ViewColumnId == (Guid)id), cancellationToken);
                if (ids.Count != 0)
                {
                    await connection.InsertAsync(current,
                    GXInsertArgs.InsertRange(ids.Select(policyId => new GXViewColumnPolicy { ViewColumnId = (Guid)id, PolicyId = policyId }).ToList()), cancellationToken);
                }
            }
            else
            {
                throw new ArgumentException("Unsupported resource type.", nameof(resourceType));
            }

            if (owned != null)
            {
                await owned.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            if (owned != null)
            {
                await owned.RollbackAsync(CancellationToken.None);
            }

            throw;
        }
    }
    private static async Task<object> ResolveIdAsync(GXDbConnection connection, Type type, object id,
        CancellationToken token, IDbTransaction? transaction = null)
    {
        if (type == typeof(GXModule))
        {
            if (id is not string name || string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Module identifier is required.", nameof(id));
            }

            return name;
        }
        if (type != typeof(GXLogType))
        {
            if (id is not Guid)
            {
                throw new ArgumentException("The resource identifier must be a GUID.", nameof(id));
            }

            return id;
        }
        if (id is long)
        {
            return id;
        }

        if (id is not Guid publicId)
        {
            throw new ArgumentException("The log type identifier must be numeric or a public GUID.", nameof(id));
        }

        var args = GXSelectArgs.Select<GXLogType>(type => type.Id, type => type.PublicId == publicId);
        var row = transaction == null ? await connection.SingleOrDefaultAsync<GXLogType>(args, token)
            : await connection.SingleOrDefaultAsync<GXLogType>(transaction, args, token);
        return row?.Id ?? throw new ArgumentException("Unknown log type.", nameof(id));
    }
}

