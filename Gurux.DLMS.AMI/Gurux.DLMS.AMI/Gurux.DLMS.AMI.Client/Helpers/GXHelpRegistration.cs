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

using Gurux.UI.Components;
using Gurux.UI.Components.Help;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Connects AMI Help navigation to the COSEM documentation provider.</summary>
public static class GXHelpRegistration
{
    /// <summary>Registers the COSEM help provider and configures versioned Help links for the current client scope.</summary>
    /// <param name="services">Client services to which the help provider and help service are added.</param>
    /// <returns>The same collection so further services can be registered.</returns>
    /// <remarks>Version and culture are included in the Help URL so direct documentation links retain the requested subject.</remarks>
    public static IServiceCollection AddAmiHelp(this IServiceCollection services)
    {
        services.AddScoped<IGXHelpLinkLoader, GXAmiHelpLinkLoader>();
        services.AddScoped<IGXHelpProvider, GXCosemHelpProvider>();
        services.AddScoped<IGXHelpProvider, GXModuleHelpProvider>();
        services.AddScoped<IGXHelpProvider, GXAmiManualHelpProvider>();
        services.AddScoped(sp =>
        {
            var help = new GXHelpService(sp.GetServices<IGXHelpProvider>())
            {
                BaseAddress = "/help",
                HiddenUrls = ["/help"]
            };
            // Keep direct documentation links usable outside the current application scope.
            help.Changed += () =>
            {
                var address = "/help";
                if (help.Request?.Version is { } version)
                {
                    address += "?version=" + Uri.EscapeDataString(version);
                }
                if (help.Request?.Culture is { } culture)
                {
                    address += (address.Contains('?') ? "&" : "?") + "culture=" + Uri.EscapeDataString(culture);
                }
                help.BaseAddress = address;
            };
            return help;
        });
        return services;
    }
}
