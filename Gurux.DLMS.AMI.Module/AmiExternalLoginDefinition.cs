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

using System.Text.Json.Serialization;
using Gurux.DLMS.AMI.Module.Enums;

namespace Gurux.DLMS.AMI.Module;

/// <summary>Server-only runtime configuration of one external login service.</summary>
/// <remarks>
/// This is a declaration, not a credential validator or a public settings DTO. The host must
/// validate the protocol, required endpoints, client configuration and requested scopes,
/// namespace scheme identifiers by the owning module, and copy the definition before use.
/// OAuth claim mapping and OIDC token validation belong to the host protocol adapter.
/// Keep persisted secrets in private module settings rather than serializing this descriptor.
/// </remarks>
public sealed class AmiExternalLoginDefinition
{
    /// <summary>Stable login-service identifier within the owning module, for example github.</summary>
    public required string Id { get; init; }

    /// <summary>Text displayed on the host's login button.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Authentication protocol; defaults to OpenID Connect.</summary>
    public AmiExternalLoginProtocol Protocol { get; init; } = AmiExternalLoginProtocol.OpenIdConnect;

    /// <summary>Absolute authority URI used for OpenID Connect discovery.</summary>
    public Uri? Authority { get; init; }

    /// <summary>Absolute authorization endpoint for an OAuth 2.0 service.</summary>
    public Uri? AuthorizationEndpoint { get; init; }

    /// <summary>Absolute token endpoint for an OAuth 2.0 service.</summary>
    public Uri? TokenEndpoint { get; init; }

    /// <summary>Absolute user-information endpoint for an OAuth 2.0 service.</summary>
    public Uri? UserInfoEndpoint { get; init; }

    /// <summary>Client identifier registered with the external service.</summary>
    public required string ClientId { get; init; }

    /// <summary>Optional server-side client secret; excluded from System.Text.Json serialization.</summary>
    [JsonIgnore]
    public string? ClientSecret { get; init; }

    /// <summary>Protocol scopes requested by the host; the host validates and snapshots this list.</summary>
    public IReadOnlyList<string> Scopes { get; init; } = Array.Empty<string>();
}
