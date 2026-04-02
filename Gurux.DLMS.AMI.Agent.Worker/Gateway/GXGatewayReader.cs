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

using Gurux.Common;
using Gurux.DLMS.AMI.Agent.Worker.AutoConnect;
using Gurux.DLMS.AMI.Script;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
using Gurux.DLMS.Secure;
using Gurux.Net;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.Loader;
using static Gurux.DLMS.AMI.Agent.Worker.GXAgentWorker;

namespace Gurux.DLMS.AMI.Agent.Worker.Gateway
{
    /// <summary>
    /// Auto connect is used when the meter establishes the connection for the server.
    /// </summary>
    internal class GXGatewayReader
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger? _logger;
        private readonly GXScriptMethod? _gatewayIdentificationScript;
        private readonly AssemblyLoadContext? _loadedScript;
        private readonly IGXTaskNotification _taskNotification;
        private readonly GXNet _media;
        private readonly string _info;
        private readonly GatewaySettings? settings;
        GXByteBuffer _notify = new GXByteBuffer();
        /// <summary>
        /// Gateway identifier.
        /// </summary>
        private Guid? _gatewayId;
        private AutoResetEvent _connected = new AutoResetEvent(false);

        public GXGatewayReader(IServiceProvider serviceProvider,
            ILogger? logger,
            GXNet media,
            string info,
            GXScriptMethod? gatewayIdentificationScript,
            AssemblyLoadContext? loadedScript,
            IGXTaskNotification taskNotification)
        {
            _serviceProvider = serviceProvider;
            settings = Options.GatewaySettings;
            _gatewayId = Options.GatewaySettings?.Id;
            _logger = logger;
            _media = media;
            _info = info;
            _gatewayIdentificationScript = gatewayIdentificationScript;
            _loadedScript = loadedScript;
            _media.OnReceived += Media_OnReceived;
            if (settings != null && settings.TraceLevel == TraceLevel.Verbose)
            {
                _media.OnTrace += _media_OnTrace;
                _media.Trace = TraceLevel.Verbose;
            }
            _taskNotification = taskNotification;
        }

        private async void _media_OnTrace(object sender, TraceEventArgs e)
        {
            try
            {
                string? str = null;
                if (e.Type == TraceTypes.Sent)
                {
                    str = string.Format("Meter TX: {0}", GXCommon.ToHex((byte[])e.Data));

                }
                else if (e.Type == TraceTypes.Received)
                {
                    str = string.Format("Meter RX: {0}", GXCommon.ToHex((byte[])e.Data));
                }
                if (str != null)
                {
                    _logger?.LogInformation(str);
                    AddAgentLog log = new AddAgentLog();
                    log.Type = TraceLevel.Verbose.ToString();
                    log.Logs = [new GXAgentLog(TraceLevel.Verbose)
                        {
                            Agent = new GXAgent(){Id =GXAgentWorker.Options.Id },
                            Message = str
                        } ];
                    await GXAgentWorker.client.PostAsJson("/api/AgentLog/Add", log);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex.Message);
            }
        }

