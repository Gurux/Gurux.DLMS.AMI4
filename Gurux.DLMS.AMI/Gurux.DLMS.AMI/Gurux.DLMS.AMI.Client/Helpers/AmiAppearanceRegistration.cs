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
using Microsoft.Extensions.DependencyInjection;

namespace Gurux.DLMS.AMI.Client.Helpers;

public static class AmiAppearanceRegistration
{
    public static IServiceCollection AddAmiAppearance(this IServiceCollection services)
    {
        return services.AddGXAppearance(options =>
        {
            // Preserve AMI's light appearance unless the user explicitly changes it.
            options.Defaults.ColorMode = GXColorMode.Light;
            // Register before preferences restore; the resource provider supplies its current definition.
            options.Themes.Add(new GXThemeDefinition { Id = AmiThemeAdapter.ThemeId, Name = "AMI" });
        });
    }
}
