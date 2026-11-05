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

using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Gurux.DLMS;
using Gurux.DLMS.Enums;
using Gurux.DLMS.AMI.Agent.Worker;
using Gurux.DLMS.AMI.Agent.Worker.Notifier;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using DeviceSettings = Gurux.DLMS.AMI.Shared.DTOs.Device.GXDLMSSettings;
using Task = System.Threading.Tasks.Task;

internal static class PushDeviceKeyChecks
{
    internal static async Task RunAsync()
    {
        byte[] title = [1, 2, 3, 4, 5, 6, 7, 8];
        using var handler = new Devices(title);
        using var http = new HttpClient(handler) { BaseAddress = new("https://devices.test") };
        await using var worker = new GXAgentWorker(http);
        using var services = new ServiceCollection().BuildServiceProvider();
        using var notify = new GXNotifyService(NullLogger<GXNotifyService>.Instance, services, worker);
        var get = typeof(GXNotifyService).GetMethod("OnGetKeys", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var args = new GXCryptoKeyParameter();
        typeof(GXCryptoKeyParameter).GetProperty(nameof(args.SystemTitle))!.SetValue(args, title);
        typeof(GXCryptoKeyParameter).GetProperty(nameof(args.KeyType))!.SetValue(args, CryptoKeyType.Authentication | CryptoKeyType.BlockCipher);
        void Read() => get.Invoke(notify, [new object(), args]);
        Read();
        if (args.BlockCipherKey![0] != 1 || args.AuthenticationKey![0] != 21) {
            throw new Exception("Push did not use broadcast/authentication keys.");
        }
        handler.Generation = 2; Read();
        if (args.BlockCipherKey![0] != 2 || handler.Reads != 2) {
            throw new Exception("Push reused an old key instead of fresh Device settings.");
        }
        handler.NoBroadcast = true; Read();
        if (args.BlockCipherKey![0] != 12) {
            throw new Exception("Push did not fall back to block cipher key.");
        }
        handler.MissingAuthentication = true;
        try { Read(); throw new Exception("Missing push key was accepted."); }
        catch (TargetInvocationException error) when (error.InnerException is InvalidOperationException) { }
        if (args.AuthenticationKey != null || args.BlockCipherKey != null) {
            throw new Exception("Rejected push retained stale crypto arguments.");
        }
        Console.WriteLine("PASS push broadcast/fallback keys, fresh settings after rotation, and missing-key rejection without key-management HTTP.");
    }
    private sealed class Devices(byte[] title) : HttpMessageHandler
    {
        internal byte Generation = 1;
        internal int Reads;
        internal bool NoBroadcast, MissingAuthentication;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (request.Method != HttpMethod.Get || request.RequestUri!.AbsolutePath != "/api/Device/BySystemTitle" ||
                !request.RequestUri.Query.Contains(Convert.ToHexString(title), StringComparison.OrdinalIgnoreCase))
                {
                throw new Exception("Unexpected device/key route: " + request.RequestUri);
            }
            Reads++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new GetDeviceResponse
            {
                Item = new GXDevice { Id = Guid.NewGuid(), Settings = JsonSerializer.Serialize(new DeviceSettings
                {
                    DeviceSystemTitle = title, BroadcastKey = NoBroadcast ? null : Enumerable.Repeat(Generation, 16).ToArray(),
                    BlockCipherKey = Enumerable.Repeat((byte)(Generation + 10), 16).ToArray(),
                    AuthenticationKey = MissingAuthentication ? null : Enumerable.Repeat((byte)(Generation + 20), 16).ToArray()
                }) }
            }) });
        }
    }
}
