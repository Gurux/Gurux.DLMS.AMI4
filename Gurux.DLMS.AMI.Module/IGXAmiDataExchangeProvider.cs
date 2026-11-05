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

/// <summary>Exports and imports module target data for a user.</summary>
public interface IGXAmiDataExchangeProvider
{
    /// <summary>Exports the requested target records for the specified user.</summary>
    /// <param name="user">User whose access determines which records can be exported.</param>
    /// <param name="target">Module-defined category of records to export.</param>
    /// <param name="ids">Optional identifiers restricting the export to selected records.</param>
    /// <param name="cancellationToken">Token used to cancel the export.</param>
    /// <returns>The exported records as JSON values.</returns>
    Task<IReadOnlyList<JsonElement>> ExportAsync(ClaimsPrincipal user, string target, IReadOnlyList<Guid>? ids, CancellationToken cancellationToken);
    /// <summary>Imports target data using the specified rule and returns the number of imported records.</summary>
    /// <param name="user">User whose permissions govern the import.</param>
    /// <param name="target">Module-defined category of records to import.</param>
    /// <param name="data">JSON payload to import.</param>
    /// <param name="rule">Import rule interpreted by the provider.</param>
    /// <param name="cancellationToken">Token used to cancel the import.</param>
    /// <returns>The number of records imported by the provider.</returns>
    Task<int> ImportAsync(ClaimsPrincipal user, string target, JsonElement data, string rule, CancellationToken cancellationToken);
}
