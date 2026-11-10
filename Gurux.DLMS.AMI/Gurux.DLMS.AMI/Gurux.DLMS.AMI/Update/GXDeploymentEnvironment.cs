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

namespace Gurux.DLMS.AMI.Update;

/// <summary>Reports the host environment; update availability still requires a configured independent agent.</summary>
internal static class GXDeploymentEnvironment
{
    internal static string Detect() => Detect(OperatingSystem.IsLinux(),
        Environment.GetEnvironmentVariable, File.Exists, ReadFile);

    internal static string Detect(bool linux, Func<string, string?> environment,
        Func<string, bool> fileExists, Func<string, string?> readFile)
    {
        bool Enabled(string name) => environment(name) is { } value &&
            (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1");
        if (Enabled("DOTNET_RUNNING_IN_CONTAINER") || Enabled("DOTNET_RUNNING_IN_CONTAINERS"))
        {
            return "Container";
        }
        if (!linux)
        {
            return "Other";
        }
        var cgroup = readFile("/proc/self/cgroup") ?? "";
        if (fileExists("/.dockerenv") || fileExists("/run/.containerenv") ||
            cgroup.Contains("docker", StringComparison.OrdinalIgnoreCase) ||
            cgroup.Contains("kubepods", StringComparison.OrdinalIgnoreCase) ||
            cgroup.Contains("libpod", StringComparison.OrdinalIgnoreCase) ||
            cgroup.Contains("containerd", StringComparison.OrdinalIgnoreCase))
        {
            return "Container";
        }
        if (!string.IsNullOrWhiteSpace(environment("INVOCATION_ID")) ||
            cgroup.Split('\n', StringSplitOptions.RemoveEmptyEntries).Any(line => line.TrimEnd().EndsWith(".service", StringComparison.OrdinalIgnoreCase)))
        {
            return "Systemd";
        }
        return "Linux";
    }

    private static string? ReadFile(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
