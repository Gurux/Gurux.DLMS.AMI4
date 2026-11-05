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

internal static class SettingsChecks
{
    internal static async Task RunAsync()
    {
        string previous = Environment.CurrentDirectory;
        string root = Path.Combine(Path.GetTempPath(), "agent-settings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            foreach (string content in new[] { "", " \t\r\n" })
            {
                bool useDefault = content.Length != 0;
                string emptySettings = Path.Combine(root, useDefault ? "settings.json" : "empty.json");
                await File.WriteAllTextAsync(emptySettings, content);
                var start = new System.Diagnostics.ProcessStartInfo("dotnet")
                {
                    UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = root,
                    RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
                };
                start.ArgumentList.Add(typeof(Settings).Assembly.Location);
                if (useDefault)
                {
                    start.Environment.Remove("GURUX_AGENT_SETTINGS");
                }
                else
                {
                    start.Environment["GURUX_AGENT_SETTINGS"] = emptySettings;
                }
                using var process = System.Diagnostics.Process.Start(start)!;
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                process.StandardInput.Close();
                try
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                    await process.WaitForExitAsync(timeout.Token);
                    string text = await output + await error;
                    if (!text.Contains("Enter Personal Access Token:"))
                    {
                        throw new Exception("Empty settings did not start registration: " + text);
                    }
                    if (useDefault && !File.Exists(emptySettings + ".lock"))
                    {
                        throw new Exception("Agent startup did not lock working-directory settings.");
                    }
                    if (await File.ReadAllTextAsync(emptySettings) != content)
                    {
                        throw new Exception("Incomplete registration changed saved settings.");
                    }
                }
                finally
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        await process.WaitForExitAsync();
                    }
                }
            }
            Environment.CurrentDirectory = root;
            string first = GXAgentSettingsStore.DefaultPath;
            if (first != Path.Combine(root, "settings.json"))
            {
                throw new Exception("Default settings must use the agent working directory.");
            }
            var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
            if (GXAgentUpdaterOptions.Read(configuration).SettingsPath != first)
            {
                throw new Exception("Startup and updater must use the working-directory settings.");
            }
            string secondDirectory = Path.Combine(root, "second-agent");
            Directory.CreateDirectory(secondDirectory);
            using (var firstLock = GXAgentInstanceLock.Acquire(first))
            {
                Environment.CurrentDirectory = secondDirectory;
                string second = GXAgentSettingsStore.DefaultPath;
                if (second != Path.Combine(secondDirectory, "settings.json") || second == first)
                {
                    throw new Exception("Agents in different directories share their default settings.");
                }
                using var secondLock = GXAgentInstanceLock.Acquire(second);
                await GXAgentSettingsStore.SaveAsync(first, "first-agent", CancellationToken.None);
                await GXAgentSettingsStore.SaveAsync(second, "second-agent", CancellationToken.None);
                if (await File.ReadAllTextAsync(first) != "first-agent" || await File.ReadAllTextAsync(second) != "second-agent")
                {
                    throw new Exception("Separate agents overwrite each other's settings.");
                }
            }
            string target = Path.Combine(root, "data", "settings.json");
            await GXAgentSettingsStore.SaveAsync(target, "secret-token", CancellationToken.None);
            await GXAgentSettingsStore.SaveAsync(target, "new-token", CancellationToken.None);
            if (await File.ReadAllTextAsync(target) != "new-token" || Directory.GetFiles(Path.GetDirectoryName(target)!).Length != 1)
                {
                throw new Exception("Atomic settings replacement failed.");
            }
            CheckPrivate(target);
        }
        finally { Environment.CurrentDirectory = previous; Directory.Delete(root, recursive: true); }
        Console.WriteLine("PASS working-directory settings, independent agent locks and private atomic replacement.");
    }

    private static void CheckPrivate(string target)
    {
            if (!OperatingSystem.IsWindows() && (File.GetUnixFileMode(target) & (UnixFileMode.GroupRead | UnixFileMode.OtherRead)) != 0)
                {
                throw new Exception("Settings expose secrets to other users.");
            }
            if (OperatingSystem.IsWindows())
            {
                var acl = System.IO.FileSystemAclExtensions.GetAccessControl(new FileInfo(target));
                var rules = acl.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
                if (!acl.AreAccessRulesProtected || rules.Count != 1 ||
                    rules[0]!.IdentityReference != System.Security.Principal.WindowsIdentity.GetCurrent().User)
                    {
                    throw new Exception("Settings ACL is not restricted to its owner.");
                }
            }
    }
}
