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
using System.Text;

internal static class IdentifyByDataChecks
{
    // Execute the supplied method body, wrapped only with the required class and AMI constructor.
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

                public GXGateway? Identify(string sender, IGXMedia media, byte[] data)
                {
                    GXGateway? gw = new GXGateway();
                    gw.Identifier = System.Text.ASCIIEncoding.ASCII.GetString(data);
                    gw = Ami.SingleOrDefault(gw);
                    if(gw == null)
                    {
                        //Add unknown gateway.
                        gw = new GXGateway();
                        gw.Name = gw.Identifier = System.Text.ASCIIEncoding.ASCII.GetString(data);
                        Ami.Add(gw);
                    }
                    return gw;
                }
            }
        }
        """;

    internal static async Task RunAsync()
    {
        using var media = new Gurux.Net.GXNet();
        var errors = new List<GXScriptException>();
        byte[] assembly = GXAmiScript.Generate(ScriptLanguage.CSharp, "IdentifyByData-" + Guid.NewGuid().ToString("N"), Source, errors, null)
            ?? throw new Exception("Identify compilation failed: " + string.Join("; ", errors.Select(e => e.Message)));
        var identifiers = new Dictionary<string, Guid>
        {
            ["GW-001"] = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            ["GW-002"] = Guid.Parse("44444444-4444-4444-4444-444444444444")
        };
        foreach (bool existing in new[] { true, false })
        {
            var stored = new List<GXGateway>();
            if (existing)
                {
                foreach (var entry in identifiers)
                    {
                    stored.Add(new() { Id = entry.Value, Identifier = entry.Key, Name = "Existing " + entry.Key });
                }
            }
            int lookups = 0, additions = 0;
            string expectedIdentifier = "";
            using var http = new HttpClient(new Handler(async (request, token) =>
            {
                if (request.Method != HttpMethod.Post) {
                    throw new Exception("Unexpected gateway HTTP method.");
                }
                switch (request.RequestUri!.AbsolutePath)
                {
                    case "/api/Gateway/List":
                        var query = await request.Content!.ReadFromJsonAsync<ListGateways>(token);
                        if (query?.Filter?.Identifier != expectedIdentifier)
                            {
                            throw new Exception("Gateway lookup did not use the ASCII identifier from the received data.");
                        }
                        ++lookups;
                        return Json(new ListGatewaysResponse { Gateways = [.. stored.Where(g => g.Identifier == expectedIdentifier)] });
                    case "/api/Gateway/Update":
                        var update = await request.Content!.ReadFromJsonAsync<UpdateGateway>(token);
                        var gateway = update!.Gateways!.Single();
                        if (gateway.Identifier != expectedIdentifier || gateway.Name != expectedIdentifier || gateway.Id != Guid.Empty)
                            {
                            throw new Exception("Unknown gateway name and identifier did not match the received ASCII string.");
                        }
                        if (stored.Any(g => g.Identifier == expectedIdentifier))
                            {
                            throw new Exception("Identify attempted to add a duplicate gateway.");
                        }
                        ++additions;
                        gateway.Id = identifiers[expectedIdentifier];
                        stored.Add(gateway);
                        return Json(new UpdateGatewayResponse { Ids = [gateway.Id] });
                    default:
                        throw new Exception("Unexpected script request: " + request.RequestUri.AbsolutePath);
                }
            })) { BaseAddress = new("https://ami.example") };
            using var services = new ServiceCollection()
                .AddSingleton<IGatewayRepository>(new GXGatewayRepository(http))
                .BuildServiceProvider();
            var runner = new GXAmiScript(services);
            foreach (var entry in identifiers)
            {
                expectedIdentifier = entry.Key;
                // Different identifiers share a sender; the same identifier also arrives from a different IP.
                foreach (string sender in new[] { "192.0.2.10:4059", "192.0.2.20:5060" })
                {
                    var args = new GXScriptRunArgs
                    {
                        ByteAssembly = assembly, MethodName = "Identify", Parameters = [sender, media, Encoding.ASCII.GetBytes(entry.Key)]
                    };
                    try
                    {
                        var result = await runner.RunAsync(args) as GXGateway;
                        if (result?.Id != entry.Value || result.Identifier != entry.Key || result.Name != (existing ? "Existing " + entry.Key : entry.Key))
                            {
                            throw new Exception("Identify returned the wrong gateway or failed to retain its assigned identifier.");
                        }
                    }
                    finally { args.AssemblyLoadContext?.Unload(); }
                }
            }
            if (lookups != 4 || additions != (existing ? 0 : 2) || stored.Count != 2)
                {
                throw new Exception("Identification did not reuse gateways by received identifier independently of sender IP.");
            }
            Console.WriteLine(existing
                ? "PASS Identify script finds existing gateways by received ASCII identifier independently of sender IP"
                : "PASS Identify script creates unknown gateways from ASCII data and reuses them without duplicates");
        }
    }

    private static HttpResponseMessage Json<T>(T value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token);
    }
}
