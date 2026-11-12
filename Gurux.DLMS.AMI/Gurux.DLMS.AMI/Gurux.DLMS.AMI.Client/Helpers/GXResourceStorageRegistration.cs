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

using Gurux.DLMS.AMI.Client.Internal;
using Gurux.UI.Components;

namespace Gurux.DLMS.AMI.Client.Helpers;

public static class GXResourceStorageRegistration
{
    public static IServiceCollection AddAmiResourceStorage(this IServiceCollection services)
    {
        services.AddAmiAppearance();
        services.AddScoped<GXResourceStorage>();
        services.AddScoped<GXPolicyNameCache>();
        services.AddScoped<IGXPolicyNameProvider>(sp => sp.GetRequiredService<GXPolicyNameCache>());
        services.AddScoped<GXWalkthroughPreferences>();
        services.AddScoped<GXReadTaskNotifications>();
        services.AddScoped<IGXResourceStorage>(sp => sp.GetRequiredService<GXResourceStorage>());
        services.AddScoped<IAmiMenuStorage>(sp => sp.GetRequiredService<GXResourceStorage>());
        services.AddScoped<IAmiNavigationMenuStorage>(sp => sp.GetRequiredService<GXResourceStorage>());
        services.AddScoped<IAmiBlockStorage>(sp => sp.GetRequiredService<GXResourceStorage>());
        services.AddScoped<IAmiViewStorage>(sp => sp.GetRequiredService<GXResourceStorage>());
        return services;
    }
}
