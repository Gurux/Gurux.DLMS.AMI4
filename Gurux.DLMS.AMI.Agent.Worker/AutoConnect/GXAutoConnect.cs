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
        private readonly GXScriptMethod? _scriptMethod;
        private readonly AssemblyLoadContext? _loadedScript;
        private readonly GXNet _media;
        private readonly string _info;
        private readonly ListenerSettings? settings;
        GXByteBuffer _notify = new GXByteBuffer();
        private readonly GXDLMSReader reader;
        private Guid _deviceId;

        public GXAutoConnect(IServiceProvider serviceProvider,
            ILogger? logger,
            GXNet media,
            string info,
            GXScriptMethod? scriptMethod,
            AssemblyLoadContext? loadedScript)
        {
            _serviceProvider = serviceProvider;
            settings = Options.ListenerSettings;
            _logger = logger;
            _media = media;
            _info = info;
            _scriptMethod = scriptMethod;
            _loadedScript = loadedScript;
            _media.OnReceived += Media_OnReceived;
            GXDLMSSecureClient client = new GXDLMSSecureClient(settings.UseLogicalNameReferencing, settings.ClientAddress, settings.ServerAddress, (Authentication)settings.Authentication, settings.Password, (InterfaceType)settings.Interface);
            reader = new GXDLMSReader(client, _media, _logger, TraceLevel.Off,
                settings.TraceLevel, 60000, 3, null);
        }

        /// <summary>
        /// Read data from the meter.
        /// </summary>
        public async void ReadMeter()
        {
            try
            {
                ListenerSettings? settings = GXAgentWorker.Options.ListenerSettings;
                if (settings.IdentifyWaitTime != 0)
                {
                    Thread.Sleep(settings.IdentifyWaitTime * 1000);
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
                if (_deviceId == Guid.Empty && settings != null && settings.IdentifyWaitTime == 0)
                {
                    GXDLMSObjectCollection objects = new GXDLMSObjectCollection();
                    GXDLMSData ldn = new GXDLMSData("0.0.42.0.0.255");
                    ldn.SetUIDataType(2, DataType.String);
                    await reader.InitializeConnection(false, settings.InvocationCounter);
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
                if (_deviceId == Guid.Empty)
                {
                    //If unknown device.
                    throw new GXAMIUnknownDeviceException("Unknown device.");
                }
                //Get next tasks.
                {
                    GetNextTask req2 = new GetNextTask();
                    req2.AgentId = Options.Id;
                    req2.DeviceId = _deviceId;
                    req2.Listener = true;
                    GetNextTaskResponse? response = await GXAgentWorker.client.PostAsJson<GetNextTaskResponse>("/api/Task/Next", req2);
                    if (response != null && response.Tasks != null && response.Tasks.Any())
                    {
                        GXActionBlock ab = new GXActionBlock()
                        {
                            Tasks = response.Tasks,
                            Media = _media
                        };
                        await GXAgentWorker.ReadMeter(ab);
                    }
                    else if (settings != null && settings.TraceLevel == TraceLevel.Verbose)
                    {
                        _logger?.LogInformation(Properties.Resources.NoExecutedTasks);
                        if (_deviceId != Guid.Empty)
                        {
                            AddDeviceError error = new AddDeviceError();
                            error.Errors = new GXDeviceError[]{
                new GXDeviceError(TraceLevel.Info)
                        {
                            Device = new GXDevice(){Id = _deviceId},
                            Message = Properties.Resources.NoExecutedTasks
                        } };
                            _logger?.LogError(error.Errors[0].Message);
                            await client.PostAsJson("/api/DeviceError/Add", error);
                        }

                        AddAgentLog log = new AddAgentLog();
                        log.Logs = new GXAgentLog[]{
                            new GXAgentLog()
                            {
                                Agent = new GXAgent() { Id = GXAgentWorker.Options.Id },
                                Message = Properties.Resources.NoExecutedTasks
                            }
                        };
                        await GXAgentWorker.client.PostAsJson("/api/AgentLog/Add", log);

                    }
                }
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
                                Agent = new GXAgent() { Id = GXAgentWorker.Options.Id },
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
        private async void Media_OnReceived(object sender, ReceiveEventArgs e)
        {
            string str = string.Format("Meter notify: {0}", GXCommon.ToHex((byte[])e.Data));
            if (settings != null && settings.TraceLevel == TraceLevel.Verbose)
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
            GXReplyData reply = new GXReplyData();
            GXReplyData notify = new GXReplyData();
            _notify.Set((byte[])e.Data);
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
                    if (_scriptMethod?.Script?.ByteAssembly != null &&
                        !string.IsNullOrEmpty(_scriptMethod.Name))
                    {
                        GXAmiScript tmp = new GXAmiScript(_serviceProvider);
                        tmp.Sender = new GXAgent() { Id = GXAgentWorker.Options.Id };
                        //Run script.
                        GXScriptRunArgs arg = new GXScriptRunArgs()
                        {
                            ByteAssembly = _scriptMethod.Script.ByteAssembly,
                            MethodName = _scriptMethod.Name,
                            Asyncronous = _scriptMethod.Asyncronous,
                            AssemblyLoadContext = _loadedScript,
                            Parameters = new object[]
                            {
                                    e.SenderInfo,
                                    _media,
                                    args.Data
                            }
                        };
                        var ret = tmp.RunAsync(arg).Result;
                        if (_deviceId == Guid.Empty && ret is Guid id)
                        {
                            //If script returns device ID.
                            _deviceId = id;
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
                    if (_scriptMethod?.Script != null)
                    {
                        AddScriptLog se = new AddScriptLog();
                        se.Logs = new GXScriptLog[]{new GXScriptLog()
                        {
                            Script = _scriptMethod.Script,
                            Message = ex.Message
                        } };
                        //Reset parent so it doesn't cause problems with JSON.
                        GXScript original = _scriptMethod.Script;
                        _scriptMethod.Script = null;
                        try
                        {
                            GXAgentWorker.client.PostAsJson<AddAgentLogResponse>("/api/ScriptLog/Add", se).Wait();
                        }
                        finally
                        {
                            _scriptMethod.Script = original;
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
