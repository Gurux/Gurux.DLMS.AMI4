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

using System.Text.Json;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Services;

/// <summary>Reads current proxy settings so administrative changes apply to the next request.</summary>
public sealed class GXHttpProxyConfiguration(IDbConnectionFactory factory)
{
    /// <summary>Gets persisted proxy settings, failing closed when missing or disabled.</summary>
    public async Task<HttpProxySettings> ReadAsync(CancellationToken cancellationToken)
    {
        await using var connection = await factory.GetConnectionAsync(cancellationToken);
        var configuration = await connection.SingleOrDefaultAsync<GXConfiguration>(
            GXSelectArgs.SelectAll<GXConfiguration>(c => c.Name == GXConfigurations.HttpProxy), cancellationToken);
        if (configuration?.Enabled != true || string.IsNullOrWhiteSpace(configuration.Settings))
        {
            return new() { Enabled = false };
        }
        return JsonSerializer.Deserialize<HttpProxySettings>(configuration.Settings) ?? new() { Enabled = false };
    }
}
