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

using Gurux.DLMS.AMI.Gateway.Worker.Repositories;
using Gurux.DLMS.AMI.Script;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

internal static class IdentifyByIpChecks
{
    // Keep the method body identical to the supplied script; compile and invoke it through the real AMI runner.
    private const string Source = """
        using Gurux.Common;
        using Gurux.DLMS.AMI.Script;
        using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
        namespace Gurux.DLMS.AMI.GeneratedScript
        {
            public class GXAmiScript
            {
                private readonly IGXAmi Ami;
                public GXAmiScript(IGXAmi ami) { Ami = ami; }

                public GXGateway? IdentifyByIp(string sender, IGXMedia media, byte[] data)
                {
                    GXGateway? gw = new GXGateway();
                    int pos = sender.IndexOf(":");
                    sender = sender.Substring(0, pos);
                    gw.Identifier = sender;
                    gw = Ami.SingleOrDefault(gw);
                    if(gw == null)
                    {
                        //Add unknown gateway.
                        gw = new GXGateway();
                        gw.Name = gw.Identifier = sender;
                        Ami.Add(gw);
                    }
                    return gw;
                }
            }
        }
        """;

    internal static async Task RunAsync()
    {
        // Generate uses loaded assembly references; constructing the unopened media loads the IGXMedia dependency.
        using var media = new Gurux.Net.GXNet();
        var errors = new List<GXScriptException>();
        byte[] assembly = GXAmiScript.Generate(ScriptLanguage.CSharp, "IdentifyByIp-" + Guid.NewGuid().ToString("N"), Source, errors, null)
            ?? throw new Exception("IdentifyByIp compilation failed: " + string.Join("; ", errors.Select(e => e.Message)));

        foreach (bool existing in new[] { true, false })
        {
            var stored = new List<GXGateway>();
            var id = Guid.Parse("22222222-2222-2222-2222-222222222222");
            if (existing) {
                stored.Add(new() { Id = id, Identifier = "192.0.2.10", Name = "Existing gateway" });
            }
            int lookups = 0, additions = 0;
            using var http = new HttpClient(new Handler(async (request, token) =>
            {
                if (request.Method != HttpMethod.Post) {
                    throw new Exception("Unexpected gateway HTTP method.");
                }
                switch (request.RequestUri!.AbsolutePath)
                {
                    case "/api/Gateway/List":
                        var query = await request.Content!.ReadFromJsonAsync<ListGateways>(token);
                        if (query?.Filter?.Identifier != "192.0.2.10")
                            {
                            throw new Exception("Gateway lookup did not use the IP without its source port.");
                        }
                        ++lookups;
                        return Json(new ListGatewaysResponse { Gateways = [.. stored] });
                    case "/api/Gateway/Update":
                        var update = await request.Content!.ReadFromJsonAsync<UpdateGateway>(token);
                        var gateway = update!.Gateways!.Single();
                        if (gateway.Identifier != "192.0.2.10" || gateway.Name != "192.0.2.10" || gateway.Id != Guid.Empty)
                            {
                            throw new Exception("Unknown gateway was not added with its IP as name and identifier.");
                        }
                        if (stored.Count != 0) {
                            throw new Exception("IdentifyByIp attempted to add a duplicate gateway.");
                        }
                        ++additions;
                        gateway.Id = id;
                        stored.Add(gateway);
                        return Json(new UpdateGatewayResponse { Ids = [id] });
                    default:
                        throw new Exception("Unexpected script request: " + request.RequestUri.AbsolutePath);
                }
            })) { BaseAddress = new("https://ami.example") };
            using var services = new ServiceCollection()
                .AddSingleton<IGatewayRepository>(new GXGatewayRepository(http))
                .BuildServiceProvider();
            var runner = new GXAmiScript(services);
            foreach (string sender in new[] { "192.0.2.10:4059", "192.0.2.10:5060" })
            {
                var args = new GXScriptRunArgs { ByteAssembly = assembly, MethodName = "IdentifyByIp", Parameters = [sender, media, Array.Empty<byte>()] };
                try
                {
                    var result = await runner.RunAsync(args) as GXGateway;
                    if (result?.Id != id || result.Identifier != "192.0.2.10" || result.Name != (existing ? "Existing gateway" : "192.0.2.10"))
                        {
                        throw new Exception("IdentifyByIp returned a different gateway or failed to apply the persisted identifier.");
                    }
                }
                finally { args.AssemblyLoadContext?.Unload(); }
            }
            if (lookups != 2 || additions != (existing ? 0 : 1) || stored.Count != 1)
                {
                throw new Exception("Repeated identification did not reuse the gateway independently of its source port.");
            }
            Console.WriteLine(existing
                ? "PASS IdentifyByIp script finds an existing gateway by IPv4 without adding a duplicate"
                : "PASS IdentifyByIp script creates an unknown gateway and reuses it on the next connection");
        }
    }

    private static HttpResponseMessage Json<T>(T value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token);
    }
}
