//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using Gurux.DLMS.AMI.Agent.Worker.Repositories;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.Rest;

internal static class AgentLogChecks
{
    internal static async Task RunAsync()
    {
        var id = Guid.NewGuid();
        using var http = new HttpClient(new Handler(id)) { BaseAddress = new Uri("https://ami.test") };
        ILogRepository repository = new GXLogRepository(http);
        var agent = repository.ForSource("agent");
        if ((await agent.AddAsync("Info", [new GXLog { Message = "test" }])).Single() != id)
        {
            throw new Exception("Agent must use the identifiers returned by the common API.");
        }
        await agent.CloseAsync([id]);
        await agent.ClearAsync(new ClearLog { Ids = [id] });
        Console.WriteLine("Agent common log API forwarding checks passed.");
    }

    private sealed class Handler(Guid id) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri?.Query != "?source=agent")
            {
                throw new Exception("Agent source must be forwarded.");
            }
            var path = request.RequestUri.AbsolutePath;
            object result;
            if (path == "/api/Log/Add")
            {
                var body = await request.Content!.ReadFromJsonAsync<AddLog>(cancellationToken);
                if (body?.Logs.Single().Message != "test")
                {
                    throw new Exception("Agent log payload changed.");
                }
            
                result = new AddLogResponse
                {
                    Ids = [id]
                };
            }
            else if (path == "/api/Log/Close")
            {
                result = new CloseLogResponse();
            }
            else if (path == "/api/Log/Clear")
            {
                result = new ClearLogResponse();
            }
            else
            {
                throw new Exception("Unexpected common log route.");
            }
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(result, result.GetType()) };
        }
    }
}

