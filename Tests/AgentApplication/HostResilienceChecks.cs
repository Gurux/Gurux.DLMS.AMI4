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

using Gurux.DLMS.AMI.Agent;

internal static class HostResilienceChecks
{
    internal static void Run()
    {
        string root = Path.Combine(Path.GetTempPath(), "agent-instance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string settings = Path.Combine(root, "settings.json");
        try
        {
            using (var first = GXAgentInstanceLock.Acquire(settings))
            {
                try { using var second = GXAgentInstanceLock.Acquire(settings); throw new Exception("Duplicate agent acquired the same settings lock."); }
                catch (InvalidOperationException) { }
                var start = new System.Diagnostics.ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true };
                start.ArgumentList.Add(typeof(HostResilienceChecks).Assembly.Location);
                start.ArgumentList.Add("--lock-probe"); start.ArgumentList.Add(settings);
                using var child = System.Diagnostics.Process.Start(start)!;
                if (!child.WaitForExit(10000) || child.ExitCode != 3) {
                    throw new Exception("A separate process bypassed the agent instance lock.");
                }
            }
            using (var restarted = GXAgentInstanceLock.Acquire(settings)) { }
            var id = Guid.NewGuid();
            using (var identity = GXAgentInstanceLock.AcquireIdentity("https://instance.test/foo", id))
            {
                try { using var duplicate = GXAgentInstanceLock.AcquireIdentity("https://instance.test/bar", id); throw new Exception("Address path bypassed the agent identity lock."); }
                catch (InvalidOperationException) { }
                var start = new System.Diagnostics.ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true };
                foreach (var argument in new[] { typeof(HostResilienceChecks).Assembly.Location, "--identity-probe", "https://instance.test/bar", id.ToString() })
                    {
                    start.ArgumentList.Add(argument);
                }
                using var child = System.Diagnostics.Process.Start(start)!;
                if (!child.WaitForExit(10000) || child.ExitCode != 3) {
                    throw new Exception("Separate process bypassed machine-wide identity lock.");
                }
            }
            for (int attempt = 1; attempt <= 30; ++attempt)
            {
                var minimum = GXRetryBackoff.GetDelay(attempt, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(5), 0);
                var maximum = GXRetryBackoff.GetDelay(attempt, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(5), 1);
                if (minimum <= TimeSpan.Zero || maximum > TimeSpan.FromMinutes(5) || maximum < minimum || minimum != maximum / 2)
                    {
                    throw new Exception("Retry jitter is outside its bounded backoff interval.");
                }
            }
            if (GXRetryBackoff.GetDelay(4, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(5), 1) != TimeSpan.FromSeconds(80))
                {
                throw new Exception("Startup retries do not grow exponentially.");
            }
        }
        finally { Directory.Delete(root, recursive: true); }
        Console.WriteLine("PASS exclusive agent settings lock, lock release and bounded exponential retry jitter.");
    }
}
