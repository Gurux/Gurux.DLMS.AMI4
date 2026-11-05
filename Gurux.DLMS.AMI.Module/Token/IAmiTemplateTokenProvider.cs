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

namespace Gurux.DLMS.AMI.Module;

/// <summary>Provides template token definitions and values from the server or a module.</summary>
public interface IAmiTemplateTokenProvider
{
    /// <summary>Lists tokens available to the user in the specified context.</summary>
    /// <param name="context">User, culture and optional target used to determine available tokens.</param>
    /// <param name="cancellationToken">Token used to cancel discovery.</param>
    /// <returns>Definitions of tokens available in this context.</returns>
    Task<IReadOnlyList<AmiTemplateTokenDefinition>> GetDefinitionsAsync(
        AmiTemplateTokenContext context, CancellationToken cancellationToken = default);

    /// <summary>Resolves a qualified token name after checking the user's access.</summary>
    /// <param name="name">Token name without brackets, for example site:name.</param>
    /// <param name="context">User, culture and optional target used for resolution.</param>
    /// <param name="cancellationToken">Token used to cancel resolution.</param>
    /// <returns>A plain text value, or null when the token is unknown or unavailable. An empty string is a valid value.</returns>
    /// <remarks>Providers must enforce user permissions and return values without HTML encoding.</remarks>
    Task<string?> ResolveAsync(string name, AmiTemplateTokenContext context,
        CancellationToken cancellationToken = default);
}
