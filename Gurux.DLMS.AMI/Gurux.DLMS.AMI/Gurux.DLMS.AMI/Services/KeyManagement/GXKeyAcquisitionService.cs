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
using System.Security.Claims;
using System.Security.Cryptography;
using Gurux.DLMS.AMI.Client.Shared.KeyManagement;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Server.Module;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Services.KeyManagement;

/// <summary>Authorizes and releases material separately from metadata reads.</summary>
public sealed class GXKeyAcquisitionService(IDbConnectionFactory factory, IKeyManagementRepository repository,
    IGXAmiCryptoService crypto, GXModuleRuntime runtime)
{
    /// <summary>Returns material for one authorized operation and writes a metadata-only audit.</summary>
    public Task<GXKeyMaterial> AcquireAsync(ClaimsPrincipal principal, GXAcquireKeyRequest request, CancellationToken token = default)
    {
        return AcquireCoreAsync(principal, request, false, token);
    }

    /// <summary>Temporary view-scope compatibility for keys selected by the device settings service.</summary>
    internal Task<GXKeyMaterial> AcquireDeviceKeyAsync(ClaimsPrincipal principal, GXAcquireKeyRequest request, CancellationToken token = default)
    {
        return AcquireCoreAsync(principal, request, true, token);
    }

    private async Task<GXKeyMaterial> AcquireCoreAsync(ClaimsPrincipal principal, GXAcquireKeyRequest request, bool allowLegacyDeviceView, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!GXDeviceSettingsKeys.CanUse(principal, allowLegacyDeviceView))
            {
            throw new UnauthorizedAccessException("Key use permission is required.");
        }
        if (request.AllowRecovery && !(principal.HasClaim("Permission", GXKeyPermissions.Recovery) || principal.IsInRole(GXRoles.Admin) || principal.HasClaim("Permission", GXRoles.Admin)))
            {
            throw new UnauthorizedAccessException("Key recovery permission is required.");
        }
        if (request.KeyId == Guid.Empty || !Enum.IsDefined(request.Purpose))
            {
            throw new ArgumentException("Invalid key identity or purpose.");
        }
        await using var db = await factory.GetConnectionAsync(token, notifySqlExcution: false);
        async Task<GXKeyManagementKey> Read()
        {
            var query = GXSelectArgs.SelectAll<GXKeyManagementKey>(k => k.Id == request.KeyId && k.Removed == null);
            query.Columns.Add<GXKeyManagement>(m => new { m.Id, m.ProviderId, m.ProviderConnectionId, m.Removed, m.ConcurrencyStamp });
            query.Joins.AddInnerJoin<GXKeyManagementKey, GXKeyManagement>(k => k.KeyManagement!, m => m.Id);
            return await db.SingleOrDefaultAsync<GXKeyManagementKey>(query, token)
                ?? throw new GXAmiForbiddenException(request.KeyId.ToString());
        }
        var key = await Read();
        var managementId = key.KeyManagement!.Id;
        // Reuse the repository's user/group visibility checks, rather than granting access by key ID.
        var management = await repository.ReadAsync(managementId, null, token);
        if (management.Removed != null || key.KeyManagement.Removed != null)
            {
            throw new UnauthorizedAccessException("Key management is removed.");
        }
        GXKeyPolicy.ValidateUse(key, request.Purpose, DateTimeOffset.UtcNow, request.AllowRecovery);
        GXKeyPolicy.ValidateExport(key);
        byte[]? material = null;
        try
        {
            string? provider = key.ProviderId ?? key.KeyManagement.ProviderId;
            if (string.IsNullOrEmpty(provider) || provider == "local")
            {
                if (key.Key is not { Length: > 0 }) {
                    throw new InvalidOperationException("Key material is unavailable.");
                }
                material = await crypto.DecryptAsync(key.Key);
            }
            else
            {
                var reference = new GXKeyReference(provider,
                    key.ProviderConnectionId ?? key.KeyManagement.ProviderConnectionId ?? throw new InvalidOperationException("Missing key provider connection."),
                    key.ExternalId ?? throw new InvalidOperationException("Missing external key identity."));
                var remote = await runtime.InvokeAsync<IKeyProvider, GXKeyMaterial>(
                    (service, ct) => service.AcquireAsync(reference, request.Purpose, ct), token);
                material = remote.Material;
                GXKeyPolicy.ValidateUse(remote.Metadata, request.Purpose, DateTimeOffset.UtcNow, request.AllowRecovery);
                GXKeyPolicy.ValidateExport(remote.Metadata);
                if (remote.Metadata.KeyType != key.KeyType || remote.Metadata.Algorithm != key.Algorithm ||
                    remote.Metadata.Format != key.Format || remote.Metadata.CryptographicLength != key.CryptographicLength)
                    {
                    throw new InvalidOperationException("Provider key metadata does not match the registered key.");
                }
            }
            token.ThrowIfCancellationRequested();
            // A provider request may take time; ownership can change independently of the key stamp.
            var latestManagement = await repository.ReadAsync(managementId, null, token);
            if (latestManagement.Removed != null || latestManagement.ConcurrencyStamp != management.ConcurrencyStamp)
                {
                throw new UnauthorizedAccessException("Key management access or configuration changed during acquisition.");
            }
            var current = await Read();
            if (current.KeyManagement?.Id != managementId || current.KeyManagement.Removed != null ||
                current.KeyManagement.ConcurrencyStamp != key.KeyManagement.ConcurrencyStamp || current.ConcurrencyStamp != key.ConcurrencyStamp)
                {
                throw new InvalidOperationException("Key changed during acquisition.");
            }
            GXKeyPolicy.ValidateUse(current, request.Purpose, DateTimeOffset.UtcNow, request.AllowRecovery);
            GXKeyPolicy.ValidateExport(current);
            var metadata = current.ToMetadata();
            var validation = metadata.ToMetadata(); validation.Key = material;
            GXKeyPolicy.ValidateMaterial(validation);
            await db.InsertAsync(GXInsertArgs.Insert(new GXKeyAudit
            {
                Id = Guid.NewGuid(), KeyId = key.Id, UserId = principal.FindFirstValue(ClaimTypes.NameIdentifier),
                Operation = "Acquire:" + request.Purpose, Time = DateTimeOffset.UtcNow
            }), token);
            return new(metadata, material!);
        }
        catch
        {
            if (material != null) {
                CryptographicOperations.ZeroMemory(material);
            }
            throw;
        }
    }
}


