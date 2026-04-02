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

using Gurux.DLMS.AMI.Agent.Worker.Gateway;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Net;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Agent.Worker.AutoConnect
{
    internal class GXGatewayService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IGXTaskNotification _taskNotification;
        private readonly ILogger _logger;
        private GXScriptMethod? _gatewayIdentificationScript;

        //Gateway wait incoming connections from the meters.
        GXNet? listener;
        GatewaySettings? _settings;

        public GXGatewayService(ILogger<GXGatewayService> logger,
            IServiceProvider serviceProvider,
            IGXTaskNotification taskNotification)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _taskNotification = taskNotification;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _settings = GXAgentWorker.Options.GatewaySettings;
            if (_settings != null && _settings.Active)
            {
                listener = new GXNet();
                listener.Settings = _settings.MediaSettings;
                listener.Server = true;
                if (listener.Protocol == NetworkType.Tcp)
                {
                    listener.OnClientConnected += OnClientConnected;
                    listener.OnClientDisconnected += OnClientDisconnected;
                }
                else
                {
                    listener.OnReceived += OnOnReceived;
                }
                _logger.LogInformation("Gateway in port:" + listener.Port);
                if (_settings?.ScriptMethod != null)
                {
                    //Get script when app starts.
                    ListScripts req = new ListScripts()
                    {
                    };
                    if (_settings.ScriptMethod == null)
                    {
                        //Get the source code.
                        req.Select = ["SourceCode"];
                    }
                    req.Filter = new GXScript();
                    req.Filter.Methods = new List<GXScriptMethod>(
                    [
                        new GXScriptMethod() { Id = _settings.ScriptMethod.Value }
                    ]);
                    ListScriptsResponse? ret = GXAgentWorker.client.PostAsJson<ListScriptsResponse>("/api/Script/List", req).Result;
                    if (ret?.Scripts?.FirstOrDefault()?.Methods is List<GXScriptMethod> methods)
                    {
                        foreach (var sm in methods)
                        {
                            if (sm.Id == _settings.ScriptMethod.Value)
                            {
                                if (_settings.Script == null)
                                {
                                    _settings.Script = Helpers.Compile(ret.Scripts[0].SourceCode, ret.Scripts[0].Namespaces);
                                    File.WriteAllText("settings.json", JsonSerializer.Serialize(GXAgentWorker.Options));
                                }
                                //TODO: _gatewayIdentificationScript?.Script?.ByteAssembly
                                _gatewayIdentificationScript = sm;
                                //Update parent script.
                                sm.Script = ret.Scripts[0];
                                sm.Script.ByteAssembly = _settings.Script;
                                break;
                            }
                        }
                    }
                    if (_gatewayIdentificationScript == null)
                    {
                        throw new Exception("Unknown gateway identification script.");
                    }
                }
                else
                {
                    _gatewayIdentificationScript = null;
                }

                listener.Open();
            }
            else if (listener != null)
            {
                listener.Close();
            }
            if (listener == null || !listener.IsOpen)
            {
                _logger.LogInformation("Gateway service is not used.");
            }
            return Task.CompletedTask;
        }

        private static async Task UpdateGatewayStatusAsync(GatewayStatus status)
        {
            if (GXAgentWorker.Options.GatewaySettings?.Id is Guid id)
            {
                UpdateGatewayStatus req = new UpdateGatewayStatus();
                req.Id = id;
                req.Status = status;
                await GXAgentWorker.client.PostAsJson("/api/Gateway/UpdateStatus", req);
            }
        }

        /// <summary>
        /// Gateway has made a connection to the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal async void OnClientConnected(object sender, Gurux.Common.ConnectionEventArgs e)
        {
            await UpdateGatewayStatusAsync(GatewayStatus.Connected);
            if (GXAgentWorker.Options.TraceLevel > TraceLevel.Warning)
            {
                AddAgentLog log = new AddAgentLog();
                log.Type = "Gateway";
                log.Logs = [new GXAgentLog(TraceLevel.Verbose)
                        {
                            Agent = new GXAgent(){Id =GXAgentWorker.Options.Id },
                            Message = string.Format("Client {0} is connected.", e.Info)
                        } ];
                await GXAgentWorker.client.PostAsJson("/api/AgentLog/Add", log);
            }
            if (_settings?.TraceLevel > TraceLevel.Warning)
            {
                _logger?.LogInformation("Client {0} is connected.", e.Info);
            }
            Console.WriteLine();
            GXNet server = (GXNet)sender;
            try
            {
                GXNet media = server.Attach(e);
                var ac = new GXGatewayReader(_serviceProvider,
                    _logger,
                    media,
                    e.Info,
                    _gatewayIdentificationScript,
                    null,
                    _taskNotification);
                Thread thread = new Thread(() =>
                {
                    ac.ReadMeter();
                });
                thread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        /// <summary>
        /// Gateway has close the connection to the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal async void OnClientDisconnected(object sender, Common.ConnectionEventArgs e)
        {
            await UpdateGatewayStatusAsync(GatewayStatus.Offline);
            if (GXAgentWorker.Options.TraceLevel > TraceLevel.Warning)
            {
                AddAgentLog log = new AddAgentLog();
                log.Type = "Gateway";
                log.Logs = [new GXAgentLog(TraceLevel.Verbose)
                        {
                            Agent = new GXAgent(){Id =GXAgentWorker.Options.Id },
                            Message = string.Format("Client {0} is disconnect.", e.Info)
                        } ];
                await GXAgentWorker.client.PostAsJson("/api/AgentLog/Add", log);
            }
            if (_settings?.TraceLevel > TraceLevel.Warning)
            {
                Console.WriteLine("Client {0} is disconnect.", e.Info);
            }
            if (GXAgentWorker.Options.GatewaySettings?.Id is Guid id2)
            {
                //If only one gateway is connected.
                _taskNotification.Unregister(id2);
            }
        }

        internal void OnOnReceived(object sender, Common.ReceiveEventArgs e)
        {
            Console.WriteLine("Client {0} is connected.", e.SenderInfo);
            GXNet server = (GXNet)sender;
            try
            {
                GXNet media = server.Attach(e);
                var ac = new GXGatewayReader(_serviceProvider,
                    _logger,
                    media,
                    e.SenderInfo,
                    _gatewayIdentificationScript,
                    null,
                    _taskNotification);
                Thread thread = new Thread(() =>
                {
                    ac.ReadMeter();
                });
                thread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Gateway service is stopping.");
            if (listener != null)
            {
                listener.Close();
            }
            _taskNotification.Close();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (listener != null)
            {
                listener.Dispose();
                listener = null;
            }
        }
    }
}
