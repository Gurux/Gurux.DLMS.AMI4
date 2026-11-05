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

using Gurux.DLMS.AMI.Agent.Worker;
using System.Reflection;

if (args.Contains("completion-outbox")) { await CompletionOutboxChecks.RunAsync(); return; }
if (args.Contains("meter-session")) { await MeterSessionChecks.RunAsync(); return; }
var failures = new List<string>();
void Check(string name, Action test)
{
    try { test(); Console.WriteLine("PASS " + name); }
    catch (Exception e) { failures.Add(name + ": " + e.Message); Console.WriteLine("FAIL " + failures[^1]); }
}
Check("worker execution state is instance-owned", () =>
{
    foreach (var name in new[] { "client", "_hubConnection", "_logger", "readCancellationToken" })
        {
        if (typeof(GXAgentWorker).GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) != null)
            {
            throw new Exception(name + " is shared between workers");
        }
    }
});
Check("reader operations are awaitable", () =>
{
    foreach (var name in new[] { "AutoConnect.GXAutoConnect", "Gateway.GXGatewayReader" })
    {
        var type = typeof(GXAgentWorker).Assembly.GetType("Gurux.DLMS.AMI.Agent.Worker." + name)!;
        if (!typeof(Task).IsAssignableFrom(type.GetMethod("ReadMeter")!.ReturnType))
            {
            throw new Exception(name + " returns void");
        }
    }
});
if (failures.Count != 0) {
    throw new Exception(string.Join(Environment.NewLine, failures));
}
await ReliabilityChecks.RunAsync();
await AgentLogChecks.RunAsync();
await MeterSessionChecks.RunAsync();
await CompletionOutboxChecks.RunAsync();
