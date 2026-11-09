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

using Gurux.DLMS.AMI.Module;
using Microsoft.AspNetCore.Authentication;
using System.Globalization;

namespace Gurux.DLMS.AMI.Components.Account;

internal static class QrLoginFlow
{
    internal const string CookieName = "Gurux.QrLogin";

    internal static DateTimeOffset? SessionStartedAt(AuthenticationProperties? properties)
    {
        if (properties?.Items.TryGetValue("Gurux.SessionStartedUtc", out var value) == true &&
            DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var started))
        {
            return started;
        }
        // Cookies issued before this feature have no original timestamp yet.
        return properties?.IssuedUtc;
    }

    internal static async Task<AmiQrLoginFeatures> FeaturesAsync(IGXAmiModuleServices modules, CancellationToken token)
    {
        if (!modules.IsAvailable<IGXAmiQrLogin>())
        {
            return new(false, false);
        }
        try
        {
            return await modules.InvokeAsync<IGXAmiQrLogin, AmiQrLoginFeatures>((service, _) => Task.FromResult(service.GetFeatures()), token);
        }
        // The provider can stop between the availability check and invocation.
        catch (AmiModuleUnavailableException)
        {
            return new(false, false);
        }
    }

    internal static CookieOptions CookieOptions(HttpContext context) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        IsEssential = true,
        Path = context.Request.PathBase + "/Account/LoginWithQrCode",
        MaxAge = TimeSpan.FromMinutes(2)
    };
}
