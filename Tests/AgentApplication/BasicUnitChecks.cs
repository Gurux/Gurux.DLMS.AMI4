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

internal static class BasicUnitChecks
{
    internal static async Task RunAsync()
    {
        var failures = new List<string>();
        int passed = 0;
        async Task Check(string name, Func<Task> test)
        {
            try { await test().WaitAsync(TimeSpan.FromSeconds(5)); ++passed; Console.WriteLine("PASS " + name); }
            catch (Exception error) { failures.Add(name + ": " + error.Message); Console.WriteLine("FAIL " + failures[^1]); }
        }
        Task Sync(Action test) { test(); return Task.CompletedTask; }

        await Check("valid registered configuration is accepted", () => Sync(() => ValidOptions().Validate()));
        await Check("registration permits a missing identity but startup rejects it", () => Sync(() =>
        {
            var options = ValidOptions(); options.Id = Guid.Empty;
            options.Validate(requireAgentId: false);
            var error = Throws<GXAgentOptionsValidationException>(() => options.Validate());
            Require(error.Errors.Single().Code == GXAgentValidationErrorCode.AgentIdRequired, "Wrong missing identity error.");
        }));
        foreach (string address in new[] { "relative/path", "ftp://ami.example", "https://user:secret@ami.example", "https://ami.example?token=secret", "https://ami.example/#fragment" })
            {
            await Check("invalid server address rejected: " + address, () => Sync(() =>
            {
                var options = ValidOptions(); options.Address = address;
                var error = options.GetValidationErrors().Single();
                Require(error.Code == GXAgentValidationErrorCode.InvalidAddress && error.PropertyPath == "Address", "Wrong address validation error.");
            }));
        }
        await Check("validation aggregates errors without leaking secrets", () => Sync(() =>
        {
            var options = ValidOptions();
            options.Address = "https://user:address-secret@ami.example"; options.Token = "token-secret with spaces";
            var error = Throws<GXAgentOptionsValidationException>(() => options.Validate());
            Require(error.Errors.Count == 2 && error.Errors.Any(e => e.Code == GXAgentValidationErrorCode.InvalidToken), "Independent errors were not aggregated.");
            Require(!error.Message.Contains("address-secret") && !error.Message.Contains("token-secret"), "Validation leaked secrets.");
        }));
        await Check("enabled reader validates threads and wait times", () => Sync(() =>
        {
            var options = ValidOptions(); options.ReaderSettings = new() { Enabled = true, Threads = 0, TaskWaitTime = -1 };
            var errors = options.GetValidationErrors();
            Require(errors.Count == 2 && errors.Any(e => e.PropertyPath == "ReaderSettings.Threads" && e.Code == GXAgentValidationErrorCode.InvalidThreadCount)
                && errors.Any(e => e.PropertyPath == "ReaderSettings.TaskWaitTime" && e.Code == GXAgentValidationErrorCode.NegativeValue), "Nested reader errors are missing.");
            options.ReaderSettings.Enabled = false;
            options.Validate();
        }));
        await Check("snapshot isolates original settings and returned copies", () => Sync(() =>
        {
            var options = ValidOptions(); options.ReaderSettings = new() { Threads = 3 };
            var snapshot = GXAgentOptionsSnapshot.Capture(options);
            options.Address = "https://changed.example"; options.ReaderSettings.Threads = 9;
            var copy = snapshot.ToOptions();
            Require(copy.Address == "https://ami.example" && copy.ReaderSettings!.Threads == 3, "Input mutation changed the snapshot.");
            copy.ReaderSettings!.Threads = 12; copy.Token = "replaced";
            var next = snapshot.ToOptions();
            Require(next.ReaderSettings!.Threads == 3 && next.Token == "private-token", "Returned copy shares mutable state.");
        }));
        await Check("snapshot rejects null and invalid configuration", () => Sync(() =>
        {
            Throws<ArgumentNullException>(() => GXAgentOptionsSnapshot.Capture(null!));
            var options = ValidOptions(); options.Token = "";
            Throws<GXAgentOptionsValidationException>(() => GXAgentOptionsSnapshot.Capture(options));
        }));
        await Check("diagnostics redact credentials and query secrets", () => Sync(() =>
        {
            var options = ValidOptions(); options.Address = "https://user:address-secret@ami.example/?key=query-secret#fragment-secret";
            string text = options.ToString();
            foreach (string secret in new[] { "private-token", "address-secret", "query-secret", "fragment-secret" })
                {
                Require(!text.Contains(secret), "Diagnostics leaked " + secret);
            }
            Require(text.Contains("ami.example"), "Useful server diagnostics were lost.");
        }));
        await Check("serial capabilities filter, sort and copy supplied names", () => Sync(() =>
        {
            var ports = new List<string> { "COM3", "", "COM1", "COM3", " " };
            var capabilities = new GXAgentCapabilities(ports); ports.Clear();
            Require(capabilities.SerialPorts.SequenceEqual(new[] { "COM1", "COM3" }), "Capabilities did not normalize and copy names.");
        }));
        await Check("notifications combine flags and consume each batch once", async () =>
        {
            using var notifications = new GXAgentHostNotifications();
            notifications.NotifySettingsChanged(); notifications.NotifySettingsChanged(); notifications.NotifyVersionAvailable();
            Require(await notifications.WaitForChangeAsync() == (GXAgentHostChange.SettingsChanged | GXAgentHostChange.VersionAvailable), "Changes were lost or not combined.");
            var waiting = notifications.WaitForChangeAsync().AsTask();
            Require(!waiting.IsCompleted, "Consumed changes were delivered twice.");
            notifications.NotifySettingsChanged();
            Require(await waiting == GXAgentHostChange.SettingsChanged, "Fresh notification did not wake the consumer.");
        });
        await Check("canceled notification wait preserves pending changes", async () =>
        {
            using var notifications = new GXAgentHostNotifications(); notifications.NotifyVersionAvailable();
            using var canceled = new CancellationTokenSource(); canceled.Cancel();
            await ThrowsAsync<OperationCanceledException>(() => notifications.WaitForChangeAsync(canceled.Token).AsTask());
            Require(await notifications.WaitForChangeAsync() == GXAgentHostChange.VersionAvailable, "Canceled wait consumed pending changes.");
        });
        await Check("notification disposal releases waiters and rejects producers", async () =>
        {
            using var notifications = new GXAgentHostNotifications();
            var waiting = notifications.WaitForChangeAsync().AsTask(); notifications.Dispose();
            await ThrowsAsync<ObjectDisposedException>(() => waiting);
            Throws<ObjectDisposedException>(notifications.NotifySettingsChanged);
            Throws<ObjectDisposedException>(notifications.NotifyVersionAvailable);
        });
        await Check("retry delay applies jitter, exponential growth and cap", () => Sync(() =>
        {
            var initial = TimeSpan.FromSeconds(10); var maximum = TimeSpan.FromMinutes(5);
            Require(GXRetryBackoff.GetDelay(1, initial, maximum, 0) == TimeSpan.FromSeconds(5), "Wrong first-attempt minimum.");
            Require(GXRetryBackoff.GetDelay(3, initial, maximum, 1) == TimeSpan.FromSeconds(40), "Wrong exponential growth.");
            Require(GXRetryBackoff.GetDelay(30, initial, maximum, 1) == TimeSpan.FromSeconds(300), "Retry exceeded its cap.");
            Throws<ArgumentOutOfRangeException>(() => GXRetryBackoff.GetDelay(1, TimeSpan.Zero, maximum));
            Throws<ArgumentOutOfRangeException>(() => GXRetryBackoff.GetDelay(1, initial, TimeSpan.Zero));
        }));
        foreach (var (name, change) in new (string, Action<GXCompletionOutboxOptions>)[]
        {
            ("zero records", o => o.MaxPendingRecords = 0), ("zero bytes", o => o.MaxPendingBytes = 0),
            ("negative free space", o => o.MinFreeBytes = -1), ("zero request timeout", o => o.RequestTimeout = TimeSpan.Zero),
            ("excessive request timeout", o => o.RequestTimeout = TimeSpan.FromMinutes(6)),
            ("zero flush budget", o => o.FlushBudget = TimeSpan.Zero), ("excessive flush budget", o => o.FlushBudget = TimeSpan.FromMinutes(6))
        })
            {
            await Check("completion options reject " + name, () => Sync(() =>
            {
                var options = new GXCompletionOutboxOptions(); change(options);
                Throws<ArgumentException>(options.Validate);
            }));
        }
        Console.WriteLine($"Basic unit checks: {passed} passed, {failures.Count} failed.");
        if (failures.Count != 0) {
            throw new Exception(string.Join(Environment.NewLine, failures));
        }
    }

    private static GXAgentOptions ValidOptions() => new()
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Address = "https://ami.example", Token = "private-token"
    };
    private static void Require(bool condition, string message) { if (!condition) {
        throw new Exception(message);
    } }
    private static T Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T error) { return error; }
        throw new Exception("Expected " + typeof(T).Name);
    }
    private static async Task ThrowsAsync<T>(Func<Task> action) where T : Exception
    {
        try { await action(); } catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
}