        /// <summary>
        /// Read data from the meter.
        /// </summary>
        public async void ReadMeter()
        {
            try
            {
                GatewaySettings? settings = Options.GatewaySettings;
                if (settings?.TraceLevel > TraceLevel.Warning)
                {
                    _logger?.LogInformation("Gateway {0} is connected.", _info);
                }
                if (settings == null)
                {
                    return;
                }
                _gatewayId = settings.Id;
                if (_gatewayId is Guid id2)
                {
                    //If only one gateway is connecting.
                    _taskNotification.Register(id2);
                }
                else if (settings.IdentifyWaitTime != 0)
                {
                    _connected.WaitOne(settings.IdentifyWaitTime * 1000);
                    if (_gatewayId == null)
                    {
                        try
                        {
                            if (settings.TraceLevel > TraceLevel.Warning)
                            {
                                _logger?.LogInformation("Gateway {0} is connected.", _info);
                            }
                            AddAgentLog log = new AddAgentLog();
                            log.Type = settings.TraceLevel.ToString();
                            log.Logs = [
                                    new GXAgentLog()
                            {
                                Agent = new GXAgent() { Id = Options.Id },
                                Message = string.Format("Unknown gateway connected {0}.", _info)
                            }
                                ];
                            await client.PostAsJson("/api/AgentLog/Add", log);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
                        }
                        return;
                    }
                }
                else
                {
                    //If Gateway is identified by IP address.
                    if (_gatewayIdentificationScript != null)
                    {
                        try
                        {
                            if (_gatewayIdentificationScript?.Script?.ByteAssembly != null &&
                                !string.IsNullOrEmpty(_gatewayIdentificationScript.Name))
                            {
                                GXAmiScript tmp = new GXAmiScript(_serviceProvider);
                                tmp.Sender = new GXAgent() { Id = Options.Id };
                                //Run script.
                                GXScriptRunArgs arg = new GXScriptRunArgs()
                                {
                                    ByteAssembly = _gatewayIdentificationScript.Script.ByteAssembly,
                                    MethodName = _gatewayIdentificationScript.Name,
                                    Asyncronous = _gatewayIdentificationScript.Asyncronous,
                                    AssemblyLoadContext = _loadedScript,
                                    Parameters = new object?[]
                                    {
                                    _info,
                                    _media,
                                    null
                                    }
                                };
                                var ret = tmp.RunAsync(arg).Result;
                                if ((_gatewayId == null || _gatewayId == Guid.Empty))
                                {
                                    if (ret is GXGateway gw && gw.Id != Guid.Empty)
                                    {
                                        //If script returns gateway.
                                        _gatewayId = gw.Id;
                                        _taskNotification.Register(gw.Id);
                                        if (gw.Agent?.Id != Options.Id)
                                        {
                                            //Update the gateway agent if it has changed.
                                            gw.Agent = new GXAgent() { Id = Options.Id };
                                            UpdateGateway tmp2 = new UpdateGateway()
                                            {
                                                Gateways = new GXGateway[] { gw }
                                            };
                                            GXAgentWorker.client.PostAsJson("/api/Gateway/Update", tmp2).Wait();
                                        }
                                        _connected.Set();
                                    }
                                    else if (ret == null)
                                    {
                                        _connected.Set();
                                    }
                                }
                            }
                            return;
                        }
                        catch (Exception ex)
                        {
                            if (_gatewayId is Guid id)
                            {
                                _taskNotification.Unregister(id);
                            }
                            _logger?.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
                            AddAgentLog log = new AddAgentLog();
                            log.Type = TraceLevel.Error.ToString();
                            log.Logs = [new GXAgentLog()
                        {
                            Agent = new GXAgent(){Id = Options.Id },
                            Message = ex.Message,
                            StackTrace =ex.StackTrace
                        } ];
                            GXAgentWorker.client.PostAsJson("/api/AgentLog/Add", log).Wait();
                            if (_gatewayIdentificationScript?.Script != null)
                            {
                                AddScriptLog se = new AddScriptLog();
                                se.Type = TraceLevel.Error.ToString();
                                se.Logs = [new GXScriptLog()
                        {
                            Script = _gatewayIdentificationScript.Script,
                            Message = ex.Message
                        } ];
                                //Reset parent so it doesn't cause problems with JSON.
                                GXScript original = _gatewayIdentificationScript.Script;
                                _gatewayIdentificationScript.Script = null;
                                try
                                {
                                    GXAgentWorker.client.PostAsJson<AddScriptLogResponse>("/api/ScriptLog/Add", se).Wait();
                                }
                                finally
                                {
                                    _gatewayIdentificationScript.Script = original;
                                }
                            }
                        }
                        return;
                    }
                }
                if (_gatewayId == null)
                {
                    return;
                }
                //if the connecting gateway is identify with script.
                if (settings.Id == null && settings != null && settings.TraceLevel == TraceLevel.Verbose)
                {
                    _logger?.LogInformation("Gateway connected from address: {0}", _info);
                    AddGatewayLog log = new AddGatewayLog();
                    log.Type = "Connected";
                    log.Logs = [new GXGatewayLog(TraceLevel.Verbose)
                        {
                            Gateway = new GXGateway(){Id =_gatewayId.Value },
                            Message = string.Format("Gateway connected from address '{0}'", _info)
                        } ];
                    await client.PostAsJson("/api/GatewayLog/Add", log);
                }
                DateTime start = DateTime.Now;
                GXDevice? dev = null;
                //Get next tasks.
                do
                {
                    GetNextTask req2 = new GetNextTask();
                    req2.AgentId = Options.Id;
                    req2.GatewayId = _gatewayId;
                    GetNextTaskResponse? response = await client.PostAsJson<GetNextTaskResponse>("/api/Task/Next", req2);
                    if (response != null && response.Tasks != null && response.Tasks.Any())
                    {
                        GXActionBlock ab = new GXActionBlock()
                        {
                            Tasks = response.Tasks,
                            Media = _media
                        };
                        dev = await GXAgentWorker.ReadMeter(ab, true, null);
                    }
                    else if (settings != null && settings.TraceLevel == TraceLevel.Verbose)
                    {
                        _logger?.LogInformation(Properties.Resources.NoExecutedTasks);
                        AddGatewayLog log = new AddGatewayLog();
                        log.Type = "Task";
                        log.Logs = [
                            new GXGatewayLog()
                            {
                                Gateway = new GXGateway(){Id =_gatewayId.Value },
                                Message = Properties.Resources.NoExecutedTasks
                            }
                        ];
                        await client.PostAsJson("/api/GatewayLog/Add", log);
                    }
                    //Wait until the next message is received.
                    if (_gatewayId is Guid id)
                    {
                        try
                        {
                            _taskNotification.Wait(id);
                        }
                        catch (Exception)
                        {
                            //Exception is thrown when GW established a new connection and old is closed.
                            //This is necessary so that the new connection is not unregistered.
                            _taskNotification.Unregister(id);
                            _gatewayId = null;
                            break;
                        }
                    }
                }
                while (true);
            }
            catch (Exception ex)
            {
                try
                {
                    if (settings.TraceLevel != TraceLevel.Off)
                    {
                        _logger?.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                    AddAgentLog log = new AddAgentLog();
                    log.Type = "Error";
                    log.Logs = [
                        new GXAgentLog(){
                        Agent = new GXAgent() { Id = Options.Id },
                                Message = ex.Message,
                                StackTrace = ex.StackTrace
                            }
                        ];
                    await client.PostAsJson("/api/AgentLog/Add", log);
                }
                catch (Exception ex2)
                {
                    _logger?.LogError(ex2.Message + Environment.NewLine + ex2.StackTrace);
                }
            }
            finally
            {
                if (_gatewayId is Guid id)
                {
                    _taskNotification.Unregister(id);
                }
            }
        }

        /// <summary>
        /// Handle notification and push messages.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Media_OnReceived(object sender, ReceiveEventArgs e)
        {
            GXReplyData reply = new GXReplyData();
            GXReplyData notify = new GXReplyData();
            _notify.Set((byte[])e.Data);
            //If meter establish the connection for the gateway.
            if (_gatewayIdentificationScript != null)
            {
                try
                {
                    if (_gatewayIdentificationScript?.Script?.ByteAssembly != null &&
                        !string.IsNullOrEmpty(_gatewayIdentificationScript.Name))
                    {
                        GXAmiScript tmp = new GXAmiScript(_serviceProvider);
                        tmp.Sender = new GXAgent() { Id = Options.Id };
                        //Run script.
                        GXScriptRunArgs arg = new GXScriptRunArgs()
                        {
                            ByteAssembly = _gatewayIdentificationScript.Script.ByteAssembly,
                            MethodName = _gatewayIdentificationScript.Name,
                            Asyncronous = _gatewayIdentificationScript.Asyncronous,
                            AssemblyLoadContext = _loadedScript,
                            Parameters =
                            [
                                    e.SenderInfo,
                                    _media,
                                    _notify.Array()
                            ]
                        };
                        var ret = tmp.RunAsync(arg).Result;
                        if ((_gatewayId == null || _gatewayId == Guid.Empty))
                        {
                            if (ret is GXGateway gw && gw.Id != Guid.Empty)
                            {
                                //If script returns gateway.
                                _gatewayId = gw.Id;
                                _taskNotification.Register(gw.Id);
                                if (gw.Agent?.Id != Options.Id)
                                {
                                    //Update the gateway agent if it has changed.
                                    gw.Agent = new GXAgent() { Id = Options.Id };
                                    UpdateGateway tmp2 = new UpdateGateway()
                                    {
                                        Gateways = [gw]
                                    };
                                    GXAgentWorker.client.PostAsJson("/api/Gateway/Update", tmp2)
                                        .Wait();
                                }
                                _connected.Set();
                            }
                            else if (ret == null)
                            {
                                _connected.Set();
                            }
                        }
                    }
                    return;
                }
                catch (Exception ex)
                {
                    if (_gatewayId is Guid id)
                    {
                        _taskNotification.Unregister(id);
                    }
                    _logger?.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
                    AddAgentLog log = new AddAgentLog();
                    log.Type = TraceLevel.Error.ToString();
                    log.Logs = [new GXAgentLog()
                        {
                            Agent = new GXAgent(){Id = Options.Id },
                            Message = ex.Message,
                            StackTrace =ex.StackTrace
                        } ];
                    GXAgentWorker.client.PostAsJson("/api/AgentLog/Add", log).Wait();
                    if (_gatewayIdentificationScript?.Script != null)
                    {
                        AddScriptLog se = new AddScriptLog();
                        se.Type = TraceLevel.Error.ToString();
                        se.Logs = [new GXScriptLog()
                        {
                            Script = _gatewayIdentificationScript.Script,
                            Message = ex.Message
                        } ];
                        //Reset parent so it doesn't cause problems with JSON.
                        GXScript original = _gatewayIdentificationScript.Script;
                        _gatewayIdentificationScript.Script = null;
                        try
                        {
                            GXAgentWorker.client.PostAsJson<AddAgentLogResponse>("/api/ScriptLog/Add", se).Wait();
                        }
                        finally
                        {
                            _gatewayIdentificationScript.Script = original;
                        }
                    }
                }
                return;
            }            
        }
    }
}
