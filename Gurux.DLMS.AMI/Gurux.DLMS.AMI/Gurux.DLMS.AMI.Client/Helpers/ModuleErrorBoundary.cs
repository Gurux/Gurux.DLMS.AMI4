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

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Gurux.DLMS.AMI.Client.Helpers;

public sealed class ModuleErrorBoundary : ErrorBoundary
{
    [Parameter] public string ModuleId { get; set; } = "";
    [Inject] public ModuleLoader Loader { get; set; } = default!;
    [Inject] public ILogger<ModuleErrorBoundary> Logger { get; set; } = default!;
    protected override Task OnErrorAsync(Exception exception)
    {
        Logger.LogError(exception, "Module {Id} UI failed and was disabled for this content version.", ModuleId);
        Loader.DisableClientModule(ModuleId, exception);
        return Task.CompletedTask;
    }
}
