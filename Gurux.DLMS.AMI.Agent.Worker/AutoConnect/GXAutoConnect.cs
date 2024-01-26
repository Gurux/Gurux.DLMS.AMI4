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

namespace Gurux.DLMS.AMI.Agent.Worker.AutoConnect
{
    /// <summary>
    /// Auto connect is used when the meter establishes the connection for the server.
    /// </summary>
    internal class GXAutoConnect
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger? _logger;
        private readonly GXScriptMethod? _autoConnectionIdentificationScript;
        private readonly GXScriptMethod? _gatewayIdentificationScript;
        private readonly AssemblyLoadContext? _loadedScript;
        private readonly IGXTaskNotification _taskNotification;
        private readonly GXNet _media;
        private readonly string _info;
        private readonly ListenerSettings? settings;
        GXByteBuffer _notify = new GXByteBuffer();
        private readonly GXDLMSReader reader;
        /// <summary>
        /// Device identifier.
        /// </summary>
        private Guid? _deviceId;
        /// <summary>
        /// Gateway identifier.
        /// </summary>
        private Guid? _gatewayId;
        private AutoResetEvent _connected = new AutoResetEvent(false);

        public GXAutoConnect(IServiceProvider serviceProvider,
            ILogger? logger,
            GXNet media,
            string info,
            GXScriptMethod? autoConnectionIdentificationScript,
            GXScriptMethod? gatewayIdentificationScript,
            AssemblyLoadContext? loadedScript,
            IGXTaskNotification taskNotification)
        {
            _serviceProvider = serviceProvider;
            settings = Options.ListenerSettings;
            _logger = logger;
            _media = media;
            _info = info;
            _autoConnectionIdentificationScript = autoConnectionIdentificationScript;
            _gatewayIdentificationScript = gatewayIdentificationScript;
            _loadedScript = loadedScript;
            _media.OnReceived += Media_OnReceived;
            if (settings != null && settings.TraceLevel == TraceLevel.Verbose)
            {
                _media.OnTrace += _media_OnTrace;
                _media.Trace = TraceLevel.Verbose;
            }
            GXDLMSSecureClient client = new GXDLMSSecureClient(settings.UseLogicalNameReferencing, settings.ClientAddress, settings.ServerAddress, (Authentication)settings.Authentication, settings.Password, (InterfaceType)settings.Interface);
            reader = new GXDLMSReader(client, _media, _logger, TraceLevel.Off,
                settings.TraceLevel, 60000, 3, null);
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
                    log.Logs = new GXAgentLog[]{new GXAgentLog(TraceLevel.Verbose)
                        {
                            Agent = new GXAgent(){Id =GXAgentWorker.Options.Id },
                            Message = str
                        } };
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
                ListenerSettings? settings = Options.ListenerSettings;
                if (settings.IdentifyWaitTime != 0)
                {
                    _connected.WaitOne(settings.IdentifyWaitTime * 1000);
                    if (_deviceId == null && _gatewayId == null)
                    {
                        //_deviceId is null if meter is sending data that is not require 
                        //connection for the meter (e.g keep alive msg).
                        return;
                    }
                }
                if (settings != null && settings.TraceLevel == TraceLevel.Verbose)
                {
                    _logger?.LogInformation("Meter connected from address: {0}", _info);
                    AddAgentLog log = new AddAgentLog();
                    log.Logs = new GXAgentLog[]{new GXAgentLog(TraceLevel.Verbose)
                        {
                            Agent = new GXAgent(){Id =GXAgentWorker.Options.Id },
                            Message = string.Format("Meter Meter connected from address '{0}'", _info)
                        } };
                    await client.PostAsJson("/api/AgentLog/Add", log);
                }
                if (_gatewayId == null && settings != null && !settings.PreEstablished &&
                    (_deviceId == null || _deviceId == Guid.Empty && settings.IdentifyWaitTime == 0))
                {
                    GXDLMSObjectCollection objects = new GXDLMSObjectCollection();
                    GXDLMSData ldn = new GXDLMSData("0.0.42.0.0.255");
                    ldn.SetUIDataType(2, DataType.String);
                    await reader.InitializeConnection(false, false, settings.InvocationCounter);
                    await reader.Read(ldn, 2);
                    if (settings != null && settings.TraceLevel >= TraceLevel.Info)
                    {
                        _logger?.LogInformation("Meter connected: {0}", ldn.Value);
                        AddAgentLog log = new AddAgentLog();
                        log.Logs = new GXAgentLog[]{new GXAgentLog(TraceLevel.Info)
                        {
                            Agent = new GXAgent(){Id =GXAgentWorker.Options.Id },
                            Message = string.Format("Meter '{0}' connected", ldn.Value)
                        } };
                        await GXAgentWorker.client.PostAsJson("/api/AgentLog/Add", log);
                    }
                    //Get device using logical device name.
                    GXAttribute att = new GXAttribute();
                    att.Value = Convert.ToString(ldn.Value);
                    GXObject obj = new GXObject()
                    {
                        Template = new GXObjectTemplate()
                        {
                            LogicalName = ldn.LogicalName
                        },
                        Attributes = new List<GXAttribute>(new GXAttribute[] { att })
                    };
                    ListDevices req = new ListDevices()
                    {
                        Filter = new GXDevice()
                        {
                            Objects = new List<GXObject>(new[] { obj })
                        }
                    };
                    var ret = await client.PostAsJson<ListDevicesResponse>("/api/Device/List", req);
                    if (ret?.Devices == null || !ret.Devices.Any())
                    {
                        //If unknown device.
                        throw new GXAMIUnknownDeviceException(string.Format("Unknown device '{0}'", ldn.Value));
                    }
                    _deviceId = ret.Devices[0].Id;
                }
                if (_gatewayId == null && settings != null &&
                    !settings.PreEstablished && (_deviceId == null || _deviceId == Guid.Empty))
                {
                    //If unknown device.
                    throw new GXAMIUnknownDeviceException(Properties.Resources.UnknownDevice);
                }
                DateTime start = DateTime.Now;
                GXDevice? dev = null;
                //Get next tasks.
                do
                {
                    GetNextTask req2 = new GetNextTask();
                    req2.AgentId = Options.Id;
                    req2.DeviceId = _deviceId;
                    req2.GatewayId = _gatewayId;
                    req2.Listener = true;
                    GetNextTaskResponse? response = await client.PostAsJson<GetNextTaskResponse>("/api/Task/Next", req2);
                    if (response != null && response.Tasks != null && response.Tasks.Any())
                    {
                        GXActionBlock ab = new GXActionBlock()
                        {
                            Tasks = response.Tasks,
                            Media = _media
                        };
                        dev = await GXAgentWorker.ReadMeter(ab, true, settings);
                    }
                    else if (settings != null && settings.TraceLevel == TraceLevel.Verbose)
                    {
                        _logger?.LogInformation(Properties.Resources.NoExecutedTasks);
                        if (_deviceId != null && _deviceId != Guid.Empty)
                        {
                            AddDeviceError error = new AddDeviceError();
                            error.Errors = new GXDeviceError[]{
                new GXDeviceError(TraceLevel.Info)
                        {
                            Device = new GXDevice(){Id = _deviceId.Value},
                            Message = Properties.Resources.NoExecutedTasks
                        } };
                            await client.PostAsJson("/api/DeviceError/Add", error);
                        }

                        AddAgentLog log = new AddAgentLog();
                        log.Logs = new GXAgentLog[]{
                            new GXAgentLog()
                            {
                                Agent = new GXAgent() { Id = Options.Id },
                                Message = Properties.Resources.NoExecutedTasks
                            }
                        };
                        await client.PostAsJson("/api/AgentLog/Add", log);
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
                            _gatewayId = null;
                            break;
                        }
                    }
                }
                while ((settings != null && (settings.ConnectionUpTime == null || (DateTime.Now - start).TotalSeconds < settings.ConnectionUpTime)) ||
                (dev != null && (dev.ConnectionUpTime != null || (DateTime.Now - start).TotalSeconds < dev.ConnectionUpTime)));
            }
            catch (Exception ex)
            {
                try
                {
                    _logger?.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
                    AddAgentLog log = new AddAgentLog();
                    log.Logs = new GXAgentLog[]{
                            new GXAgentLog()
                            {
                                Agent = new GXAgent() { Id = Options.Id },
                                Message = ex.Message,
                                StackTrace = ex.StackTrace
                            }
                        };
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
                if (reader != null)
                {
                    reader.Close();
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
            //If Gateway establish the connection for the agent.
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
                            Parameters = new object[]
                            {
                                    e.SenderInfo,
                                    _media,
                                    _notify.Array()
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
                                        Gateways = new GXGateway[]{gw}
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
                    log.Logs = new GXAgentLog[]{new GXAgentLog()
                        {
                            Agent = new GXAgent(){Id = Options.Id },
                            Message = ex.Message,
                            StackTrace =ex.StackTrace
                        } };
                    GXAgentWorker.client.PostAsJson("/api/AgentLog/Add", log).Wait();
                    if (_gatewayIdentificationScript?.Script != null)
                    {
                        AddScriptLog se = new AddScriptLog();
                        se.Logs = new GXScriptLog[]{new GXScriptLog()
                        {
                            Script = _gatewayIdentificationScript.Script,
                            Message = ex.Message
                        } };
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

            GXDLMSSecureClient client =
                new GXDLMSSecureClient(settings.UseLogicalNameReferencing,
                0,
                0,
                (Authentication)settings.Authentication,
                settings.Password,
                (InterfaceType)settings.Interface);
            client.OnCustomPdu += (args) =>
            {
                try
                {
                    if (_autoConnectionIdentificationScript?.Script?.ByteAssembly != null &&
                        !string.IsNullOrEmpty(_autoConnectionIdentificationScript.Name))
                    {
                        GXAmiScript tmp = new GXAmiScript(_serviceProvider);
                        tmp.Sender = new GXAgent() { Id = Options.Id };
                        if (settings.DefaultDeviceTemplate != null)
                        {
                            tmp.DefaultDeviceTemplate = new GXDeviceTemplate()
                            {
                                Id = settings.DefaultDeviceTemplate.Value
                            };
                        }
                        //Run script.
                        GXScriptRunArgs arg = new GXScriptRunArgs()
                        {
                            ByteAssembly = _autoConnectionIdentificationScript.Script.ByteAssembly,
                            MethodName = _autoConnectionIdentificationScript.Name,
                            Asyncronous = _autoConnectionIdentificationScript.Asyncronous,
                            AssemblyLoadContext = _loadedScript,
                            Parameters = new object[]
                            {
                                    e.SenderInfo,
                                    _media,
                                    args.Data
                            }
                        };
                        var ret = tmp.RunAsync(arg).Result;
                        if ((_deviceId == null || _deviceId == Guid.Empty))
                        {
                            if (ret is Guid id)
                            {
                                //If script returns device ID.
                                _deviceId = id;
                                _connected.Set();
                            }
                            else if (ret == null)
                            {
                                _connected.Set();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
                    AddAgentLog log = new AddAgentLog();
                    log.Logs = new GXAgentLog[]{new GXAgentLog()
                        {
                            Agent = new GXAgent(){Id =GXAgentWorker.Options.Id },
                            Message = ex.Message,
                            StackTrace =ex.StackTrace
                        } };
                    GXAgentWorker.client.PostAsJson("/api/AgentLog/Add", log).Wait();
                    if (_autoConnectionIdentificationScript?.Script != null)
                    {
                        AddScriptLog se = new AddScriptLog();
                        se.Logs = new GXScriptLog[]{new GXScriptLog()
                        {
                            Script = _autoConnectionIdentificationScript.Script,
                            Message = ex.Message
                        } };
                        //Reset parent so it doesn't cause problems with JSON.
                        GXScript original = _autoConnectionIdentificationScript.Script;
                        _autoConnectionIdentificationScript.Script = null;
                        try
                        {
                            GXAgentWorker.client.PostAsJson<AddAgentLogResponse>("/api/ScriptLog/Add", se).Wait();
                        }
                        finally
                        {
                            _autoConnectionIdentificationScript.Script = original;
                        }
                    }
                }
            };
            try
            {
                client.GetData(_notify, reply, notify);
                if (reply.IsComplete && reply.Value != null)
                {
                    _notify.Clear();
                }
                if (notify.IsComplete && notify.Value != null)
                {
                    _notify.Clear();
                }
            }
            catch (Exception)
            {
                _notify.Clear();
            }
        }
    }
}
