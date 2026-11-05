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

using Gurux.DLMS.AMI.Shared;
using System.Data;
using System.Security.Claims;
using System.Text.Json;
namespace Gurux.DLMS.AMI.Module;

/// <summary>Host-owned notification transport. Providers select authorized recipients.</summary>
public interface IGXAmiModuleEventPublisher
{
    /// <summary>Publishes a module target event to the authorized recipients.</summary>
    /// <param name="recipients">User identifiers selected and authorized by the calling provider.</param>
    /// <param name="moduleId">Identifier of the module that owns the target.</param>
    /// <param name="target">Module-defined target category.</param>
    /// <param name="targetId">Identifier of the affected target.</param>
    /// <param name="action">Module-defined action describing the change.</param>
    /// <param name="cancellationToken">Token used to cancel publication.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task PublishAsync(IReadOnlyList<string> recipients, string moduleId, string target, string targetId, string action, CancellationToken cancellationToken);
}
