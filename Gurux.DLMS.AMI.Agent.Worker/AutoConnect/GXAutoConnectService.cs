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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Gurux.Net;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Shared.DTOs.Script;

namespace Gurux.DLMS.AMI.Agent.Worker.AutoConnect
{
    internal class GXAutoConnectService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IGXTaskNotification _taskNotification;
        private readonly ILogger _logger;
        private GXScriptMethod? _autoConnectionIdentificationScript;
        private GXScriptMethod? _gatewayIdentificationScript;

        //Listener wait incoming connections from the meters.
        GXNet? listener;
        ListenerSettings? _settings;

        public GXAutoConnectService(ILogger<GXAutoConnectService> logger,
            IServiceProvider serviceProvider,
            IGXTaskNotification taskNotification)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _taskNotification = taskNotification;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _settings = GXAgentWorker.Options.ListenerSettings;
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
                _logger.LogInformation("Listening incoming connections in port:" + listener.Port);
                if (_settings?.ScriptMethod != null)
                {
                    //Get script when app starts.
                    ListScripts req = new ListScripts();
                    req.Filter = new GXScript();
                    req.Filter.Methods = new List<GXScriptMethod>(new GXScriptMethod[]
                    {
                        new GXScriptMethod() { Id = _settings.ScriptMethod.Value }
                    });
                    ListScriptsResponse? ret = GXAgentWorker.client.PostAsJson<ListScriptsResponse>("/api/Script/List", req).Result;
                    if (ret?.Scripts?.FirstOrDefault()?.Methods is List<GXScriptMethod> methods)
                    {
                        foreach (var sm in methods)
                        {
                            if (sm.Id == _settings.ScriptMethod.Value)
                            {
                                _autoConnectionIdentificationScript = sm;
                                //Update parent script.
                                sm.Script = ret.Scripts[0];
                                break;
                            }
                        }
                    }
                    if (_autoConnectionIdentificationScript == null)
                    {
                        throw new Exception("Unknown auto connection identification script.");
                    }
                }
                else
                {
                    _autoConnectionIdentificationScript = null;
                }

                if (_settings?.GatewayScriptMethod != null)
                {
                    //Get script when app starts.
                    ListScripts req = new ListScripts();
                    req.Filter = new GXScript();
                    req.Filter.Methods = new List<GXScriptMethod>(new GXScriptMethod[]
                    {
                        new GXScriptMethod() { Id = _settings.GatewayScriptMethod.Value }
                    });
                    ListScriptsResponse? ret = GXAgentWorker.client.PostAsJson<ListScriptsResponse>("/api/Script/List", req).Result;
                    if (ret?.Scripts?.FirstOrDefault()?.Methods is List<GXScriptMethod> methods)
                    {
                        foreach (var sm in methods)
                        {
                            if (sm.Id == _settings.GatewayScriptMethod.Value)
                            {
                                _gatewayIdentificationScript = sm;
                                //Update parent script.
                                sm.Script = ret.Scripts[0];
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
                _logger.LogInformation("Auto connect service is not used.");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Client has made a connection to the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void OnClientConnected(object sender, Gurux.Common.ConnectionEventArgs e)
        {
            Console.WriteLine("Client {0} is connected.", e.Info);
            GXNet server = (GXNet)sender;
            try
            {
                GXNet media = server.Attach(e);
                var ac = new GXAutoConnect(_serviceProvider,
                    _logger,
                    media,
                    e.Info,
                    _autoConnectionIdentificationScript,
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
        /// Client has close the connection to the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void OnClientDisconnected(object sender, Common.ConnectionEventArgs e)
        {
            Console.WriteLine("Client {0} is disconnecting.", e.Info);
        }

        internal void OnOnReceived(object sender, Common.ReceiveEventArgs e)
        {
            Console.WriteLine("Client {0} is connected.", e.SenderInfo);
            GXNet server = (GXNet)sender;
            try
            {
                GXNet media = server.Attach(e);
                var ac = new GXAutoConnect(_serviceProvider,
                    _logger,
                    media,
                    e.SenderInfo,
                    _autoConnectionIdentificationScript,
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
            _logger.LogInformation("Listener service is stopping.");
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
