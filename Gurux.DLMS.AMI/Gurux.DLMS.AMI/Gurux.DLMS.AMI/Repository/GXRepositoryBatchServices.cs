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

using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Device;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Exposes authorized repository batch operations without adding contract assembly dependencies.</summary>
internal static class GXRepositoryBatchServices
{

    /// <summary>Registers authorized batched name lookup and a serializable validation/write callback.</summary>
    /// <param name="services">The host service registrations shared by authenticated request scopes.</param>
    internal static void Register(IServiceCollection services)
    {
        services.AddKeyedScoped<Func<string[], CancellationToken, Task<GXDevice[]>>>(
            "Gurux.AMI.Device.LookupNames", (provider, _) =>
                (names, token) => ((DeviceRepository)provider.GetRequiredService<IDeviceRepository>()).ListByNamesAsync(names, token));
        services.AddKeyedScoped<Func<Func<CancellationToken, Task<IEnumerable<Guid>>>, Action<Gurux.DLMS.AMI.Shared.DTOs.GXTarget>, CancellationToken, Task<IEnumerable<Guid>>>>(
            "Gurux.AMI.Repository.Write", (provider, _) => async (operation, transform, token) =>
            {
                using var notifications = new GXNotificationMutationScope(transform);
                return await GXHistoryScope.RunAsync(provider.GetRequiredService<IDbConnectionFactory>(),
                    _ => operation(token), token);
            });
    }
}
