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

/// <summary>Discovers template tokens and replaces bracketed tokens using registered providers.</summary>
public interface IGXAmiTemplateTokenResolver
{
    /// <summary>Lists tokens available from registered providers in the specified context.</summary>
    /// <param name="context">User, culture and optional target used to determine available tokens.</param>
    /// <param name="cancellationToken">Token used to cancel discovery.</param>
    /// <returns>Definitions of tokens available in this context.</returns>
    Task<IReadOnlyList<AmiTemplateTokenDefinition>> GetDefinitionsAsync(
        AmiTemplateTokenContext context, CancellationToken cancellationToken = default);

    /// <summary>Replaces bracketed tokens, for example [site:name], with their plain text values.</summary>
    /// <param name="text">Template text containing bracketed tokens.</param>
    /// <param name="context">User, culture and optional target used for resolution.</param>
    /// <param name="cancellationToken">Token used to cancel resolution.</param>
    /// <returns>The resolved text with unknown or unavailable tokens left unchanged.</returns>
    /// <remarks>Output encoding belongs to the caller and must match the destination, for example HTML or plain text.</remarks>
    Task<string> ResolveAsync(string text, AmiTemplateTokenContext context,
        CancellationToken cancellationToken = default);
}
