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

/// <summary>Supplies server-only external login definitions owned by a module.</summary>
/// <remarks>
/// Register one concrete scoped implementation with
/// <see cref="AmiModuleBuilder.AddExternalLoginProvider{T}"/>. One implementation can return
/// several login services. The host must resolve it within a module-owned service scope and
/// pass cancellation for the invocation and module lifetime. Returning definitions does not
/// authenticate a request or register an ASP.NET authentication scheme by itself.
/// </remarks>
public interface IGXAmiExternalLoginProvider
{
    /// <summary>Reads the currently configured external login services.</summary>
    /// <param name="cancellationToken">Cancellation for the invocation and module lifetime.</param>
    /// <returns>Definitions for the host to validate and snapshot before using them.</returns>
    /// <remarks>Do not return these definitions through a client-facing API or log their credentials.</remarks>
    Task<IReadOnlyList<AmiExternalLoginDefinition>> GetProvidersAsync(CancellationToken cancellationToken);
}
