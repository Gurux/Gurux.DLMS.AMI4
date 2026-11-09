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

using Gurux.DLMS.AMI.Module.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Module;

namespace Gurux.DLMS.AMI.Client.Pages.Module;

/// <summary>One mapping for installation labels, saved deployment choice and runtime icon styling.</summary>
public sealed record GXModulePresentation(string Installation, string Enabled, string Runtime, string Color, string CssClass)
{
    /// <summary>Accessible description of all three independent states.</summary>
    public string Description => $"{Installation}; Enabled: {Enabled}; Runtime: {Runtime}";

    /// <summary>Derives a presentation without inferring runtime state from the saved choice.</summary>
    public static GXModulePresentation For(GXModule module)
    {
        var installed = (module.Status.GetValueOrDefault() & (ModuleStatus.Installed | ModuleStatus.CustomBuild)) != 0;
        var installation = module.Status switch
        {
            null => "Unknown installation state",
            var status when (status & ModuleStatus.CustomBuild) != 0 => "Custom build",
            var status when (status & ModuleStatus.Installed) != 0 => "Installed",
            _ => "Installable"
        };
        var enabled = module.Enabled switch { true => "Yes", false => "No", null => "Unspecified" };
        var state = Decode(module.RuntimeState);
        var runtime = state?.ToString() ?? "Not loaded";
        var style = Style(state, module.Enabled == true && installed);
        return new(installation, enabled, runtime, style.Color, style.CssClass);
    }

    /// <summary>Returns the host-defined color for a module lifecycle state.</summary>
    public static string IconColor(AmiModuleState? state, bool enabled = true) => Style(state, enabled).Color;

    private static AmiModuleState? Decode(byte? state) => state.HasValue && Enum.IsDefined(typeof(AmiModuleState), state.Value)
        ? (AmiModuleState)state.Value
        : null;

    private static (string Color, string CssClass) Style(AmiModuleState? state, bool enabled)
    {
        if (!enabled)
        {
            return ("var(--bs-secondary, #6c757d)", "module-inactive");
        }
        return state switch
        {
            AmiModuleState.Failed => ("var(--bs-danger, #dc3545)", "module-failed"),
            AmiModuleState.Loading or AmiModuleState.Stopping => ("var(--bs-warning, #ffc107)", "module-transitioning"),
            AmiModuleState.Active => ("var(--bs-success, #198754)", "module-running"),
            _ => ("var(--bs-secondary, #6c757d)", "module-inactive")
        };
    }
}
