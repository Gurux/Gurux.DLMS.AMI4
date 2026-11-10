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

using DeviceSettings = Gurux.DLMS.AMI.Shared.DTOs.Device.GXDLMSSettings;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Gurux.DLMS.AMI.Client.Shared.KeyManagement;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Repository;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server.Services.KeyManagement;

/// <summary>Coordinates encrypted local LLS versions and reference-only agent tasks.</summary>
public sealed class GXPasswordChangeService(IDbConnectionFactory factory, IDeviceRepository devices,
    IKeyManagementRepository keys, IGXAmiCryptoService crypto, IAuthorizationService authorization,
    IServiceProvider services)
{
    /// <summary>Prevents unrelated work from attempting a password whose meter state is uncertain.</summary>
    public async Task EnsureOrdinaryConnectionAllowedAsync(Guid deviceId, CancellationToken token)
    {
        await using var db = await factory.GetConnectionAsync(token);
        var pending = await db.SingleOrDefaultAsync<GXPasswordChangeRecord>(
            GXSelectArgs.Select<GXPasswordChangeRecord>(r => r.Id, r => r.DeviceId == deviceId && r.State == "Writing"), token);
        if (pending != null)
        {
            throw new InvalidOperationException("A password change needs verification. Retry its operation before ordinary meter work.");
        }
    }

    private static string UserId(ClaimsPrincipal actor) => actor.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? actor.FindFirstValue("sub") ?? throw new UnauthorizedAccessException();

    /// <summary>Lists visible operation metadata without credential material.</summary>
    public async Task<IReadOnlyList<GXPasswordChangeInfo>> ListAsync(ClaimsPrincipal actor, Guid managementId, CancellationToken token)
    {
        await authorization.EnsureAnyPolicyAsync(actor, GXKeyPermissions.Lifecycle);
        _ = await keys.ReadAsync(managementId, null, token);
        await using var db = await factory.GetConnectionAsync(token);
        var rows = await db.SelectAsync<GXPasswordChangeRecord>(
            GXSelectArgs.SelectAll<GXPasswordChangeRecord>(r => r.ManagementId == managementId), token);
        var result = new List<GXPasswordChangeInfo>();
        foreach (var row in rows.OrderByDescending(r => r.Created))
        {
            var info = Info(row);
            var task = await db.SingleOrDefaultAsync<GXTask>(GXSelectArgs.Select<GXTask>(
                t => new { t.Id, t.Start, t.Ready }, t => t.Id == row.TaskId), token);
            info.CanRetry = (row.State is "Queued" or "Writing") && task?.Ready != null;
            info.CanCancel = row.State == "Queued";
            result.Add(info);
        }
        return result;
    }

    /// <summary>Queues a fresh encrypted password version or a recovery from retained material.</summary>
    public async Task<GXPasswordChangeInfo> CreateAsync(ClaimsPrincipal actor, GXPasswordChangeRequest request, CancellationToken token)
    {
        await authorization.EnsureAnyPolicyAsync(actor, GXKeyPermissions.Lifecycle);
        await authorization.EnsureAnyPolicyAsync(actor, Gurux.DLMS.AMI.Client.Shared.GXDevicePolicies.Edit);
        await authorization.EnsureAnyPolicyAsync(actor, Gurux.DLMS.AMI.Client.Shared.GXTaskPolicies.Add);
        if (!GXDeviceSettingsKeys.CanUse(actor))
        {
            throw new UnauthorizedAccessException("Key use permission is required.");
        }
        if (request.RestoreChangeId != null)
        {
            await authorization.EnsureAnyPolicyAsync(actor, GXKeyPermissions.Recovery);
        }
        _ = UserId(actor);
        var device = await devices.ReadAsync(request.DeviceId, null, token);
        var management = await keys.ReadAsync(request.ManagementId, null, token);
        if (management.Removed != null || device.Enabled == false || management.Device?.Id != device.Id || !string.IsNullOrEmpty(management.ProviderId) && management.ProviderId != "local")
        {
            throw new ArgumentException("Password changes require device-specific local key management.");
        }
        var settings = JsonSerializer.Deserialize<DeviceSettings>(device.Settings ?? "null");
        if (settings?.Authentication != (byte)Gurux.DLMS.Enums.Authentication.Low || settings.Security != 0 ||
            settings.PreEstablished || device.Dynamic == true || device.Template == null)
        {
            throw new ArgumentException("Automatic password changes require an outbound LLS association without ciphering or pre-established sessions.");
        }
        var active = management.Keys?.Where(k => k.KeyType == KeyManagementType.LLSPassword &&
            k.State == KeyState.Active && k.Removed == null).ToArray() ?? [];
        if (active.Length != 1)
        {
            throw new InvalidOperationException("Exactly one current LLS password is required.");
        }
        GXKeyPolicy.ValidateUse(active[0], GXKeyPurpose.Password, DateTimeOffset.UtcNow);
        GXPasswordChangeInfo result = null!;
        await GXHistoryMutation.RunAsync<GXKeyManagement>(factory, actor, services, [management], async () =>
        {
            await using var db = await factory.GetHistoryConnectionAsync(token, notifySqlExcution: false);
            await using var tx = await db.BeginTransactionAsync(token);
            var templateId = device.Template.Id;
            var logicalName = request.AssociationLogicalName;
            var query = GXSelectArgs.Select<GXObjectTemplate>(o => o.Id,
                o => o.ObjectType == 15 && o.LogicalName == logicalName && o.Removed == null);
            query.Joins.AddInnerJoin<GXObjectTemplate, GXDeviceTemplate>(o => o.DeviceTemplate!, t => t.Id);
            query.Where.And<GXDeviceTemplate>(t => t.Id == templateId && t.Removed == null);
            var association = await db.SingleOrDefaultAsync<GXObjectTemplate>(tx, query, token);
            if (association == null)
            {
                throw new ArgumentException("Select an Association Logical Name from this meter's template.");
            }
            var old = await LoadKey(db, management, active[0].Id, token);
            if (old.ConcurrencyStamp != active[0].ConcurrencyStamp || old.State != KeyState.Active)
            {
                throw new InvalidOperationException("The current password changed. Reload and retry.");
            }
            byte[] material;
            if (request.RestoreChangeId is Guid restoreId)
            {
                if (request.NewPassword is { Length: > 0 })
                {
                    throw new ArgumentException("A restore request cannot include a new password.");
                }
                var earlier = await db.SingleOrDefaultAsync<GXPasswordChangeRecord>(tx,
                    GXSelectArgs.SelectAll<GXPasswordChangeRecord>(r => r.Id == restoreId &&
                        r.DeviceId == device.Id && r.ManagementId == management.Id && r.State == "Verified"), token)
                    ?? throw new ArgumentException("A verified change for this meter is required.");
                var previous = await LoadKey(db, management, earlier.SourceKeyId, token);
                if (previous.State is not (KeyState.Active or KeyState.Deactivated) ||
                    previous.CompromiseDate != null || previous.DestroyDate != null)
                {
                    throw new UnauthorizedAccessException("The previous password is not available for recovery.");
                }
                GXKeyPolicy.ValidateExport(previous);
                material = await crypto.DecryptAsync(previous.Key ?? throw new InvalidOperationException("Previous password material is unavailable."));
            }
            else
            {
                material = request.NewPassword?.ToArray() ?? throw new ArgumentException("Enter the new password.");
            }
            try
            {
                if (material.Length is < 1 or > 1024)
                {
                    throw new ArgumentException("Password length must be between 1 and 1024 bytes.");
                }
                var oldMaterial = await crypto.DecryptAsync(old.Key ?? throw new InvalidOperationException("Current password material is unavailable."));
                try
                {
                    if (CryptographicOperations.FixedTimeEquals(oldMaterial, material))
                    {
                        throw new ArgumentException("The new password is already in use.");
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(oldMaterial);
                }
                var pending = new GXKeyManagementKey
                {
                    Id = Guid.NewGuid(),
                    KeyManagement = management,
                    KeyType = KeyManagementType.LLSPassword,
                    Key = material
                };
                GXKeyPolicy.InitializeLegacy(pending);
                pending.State = KeyState.PreActive;
                pending.ActivationDate = null;
                pending.CreationTime = DateTimeOffset.UtcNow;
                pending.ConcurrencyStamp = Guid.NewGuid().ToString();
                pending.Key = await crypto.EncryptAsync(material);
                var operation = new GXPasswordChangeRecord
                {
                    Id = Guid.NewGuid(),
                    TaskId = Guid.NewGuid(),
                    DeviceId = device.Id,
                    ManagementId = management.Id,
                    SourceKeyId = old.Id,
                    TargetKeyId = pending.Id,
                    RequestedBy = UserId(actor),
                    State = "Queued",
                    Created = DateTimeOffset.UtcNow,
                    AssociationLogicalName = request.AssociationLogicalName,
                    RestoredFromId = request.RestoreChangeId, ProfileHash = ProfileHash(device)
                };
                // A unique device primary key prevents simultaneous changes across server processes.
                await db.InsertAsync(tx, GXInsertArgs.Insert(new GXPasswordChangeLock { Id = device.Id, OperationId = operation.Id }), token);
                await db.InsertAsync(tx, GXInsertArgs.Insert(pending), token);
                await db.InsertAsync(tx, GXInsertArgs.Insert(operation), token);
                await db.InsertAsync(tx, GXInsertArgs.Insert(new GXTask
                {
                    Id = operation.TaskId,
                    TargetDevice = device.Id,
                    TaskType = TaskType.Write,
                    CreationTime = operation.Created,
                    Creator = new GXUser { Id = UserId(actor) },
                    Target = "Device password change",
                    Data = JsonSerializer.Serialize(new { PasswordChangeId = operation.Id })
                }), token);
                await tx.CommitAsync(token);
                result = Info(operation);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(material);
            }
        }, token);
        return result;
    }

    /// <summary>Lists valid Association LN targets without exposing template or key material.</summary>
    public async Task<IReadOnlyList<GXPasswordAssociation>> AssociationsAsync(ClaimsPrincipal actor, Guid deviceId, CancellationToken token)
    {
        await authorization.EnsureAnyPolicyAsync(actor, GXKeyPermissions.Lifecycle);
        var device = await devices.ReadAsync(deviceId, null, token);
        var templateId = device.Template?.Id ?? throw new ArgumentException("The meter template is unavailable.");
        await using var db = await factory.GetConnectionAsync(token);
        var query = GXSelectArgs.Select<GXObjectTemplate>(o => new { o.Id, o.LogicalName, o.Name },
            o => o.ObjectType == 15 && o.Removed == null);
        query.Joins.AddInnerJoin<GXObjectTemplate, GXDeviceTemplate>(o => o.DeviceTemplate!, t => t.Id);
        query.Where.And<GXDeviceTemplate>(t => t.Id == templateId && t.Removed == null);
        var rows = await db.SelectAsync<GXObjectTemplate>(query, token);
        return rows.Where(o => o.LogicalName != null).Select(o => new GXPasswordAssociation(o.LogicalName!, o.Name)).ToArray();
    }

    /// <summary>Cancels an operation only before a meter write may have started.</summary>
    public async Task CancelAsync(ClaimsPrincipal actor, Guid id, CancellationToken token)
    {
        await authorization.EnsureAnyPolicyAsync(actor, GXKeyPermissions.Lifecycle);
        await using var lookup = await factory.GetConnectionAsync(token);
        var initial = await lookup.SingleOrDefaultAsync<GXPasswordChangeRecord>(
            GXSelectArgs.SelectAll<GXPasswordChangeRecord>(r => r.Id == id), token)
            ?? throw new ArgumentException("Unknown password change.");
        var management = await keys.ReadAsync(initial.ManagementId, null, token);
        await GXHistoryMutation.RunAsync<GXKeyManagement>(factory, actor, services, [management], async () =>
        {
            await using var db = await factory.GetHistoryConnectionAsync(token, notifySqlExcution: false);
            await using var tx = await db.BeginTransactionAsync(token);
            var operation = await db.SingleOrDefaultAsync<GXPasswordChangeRecord>(tx,
                GXSelectArgs.SelectAll<GXPasswordChangeRecord>(r => r.Id == id), token)!;
            if (operation?.State != "Queued")
            {
                throw new InvalidOperationException("A write may have started. Verify the meter instead of cancelling.");
            }
            var target = await LoadKey(db, management, operation.TargetKeyId, token);
            GXKeyPolicy.ValidateTransition(target.State!.Value, KeyState.Destroyed);
            target.State = KeyState.Destroyed;
            target.DestroyDate = DateTimeOffset.UtcNow;
            target.Key = null;
            await db.UpdateAsync(tx, GXUpdateArgs.Update(target, k => new { k.State, k.DestroyDate, k.Key }), token);
            operation.State = "Cancelled";
            await db.UpdateAsync(tx, GXUpdateArgs.Update(operation, r => r.State!), token);
            var task = new GXTask { Id = operation.TaskId, Ready = DateTimeOffset.UtcNow, Result = "Password change cancelled." };
            await db.UpdateAsync(tx, GXUpdateArgs.Update(task, t => new { t.Ready, t.Result }), token);
            await db.DeleteAsync(tx, GXDeleteArgs.Delete<GXPasswordChangeLock>(r => r.Id == operation.DeviceId && r.OperationId == id), token);
            await tx.CommitAsync(token);
        }, token);
    }

    /// <summary>Releases ephemeral material only to the persisted claiming agent.</summary>
    public async Task<GXPasswordChangeWork> WorkAsync(ClaimsPrincipal actor, Guid id, GXPasswordChangeClaim claim, CancellationToken token)
    {
        await using var db = await factory.GetConnectionAsync(token, notifySqlExcution: false);
        var operation = await Claimed(db, actor, id, claim, token);
        var device = await devices.ReadAsync(operation.DeviceId, null, token);
        if (operation.State != "Verified" && operation.ProfileHash != ProfileHash(device))
        {
            throw new InvalidOperationException("The meter connection profile changed. Restore its configuration before retrying this operation.");
        }
        if (operation.State == "Verified")
        {
            return new GXPasswordChangeWork { Id = operation.Id, State = operation.State!, Device = device };
        }
        var management = await keys.ReadAsync(operation.ManagementId, null, token);
        ValidateManagement(operation, management);
        var current = await LoadKey(db, management, operation.SourceKeyId, token);
        var target = await LoadKey(db, management, operation.TargetKeyId, token);
        ValidateSourceForRetirement(current);
        var canUseCurrent = true;
        try
        {
            GXKeyPolicy.ValidateUse(current, GXKeyPurpose.Password, DateTimeOffset.UtcNow);
        }
        catch (UnauthorizedAccessException) when (operation.State == "Writing")
        {
            // A write may already have succeeded. Probe the target without releasing an
            // expired source credential for a fallback connection.
            canUseCurrent = false;
        }
        GXKeyPolicy.ValidateExport(current);
        GXKeyPolicy.ValidateExport(target);
        if (target.State != KeyState.PreActive || target.CompromiseDate != null || target.Removed != null)
        {
            throw new InvalidOperationException("The pending password is no longer available.");
        }
        var currentMaterial = canUseCurrent ? await crypto.DecryptAsync(current.Key!) : [];
        try
        {
            var targetMaterial = await crypto.DecryptAsync(target.Key!);
            return new GXPasswordChangeWork
            {
                Id = operation.Id,
                State = operation.State!,
                Device = device,
                AssociationLogicalName = operation.AssociationLogicalName!,
                CurrentPassword = currentMaterial,
                NewPassword = targetMaterial
            };
        }
        catch
        {
            CryptographicOperations.ZeroMemory(currentMaterial);
            throw;
        }
    }

    /// <summary>Records write intent before any physical mutation.</summary>
    public async Task MarkWritingAsync(ClaimsPrincipal actor, Guid id, GXPasswordChangeClaim claim, CancellationToken token)
    {
        await GXHistoryScope.RunAsync(factory, async db =>
        {
            var operation = await Claimed(db, actor, id, claim, token);
            if (operation.State is not ("Queued" or "Writing"))
            {
                throw new InvalidOperationException("This password change cannot start writing.");
            }
            var management = await keys.ReadAsync(operation.ManagementId, null, token);
            ValidateManagement(operation, management);
            var device = await devices.ReadAsync(operation.DeviceId, null, token);
            if (operation.ProfileHash != ProfileHash(device))
            {
                throw new InvalidOperationException("The meter connection profile changed during the password operation.");
            }
            var current = await LoadKey(db, management, operation.SourceKeyId, token);
            var target = await LoadKey(db, management, operation.TargetKeyId, token);
            GXKeyPolicy.ValidateUse(current, GXKeyPurpose.Password, DateTimeOffset.UtcNow);
            if (target.State != KeyState.PreActive || target.CompromiseDate != null || target.DestroyDate != null)
            {
                throw new InvalidOperationException("The pending password is no longer available.");
            }
            if (operation.State == "Writing")
            {
                return;
            }
            operation.State = "Writing";
            await db.UpdateAsync(GXUpdateArgs.Update(operation, r => r.State!), token);
        }, token);
    }

    /// <summary>Activates the verified target and retains the encrypted source atomically.</summary>
    public async Task ConfirmAsync(ClaimsPrincipal actor, Guid id, GXPasswordChangeClaim claim, CancellationToken token)
    {
        await using var lookup = await factory.GetConnectionAsync(token);
        var initial = await Claimed(lookup, actor, id, claim, token);
        if (initial.State == "Verified")
        {
            return;
        }
        var requestedBy = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, initial.RequestedBy!)], "PasswordChange"));
        await GXHistoryMutation.RunAsync<GXKeyManagement>(factory, requestedBy, services,
            [new GXKeyManagement { Id = initial.ManagementId }], async () =>
        {
            await using var db = await factory.GetHistoryConnectionAsync(token, notifySqlExcution: false);
            var operation = await Claimed(db, actor, id, claim, token);
            if (operation.State == "Verified")
            {
                return;
            }
            if (operation.State != "Writing")
            {
                throw new InvalidOperationException("The write intent was not recorded.");
            }
            var management = await keys.ReadAsync(operation.ManagementId, null, token);
            ValidateManagement(operation, management);
            var device = await devices.ReadAsync(operation.DeviceId, null, token);
            if (operation.ProfileHash != ProfileHash(device))
            {
                throw new InvalidOperationException("The meter connection profile changed during the password operation.");
            }
            var current = await LoadKey(db, management, operation.SourceKeyId, token);
            var target = await LoadKey(db, management, operation.TargetKeyId, token);
            ValidateSourceForRetirement(current);
            if (target.State != KeyState.PreActive || target.CompromiseDate != null || target.DestroyDate != null)
            {
                throw new InvalidOperationException("The pending password changed.");
            }
            await using var tx = await db.BeginTransactionAsync(token);
            GXKeyPolicy.ValidateTransition(current.State!.Value, KeyState.Deactivated);
            GXKeyPolicy.ValidateTransition(target.State!.Value, KeyState.Active);
            current.State = KeyState.Deactivated;
            current.DeactivationDate = DateTimeOffset.UtcNow;
            target.State = KeyState.Active;
            target.ActivationDate = DateTimeOffset.UtcNow;
            await db.UpdateAsync(tx, GXUpdateArgs.Update(current, k => new { k.State, k.DeactivationDate }), token);
            await db.UpdateAsync(tx, GXUpdateArgs.Update(target, k => new { k.State, k.ActivationDate }), token);
            operation.State = "Verified";
            operation.Verified = DateTimeOffset.UtcNow;
            await db.UpdateAsync(tx, GXUpdateArgs.Update(operation, r => new { r.State, r.Verified }), token);
            await db.DeleteAsync(tx, GXDeleteArgs.Delete<GXPasswordChangeLock>(r => r.Id == operation.DeviceId && r.OperationId == id), token);
            await db.InsertAsync(tx, GXInsertArgs.Insert(new GXKeyAudit
            {
                Id = Guid.NewGuid(),
                KeyId = target.Id,
                UserId = UserId(actor),
                Operation = "PasswordChangeVerified:" + id,
                Time = DateTimeOffset.UtcNow
            }), token);
            await tx.CommitAsync(token);
        }, token);
    }

    /// <summary>Requeues a completed failed claim without forgetting an uncertain write.</summary>
    public async Task RetryAsync(ClaimsPrincipal actor, Guid id, CancellationToken token)
    {
        await authorization.EnsureAnyPolicyAsync(actor, GXKeyPermissions.Lifecycle);
        await GXHistoryScope.RunAsync(factory, async db =>
        {
            var operation = await db.SingleOrDefaultAsync<GXPasswordChangeRecord>(GXSelectArgs.SelectAll<GXPasswordChangeRecord>(r => r.Id == id), token)
                ?? throw new ArgumentException("Unknown password change.");
            _ = await keys.ReadAsync(operation.ManagementId, null, token);
            if (operation.State is not ("Queued" or "Writing"))
            {
                throw new InvalidOperationException("This password change cannot be retried.");
            }
            var task = await db.SingleOrDefaultAsync<GXTask>(GXSelectArgs.SelectAll<GXTask>(t => t.Id == operation.TaskId), token)
                ?? throw new InvalidOperationException("The operation task was deleted.");
            if (task.Ready == null && task.Start != null)
            {
                throw new InvalidOperationException("The operation task is still running.");
            }
            task.Start = task.Ready = null;
            task.OperatingAgent = null;
            task.Result = null;
            task.Data = JsonSerializer.Serialize(new { PasswordChangeId = operation.Id });
            await db.UpdateAsync(GXUpdateArgs.Update(task, t => new { t.Start, t.Ready, t.OperatingAgent, t.Result, t.Data }), token);
        }, token);
    }

    private async Task<GXPasswordChangeRecord> Claimed(GXDbConnection db, ClaimsPrincipal actor, Guid id,
        GXPasswordChangeClaim claim, CancellationToken token)
    {
        if (!GXDeviceSettingsKeys.CanUse(actor))
        {
            throw new UnauthorizedAccessException("Key use permission is required.");
        }
        var operation = await db.SingleOrDefaultAsync<GXPasswordChangeRecord>(
            GXSelectArgs.SelectAll<GXPasswordChangeRecord>(r => r.Id == id && r.TaskId == claim.TaskId), token)
            ?? throw new ArgumentException("Unknown password change.");
        var query = GXSelectArgs.Select<GXTask>(t => new { t.Id, t.Start, t.Ready, t.TargetDevice }, t => t.Id == claim.TaskId);
        query.Joins.AddInnerJoin<GXTask, GXAgent>(t => t.OperatingAgent!, a => a.Id);
        query.Columns.Add<GXAgent>(a => a.Id);
        var task = await db.SingleOrDefaultAsync<GXTask>(query, token);
        if (task?.OperatingAgent?.Id != claim.AgentId || task.Start != claim.Start || task.TargetDevice != operation.DeviceId ||
            task.Ready != null && operation.State != "Verified")
        {
            throw new UnauthorizedAccessException("The password change belongs to a different or expired task claim.");
        }
        var users = GXRepositoryUsers.Query(typeof(GXAgent), claim.AgentId);
        var userId = UserId(actor);
        users.Where.And<GXUser>(u => u.Id == userId);
        if ((await db.SelectAsync<GXUser>(users, token)).Count == 0)
        {
            throw new UnauthorizedAccessException("The agent is not accessible.");
        }
        _ = await keys.ReadAsync(operation.ManagementId, null, token);
        return operation;
    }

    private static async Task<GXKeyManagementKey> LoadKey(GXDbConnection db, GXKeyManagement management, Guid id, CancellationToken token)
    {
        var key = await db.SingleOrDefaultAsync<GXKeyManagementKey>(
            GXSelectArgs.SelectAll<GXKeyManagementKey>(k => k.Id == id && k.KeyManagement == management && k.Removed == null), token)
            ?? throw new InvalidOperationException("The key is unavailable.");
        key.KeyManagement = management;
        return key;
    }

    private static void ValidateSourceForRetirement(GXKeyManagementKey current)
    {
        if (current.State != KeyState.Active || current.Removed != null || current.DestroyDate != null ||
            current.CompromiseDate <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("The source password lifecycle changed during the operation.");
        }
    }

    private static void ValidateManagement(GXPasswordChangeRecord operation, GXKeyManagement management)
    {
        if (management.Device?.Id != operation.DeviceId || management.Removed != null ||
            !string.IsNullOrEmpty(management.ProviderId) && management.ProviderId != "local")
        {
            throw new InvalidOperationException("The key management binding changed during the password operation.");
        }
    }

    private static string ProfileHash(GXDevice device)
    {
        var json = JsonSerializer.Serialize(new
        {
            device.MediaType, device.MediaSettings, device.Settings,
            TemplateId = device.Template?.Id, TemplateSettings = device.Template?.Settings
        });
        return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json)));
    }

    private static GXPasswordChangeInfo Info(GXPasswordChangeRecord row) => new()
    {
        Id = row.Id,
        DeviceId = row.DeviceId,
        ManagementId = row.ManagementId,
        TaskId = row.TaskId,
        SourceKeyId = row.SourceKeyId,
        TargetKeyId = row.TargetKeyId,
        RestoredFromId = row.RestoredFromId,
        State = row.State!,
        Created = row.Created,
        Verified = row.Verified
    };
}