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

using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Client.Shared.KeyManagement;

/// <summary>Reference to an object owned by a named external provider connection.</summary>
/// <param name="ProviderId">External provider identifier.</param>
/// <param name="ConnectionId">Configured provider connection owning the object.</param>
/// <param name="ObjectId">Object identifier within that provider connection.</param>
public sealed record GXKeyReference(string ProviderId, string ConnectionId, string ObjectId);
/// <summary>Explicit identity and intent; never inferred from a TCP request's HTTP context.</summary>
/// <param name="Principal">Authenticated caller whose permissions authorize the operation.</param>
/// <param name="Purpose">Exact operation for which material is requested.</param>
/// <param name="AllowRecovery">Whether recovery with a deactivated or compromised key is requested. Defaults to false.</param>
public sealed record GXKeyOperationContext(ClaimsPrincipal Principal, GXKeyPurpose Purpose, bool AllowRecovery = false);
/// <summary>Material authorized for one exchange. Callers must not persist it.</summary>
/// <param name="Metadata">Metadata describing the key whose material was authorized.</param>
/// <param name="Material">Secret bytes for the authorized exchange; callers must not persist them.</param>
public sealed record GXKeyMaterial(GXKeyManagementKey Metadata, byte[] Material);
/// <summary>Stable provider contract exported by the KMIP client add-in.</summary>
public interface IKeyProvider
{
    /// <summary>Reads key metadata without requesting key material.</summary>
    /// <param name="reference">Provider, connection and object identity of the remote key.</param>
    /// <param name="token">Cancels the operation.</param>
    Task<GXKeyManagementKey> ReadMetadataAsync(GXKeyReference reference, CancellationToken token = default);
    /// <summary>Acquires key material for the explicitly authorized operation.</summary>
    /// <param name="reference">Provider, connection and object identity of the remote key.</param>
    /// <param name="purpose">Exact operation for which material is requested.</param>
    /// <param name="token">Cancels the operation.</param>
    Task<GXKeyMaterial> AcquireAsync(GXKeyReference reference, GXKeyPurpose purpose, CancellationToken token = default);
    /// <summary>Activates the referenced provider key.</summary>
    /// <param name="reference">Provider, connection and object identity of the remote key.</param>
    /// <param name="token">Cancels the operation.</param>
    Task ActivateAsync(GXKeyReference reference, CancellationToken token = default);
    /// <summary>Revokes the referenced provider key, optionally recording a compromise.</summary>
    /// <param name="reference">Provider, connection and object identity of the remote key.</param>
    /// <param name="compromised">True when revocation is caused by a compromise.</param>
    /// <param name="occurrenceDate">Time of the lifecycle event, or null to use the service default.</param>
    /// <param name="token">Cancels the operation.</param>
    Task RevokeAsync(GXKeyReference reference, bool compromised, DateTimeOffset? occurrenceDate, CancellationToken token = default);
    /// <summary>Destroys the referenced provider key material.</summary>
    /// <param name="reference">Provider, connection and object identity of the remote key.</param>
    /// <param name="token">Cancels the operation.</param>
    Task DestroyAsync(GXKeyReference reference, CancellationToken token = default);
}
/// <summary>Authoritative host service shared by HTTP, DLMS and the KMIP server add-in.</summary>
public interface IKeyService
{
    /// <summary>Reads key metadata without requesting key material.</summary>
    /// <param name="principal">Authenticated caller whose access is checked.</param>
    /// <param name="keyId">Unique AMI key identity.</param>
    /// <param name="token">Cancels the operation.</param>
    Task<GXKeyManagementKey> ReadMetadataAsync(ClaimsPrincipal principal, Guid keyId, CancellationToken token = default);
    /// <summary>Lists metadata visible to the caller, optionally restricted to one management group.</summary>
    /// <param name="principal">Authenticated caller whose access is checked.</param>
    /// <param name="managementId">Optional management group filter; null includes all groups visible to the caller.</param>
    /// <param name="token">Cancels the operation.</param>
    Task<IReadOnlyList<GXKeyManagementKey>> LocateAsync(ClaimsPrincipal principal, Guid? managementId = null, CancellationToken token = default);
    /// <summary>Acquires key material for the explicitly authorized operation.</summary>
    /// <param name="context">Caller identity, operation purpose and explicit recovery intent.</param>
    /// <param name="keyId">Unique AMI key identity.</param>
    /// <param name="token">Cancels the operation.</param>
    Task<GXKeyMaterial> AcquireAsync(GXKeyOperationContext context, Guid keyId, CancellationToken token = default);
    /// <summary>Exports key material after checking export permission and extractability.</summary>
    /// <param name="principal">Authenticated caller whose access is checked.</param>
    /// <param name="keyId">Unique AMI key identity.</param>
    /// <param name="token">Cancels the operation.</param>
    Task<GXKeyMaterial> ExportAsync(ClaimsPrincipal principal, Guid keyId, CancellationToken token = default);
    /// <summary>Registers a key in a management group after validating its metadata and material.</summary>
    /// <param name="principal">Authenticated caller whose access is checked.</param>
    /// <param name="managementId">Key management group that owns the registration.</param>
    /// <param name="key">Key metadata and material to register.</param>
    /// <param name="token">Cancels the operation.</param>
    Task<GXKeyManagementKey> RegisterAsync(ClaimsPrincipal principal, Guid managementId, GXKeyManagementKey key, CancellationToken token = default);
    /// <summary>Registers an existing provider object under a DLMS role without storing a local material copy.</summary>
    /// <param name="principal">Authenticated caller whose access is checked.</param>
    /// <param name="managementId">Key management group that owns the registration.</param>
    /// <param name="objectId">Remote object identifier within the configured provider connection.</param>
    /// <param name="role">Single DLMS role assigned to the imported key.</param>
    /// <param name="token">Cancels the operation.</param>
    Task<GXKeyManagementKey> ImportExternalAsync(ClaimsPrincipal principal, Guid managementId, string objectId, Gurux.DLMS.AMI.Shared.DTOs.Enums.KeyManagementType role, CancellationToken token = default);
    /// <summary>Registers corresponding private and public keys as a key pair.</summary>
    /// <param name="principal">Authenticated caller whose access is checked.</param>
    /// <param name="managementId">Key management group that owns the registration.</param>
    /// <param name="privateKey">Private key metadata and material to register.</param>
    /// <param name="publicKey">Matching public key metadata and material to register.</param>
    /// <param name="token">Cancels the operation.</param>
    Task<(GXKeyManagementKey Private, GXKeyManagementKey Public)> RegisterPairAsync(ClaimsPrincipal principal, Guid managementId, GXKeyManagementKey privateKey, GXKeyManagementKey publicKey, CancellationToken token = default);
    /// <summary>Applies an explicit lifecycle transition with a concurrency check.</summary>
    /// <param name="principal">Authenticated caller whose access is checked.</param>
    /// <param name="keyId">Unique AMI key identity.</param>
    /// <param name="target">Requested lifecycle state.</param>
    /// <param name="expectedStamp">Expected concurrency stamp, or null when none was supplied.</param>
    /// <param name="occurrenceDate">Time of the lifecycle event, or null to use the service default.</param>
    /// <param name="token">Cancels the operation.</param>
    Task<GXKeyManagementKey> TransitionAsync(ClaimsPrincipal principal, Guid keyId, KeyState target, string? expectedStamp, DateTimeOffset? occurrenceDate = null, CancellationToken token = default);
    /// <summary>Records that a key has been installed on a device.</summary>
    /// <param name="principal">Authenticated caller whose access is checked.</param>
    /// <param name="deviceId">Device whose confirmed key deployment is read or updated.</param>
    /// <param name="keyId">Unique AMI key identity.</param>
    /// <param name="token">Cancels the operation.</param>
    Task ConfirmDeploymentAsync(ClaimsPrincipal principal, Guid deviceId, Guid keyId, CancellationToken token = default);
    /// <summary>Lists key identities confirmed as installed on the device.</summary>
    /// <param name="principal">Authenticated caller whose access is checked.</param>
    /// <param name="deviceId">Device whose confirmed key deployment is read or updated.</param>
    /// <param name="token">Cancels the operation.</param>
    Task<IReadOnlyList<Guid>> GetDeployedKeysAsync(ClaimsPrincipal principal, Guid deviceId, CancellationToken token = default);
}
/// <summary>Resolves an enabled AMI user and current permissions for certificate-authenticated requests.</summary>
public interface IKeyIdentityResolver
{
    /// <summary>Resolves a certificate-associated user to an enabled AMI principal with current permissions.</summary>
    /// <param name="userId">AMI user identifier associated with the authenticated certificate.</param>
    /// <param name="token">Cancels the operation.</param>
    Task<ClaimsPrincipal> ResolveAsync(string userId, CancellationToken token = default);
}
/// <summary>Additional permissions distinguish metadata visibility, material and lifecycle operations.</summary>
public static class GXKeyPermissions
{
    /// <summary>Permission to acquire material for an authorized operation.</summary>
    public const string Use = "key-management.use";
    /// <summary>Permission to export extractable key material.</summary>
    public const string Export = "key-management.export";
    /// <summary>Permission to change key registration or lifecycle state.</summary>
    public const string Lifecycle = "key-management.lifecycle";
    /// <summary>Permission to request recovery with a deactivated or compromised key.</summary>
    public const string Recovery = "key-management.recovery";
}
/// <summary>Wire request for an authorized key acquisition.</summary>
public sealed class GXAcquireKeyRequest
{
    /// <summary>Gets or sets the identity of the key targeted by this request.</summary>
    public Guid KeyId { get; set; }
    /// <summary>Gets or sets the exact operation requiring key material.</summary>
    public GXKeyPurpose Purpose { get; set; }
    /// <summary>Gets or sets whether recovery with a deactivated or compromised key is requested. Defaults to false.</summary>
    public bool AllowRecovery { get; set; }
}
/// <summary>Wire request for an explicit lifecycle mutation.</summary>
public sealed class GXTransitionKeyRequest
{
    /// <summary>Gets or sets the identity of the key targeted by this request.</summary>
    public Guid KeyId { get; set; }
    /// <summary>Gets or sets the requested key lifecycle state.</summary>
    public KeyState Target { get; set; }
    /// <summary>Gets or sets the expected concurrency stamp, or null when none is supplied.</summary>
    public string? ExpectedStamp { get; set; }
    /// <summary>Gets or sets the lifecycle event timestamp, or null to use the service default.</summary>
    public DateTimeOffset? OccurrenceDate { get; set; }
}
/// <summary>Binds an existing remote object to an AMI DLMS role without copying its material.</summary>
public sealed class GXImportExternalKeyRequest
{
    /// <summary>Gets or sets the key management group that will own the external reference.</summary>
    public Guid ManagementId { get; set; }
    /// <summary>Gets or sets the remote provider object identifier. Defaults to an empty string.</summary>
    public string ObjectId { get; set; } = "";
    /// <summary>Gets or sets the single DLMS role assigned to the external object.</summary>
    public Gurux.DLMS.AMI.Shared.DTOs.Enums.KeyManagementType Role { get; set; }
}

