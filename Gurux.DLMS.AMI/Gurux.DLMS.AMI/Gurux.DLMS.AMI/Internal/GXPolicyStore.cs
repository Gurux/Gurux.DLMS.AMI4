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
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>
/// Validates and atomically persists policies for repositories and module resources.
/// </summary>
internal static class GXPolicyStore
{
    [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "Preserves the established argument order used by repository and transaction callers.")]
    public static async Task<IReadOnlyList<Guid>> SaveAsync(GXDbConnection connection,
        IEnumerable<GXPolicy> policies, CancellationToken cancellationToken = default, IDbTransaction? externalTransaction = null)
    {
        var values = policies.ToArray();
        foreach (var policy in values)
        {
            policy.NormalizedName = policy.Name?.ToUpperInvariant() ?? string.Empty;
            Validator.ValidateObject(policy, new ValidationContext(policy), true);
            foreach (var requirement in policy.Requirements ?? [])
            {
                Validator.ValidateObject(requirement, new ValidationContext(requirement), true);
            }

            if (policy.SystemDefined)
            {
                throw new InvalidOperationException("System-defined policies cannot be changed through the public repository.");
            }
        }

        await using var ownedTransaction = externalTransaction == null ? await connection.BeginTransactionAsync(cancellationToken) : null;
        var transaction = externalTransaction ?? ownedTransaction!;
        var result = new List<Guid>();
        try
        {
            foreach (var policy in values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var old = policy.Id == Guid.Empty ? null : connection.SingleOrDefault<GXPolicy>(transaction,
                    GXSelectArgs.SelectAll<GXPolicy>(p => p.Id == policy.Id));
                if (policy.Id != Guid.Empty && old == null)
                {
                    throw new InvalidOperationException("The policy does not exist.");
                }

                if (old?.SystemDefined == true)
                {
                    throw new InvalidOperationException("System-defined policies cannot be edited.");
                }

                if (old?.Removed != null)
                {
                    throw new InvalidOperationException("Removed policies cannot be edited.");
                }

                if (old != null && !string.Equals(old.Name, policy.Name, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Policy Name is a permanent identifier. Edit DisplayName instead.");
                }

                // Use the same case-insensitive identifier comparison for create and update.
                var named = connection.SingleOrDefault<GXPolicy>(transaction,
                    GXSelectArgs.SelectAll<GXPolicy>(p => p.NormalizedName == policy.NormalizedName));
                if (named != null && named.Id != policy.Id)
                {
                    throw new ValidationException("A policy with this Name already exists.");
                }

                if (old != null && old.ConcurrencyStamp != policy.ConcurrencyStamp)
                {
                    throw new InvalidOperationException("The policy was changed by another user. Reload it before editing.");
                }

                var existing = old == null ? [] : connection.Select<GXPolicyRequirement>(transaction,
                    GXSelectArgs.SelectAll<GXPolicyRequirement>(r => r.Policy == old));
                if (old == null && policy.Requirements?.Count is not > 0)
                {
                    throw new ValidationException("A new policy must contain at least one requirement.");
                }

                if (policy.Requirements != null)
                {
                    var ids = new HashSet<Guid>();
                    foreach (var requirement in policy.Requirements)
                    {
                        if (requirement.Id == Guid.Empty)
                        {
                            requirement.Id = Guid.NewGuid();
                        }

                        if (!ids.Add(requirement.Id))
                        {
                            throw new ValidationException("A requirement identifier was supplied more than once.");
                        }

                        var saved = connection.SingleOrDefault<GXPolicyRequirement>(transaction,
                                                    GXSelectArgs.SelectAll<GXPolicyRequirement>(r => r.Id == requirement.Id));
                        if (saved != null && !existing.Any(r => r.Id == requirement.Id))
                        {
                            throw new ValidationException("A requirement cannot be moved from another policy.");
                        }
                    }
                }

                // Only explicit producer edits register sources; unchanged retired module
                // definitions must not be reconstructed by unrelated policy edits.
                IEnumerable<string> sourceNames = (policy.Requirements ?? [])
                    .Where(r => r.Type == PolicyRequirementType.Permission && !existing.Any(e =>
                        e.Type == PolicyRequirementType.Permission && e.Value == r.Value))
                    .Select(r => r.Value!);
                if (old == null) {
                    sourceNames = sourceNames.Append(policy.Name!);
                }
                foreach (var sourceName in sourceNames.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.Ordinal))
                {
                    var prefix = sourceName.Split('.')[0];
                    var source = await connection.SingleOrDefaultAsync<GXNotificationSource>(transaction,
                        GXSelectArgs.SelectAll<GXNotificationSource>(s => s.Source == prefix), cancellationToken);
                    await GXNotificationSourceStorage.RegisterAsync(connection, transaction, prefix, source?.ModuleId, cancellationToken);
                    await GXNotificationSourceStorage.RegisterAsync(connection, transaction, sourceName, source?.ModuleId, cancellationToken);
                }
                policy.Updated = DateTimeOffset.UtcNow;
                policy.ConcurrencyStamp = Guid.NewGuid().ToString();
                if (old == null)
                {
                    policy.Id = Guid.NewGuid();
                    policy.Removed = null;
                    var insert = GXInsertArgs.Insert(policy);
                    insert.Exclude<GXPolicy>(p => p.Requirements!);
                    await connection.InsertAsync(transaction, insert, cancellationToken);
                }
                else
                {
                    var update = GXUpdateArgs.Update(policy, p => new
                    {
                        p.DisplayName,
                        p.Description,
                        p.RequirementOperator,
                        p.RequireAuthenticatedUser,
                        p.Updated,
                        p.ConcurrencyStamp
                    });
                    update.Where.And<GXPolicy>(p => p.ConcurrencyStamp == old.ConcurrencyStamp);
                    update.Where.And<GXPolicy>(p => p.SystemDefined == false && p.Removed == null);
                    if (await connection.UpdateAsync(transaction, update, cancellationToken) != 1)
                    {
                        throw new InvalidOperationException("The policy was changed by another user.");
                    }
                }

                if (policy.Requirements != null)
                {
                    foreach (var removed in existing.Where(r => !policy.Requirements.Any(v => v.Id == r.Id)))
                    {
                        await connection.DeleteAsync(transaction, GXDeleteArgs.DeleteById<GXPolicyRequirement>(removed), cancellationToken);
                    }

                    foreach (var requirement in policy.Requirements)
                    {
                        requirement.Policy = policy;
                        requirement.Updated = policy.Updated;
                        if (existing.Any(r => r.Id == requirement.Id))
                        {
                            await connection.UpdateAsync(transaction, GXUpdateArgs.Update(requirement,
                            r => new { r.Type, r.ClaimType, r.Value, r.Name, r.Description, r.Updated }), cancellationToken);
                        }
                        else
                        {
                            await connection.InsertAsync(transaction, GXInsertArgs.Insert(requirement), cancellationToken);
                        }
                    }
                }
                result.Add(policy.Id);
            }
            await GXPermissionCatalog.SynchronizeAsync(connection, transaction, cancellationToken);
            if (ownedTransaction != null)
            {
                await ownedTransaction.CommitAsync(cancellationToken);
            }
            return result;
        }
        catch
        {
            if (ownedTransaction != null)
            {
                await ownedTransaction.RollbackAsync(CancellationToken.None);
            }
            throw;
        }
    }

    public static async Task DeleteAsync(GXDbConnection connection, IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var targets = ids.Distinct().ToArray();
        if (targets.Length == 0)
        {
            return;
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var policies = connection.Select<GXPolicy>(transaction, GXSelectArgs.SelectAll<GXPolicy>(p => targets.Contains(p.Id)), cancellationToken);
            if (policies.Any(p => p.SystemDefined))
            {
                throw new InvalidOperationException("System-defined policies cannot be deleted.");
            }

            await GXPolicyReferences.EnsureUnusedAsync(connection, transaction,
                            policies.Where(p => p.Removed == null).Select(p => p.Name!), cancellationToken);
            foreach (var policy in policies.Where(p => p.Removed == null))
            {
                string? expectedStamp = policy.ConcurrencyStamp;
                policy.Removed = policy.Updated = DateTimeOffset.UtcNow;
                policy.ConcurrencyStamp = Guid.NewGuid().ToString();
                var update = GXUpdateArgs.Update(policy, p => new { p.Removed, p.Updated, p.ConcurrencyStamp });
                update.Where.And<GXPolicy>(p => p.ConcurrencyStamp == expectedStamp && p.SystemDefined == false && p.Removed == null);
                if (await connection.UpdateAsync(transaction, update, cancellationToken) != 1)
                {
                    throw new InvalidOperationException("The policy was changed by another user.");
                }
            }
            await GXPermissionCatalog.SynchronizeAsync(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
