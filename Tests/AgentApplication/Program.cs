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
using Gurux.DLMS.AMI.Agent.Shared;
using Gurux.DLMS.AMI.Agent.Worker;

if (args is ["--lock-probe", var lockPath])
{
    try { using var lease = GXAgentInstanceLock.Acquire(lockPath); }
    catch (InvalidOperationException) { Environment.ExitCode = 3; }
    return;
}
if (args is ["--identity-probe", var address, var agentId])
{
    try { using var lease = GXAgentInstanceLock.AcquireIdentity(address, Guid.Parse(agentId)); }
    catch (InvalidOperationException) { Environment.ExitCode = 3; }
    return;
}

var application = typeof(Settings).Assembly;
if (args is ["identify-by-data"]) { await IdentifyByDataChecks.RunAsync(); return; }
if (args is ["identify-by-ip"]) { await IdentifyByIpChecks.RunAsync(); return; }
if (args is ["unit"]) { await TaskFailureChecks.RunAsync();
await BasicUnitChecks.RunAsync(); return; }
if (args is ["completion-monitoring"]) { await CompletionMonitoringChecks.RunAsync(); return; }
if (args is ["updater"]) { await UpdaterChecks.RunAsync(); return; }
if (args is ["authentication"]) { await AuthenticationChecks.RunAsync(); return; }
var failures = new List<string>();
if (typeof(GXAgentWorker).Assembly != application) {
    failures.Add("Meter runtime is still a separate worker assembly.");
}
if (typeof(GXAgentOptions).Assembly != application) {
    failures.Add("Agent-only configuration is still a separate shared assembly.");
}
if (application.GetReferencedAssemblies().Any(a => a.Name is "Gurux.DLMS.AMI.Agent.Worker" or "Gurux.DLMS.AMI.Agent.Shared"))
    {
    failures.Add("Application still references split worker/shared projects.");
}
if (application.GetType("Gurux.DLMS.AMI.Agent.Shared.GXAgentContract") != null)
    {
    failures.Add("Dynamic host-worker binary compatibility contract still exists.");
}
var resources = new System.Resources.ResourceManager("Gurux.DLMS.AMI.Agent.Worker.Properties.Resources", application);
if (string.IsNullOrWhiteSpace(resources.GetString("NoExecutedTasks"))) {
    failures.Add("Runtime resources were lost during the project merge.");
}
if (failures.Count != 0) {
    throw new Exception(string.Join(Environment.NewLine, failures));
}
Console.WriteLine("PASS single application assembly owns runtime and configuration, without dynamic worker contracts.");
await TaskFailureChecks.RunAsync();
await BasicUnitChecks.RunAsync();
await IdentifyByIpChecks.RunAsync();
await IdentifyByDataChecks.RunAsync();
await UpdaterChecks.RunAsync();
await SettingsChecks.RunAsync();
await AuthenticationChecks.RunAsync();
await CompletionResilienceChecks.RunAsync();
await CompletionMonitoringChecks.RunAsync();
HostResilienceChecks.Run();
await PushDeviceKeyChecks.RunAsync();
