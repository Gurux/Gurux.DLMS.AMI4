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

using Gurux.DLMS.AMI.Shared.DTOs.Log;

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
using GXActionBlock = Gurux.DLMS.AMI.Agent.Worker.GXAgentWorker.GXActionBlock;

namespace Gurux.DLMS.AMI.Agent.Worker.AutoConnect
{
    /// <summary>
    /// Auto connect is used when the meter establishes the connection for the server.
    /// </summary>
    internal class GXAutoConnect
    {
        private readonly CancellationToken cancellationToken;
        private GXTaskRegistration? registration;
        private readonly object callbackGate = new();
        private readonly object traceGate = new();
        private bool closing;
        private readonly GXAgentWorker worker;
        private Gurux.DLMS.AMI.Agent.Shared.GXAgentOptions RuntimeOptions => worker.RuntimeOptions;
        private HttpClient client => worker.client;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger? _logger;
        private readonly GXScriptMethod? _autoConnectionIdentificationScript;
        private readonly GXScriptMethod? _gatewayIdentificationScript;
        private readonly IGXTaskNotification _taskNotification;
        private readonly GXNet _media;
        private readonly string _info;
        private readonly ListenerSettings settings;
        readonly GXByteBuffer _notify = new();
        private readonly GXDLMSReader reader;
        /// <summary>
        /// Device identifier.
        /// </summary>
        private Guid? _deviceId;
        /// <summary>
        /// Gateway identifier.
        /// </summary>
        private Guid? _gatewayId;
        private readonly AutoResetEvent _connected = new(false);

        public GXAutoConnect(IServiceProvider serviceProvider,
            ILogger? logger,
            GXNet media,
            string info,
            GXScriptMethod? autoConnectionIdentificationScript,
            GXScriptMethod? gatewayIdentificationScript,
            AssemblyLoadContext? loadedScript,
            IGXTaskNotification taskNotification, GXAgentWorker worker, CancellationToken cancellationToken = default)
        {
            // Prepared scripts now own their load contexts; retain the legacy constructor parameter.
            _ = loadedScript;
            this.cancellationToken = cancellationToken;
            this.worker = worker;
            _serviceProvider = serviceProvider;
            settings = RuntimeOptions.ListenerSettings
                ?? throw new InvalidOperationException("Listener settings are missing.");
            _logger = logger;
            _media = media;
            _info = info;
            _autoConnectionIdentificationScript = worker.Scripts.Current(autoConnectionIdentificationScript);
            //Pin the prepared version for this connection; edits apply to later connections.
            _gatewayIdentificationScript = worker.Scripts.Current(gatewayIdentificationScript);
            _taskNotification = taskNotification;
            _media.OnReceived += Media_OnReceived;
            if (settings.TraceLevel == TraceLevel.Verbose)
            {
                _media.OnTrace += MediaOnTrace;
                _media.Trace = TraceLevel.Verbose;
            }
            GXDLMSSecureClient client = new(settings.UseLogicalNameReferencing, settings.ClientAddress, settings.ServerAddress, (Authentication)settings.Authentication, settings.Password, (InterfaceType)settings.Interface);
            reader = new GXDLMSReader(client, _media, _logger, TraceLevel.Off,
                settings.TraceLevel, 60, 3, null, worker.client) { CancellationToken = cancellationToken };
        }

        private void MediaOnTrace(object sender, TraceEventArgs e)
        {
            lock (traceGate)
            {
                if (Volatile.Read(ref closing) || cancellationToken.IsCancellationRequested) {
                    return;
                }
                MediaOnTraceCore(sender, e);
            }
        }

        private void MediaOnTraceCore(object sender, TraceEventArgs e)
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
                    if (_logger?.IsEnabled(LogLevel.Information) == true)
                    {
                        _logger.LogInformation("{Message}", str);
                    }
                    AddLog log = new()
                    {
                        Type = TraceLevel.Verbose.ToString(),
                        Logs = [new(TraceLevel.Verbose)
                            {
                                Creator = worker.RuntimeOptions.Id ,
                                Message = str
                            } ]
                    };
                    worker.client.PostAsJson("/api/Log/Add?source=agent", log, cancellationToken).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                if (_logger?.IsEnabled(LogLevel.Error) == true)
                {
                    _logger.LogError("{Message}", ex.Message);
                }
            }
        }

        /// <summary>
        /// Read data from the meter.
        /// </summary>
        public async System.Threading.Tasks.Task ReadMeter()
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                ListenerSettings settings = RuntimeOptions.ListenerSettings
                    ?? throw new InvalidOperationException("Listener settings are missing.");
                if (settings.IdentifyWaitTime != 0)
                {
                    await GXConnectionTasks.WaitAsync(_connected, TimeSpan.FromSeconds(settings.IdentifyWaitTime), cancellationToken);
                    if (_deviceId == null && _gatewayId == null)
                    {
                        //_deviceId is null if meter is sending data that is not require
                        //connection for the meter (e.g keep alive msg).
                        return;
                    }
                }
                if (settings.TraceLevel == TraceLevel.Verbose)
                {
                    if (_logger?.IsEnabled(LogLevel.Information) == true)
                    {
                        _logger.LogInformation("Meter connected from address: {Value0}", _info);
                    }
                    AddLog log = new()
                    {
                        Type = TraceLevel.Verbose.ToString(),
                        Logs = [new(TraceLevel.Verbose)
                            {
                                Creator = worker.RuntimeOptions.Id ,
                                Message = string.Format("Meter Meter connected from address '{0}'", _info)
                            } ]
                    };
                    await client.PostAsJson("/api/Log/Add?source=agent", log, cancellationToken);
                }
                if (_gatewayId == null && !settings.PreEstablished &&
                    (_deviceId == null || _deviceId == Guid.Empty && settings.IdentifyWaitTime == 0))
                {
                    GXDLMSObjectCollection objects = [];
                    GXDLMSData ldn = new("0.0.42.0.0.255");
                    ldn.SetUIDataType(2, DataType.String);
                    await reader.InitializeConnection(false, false, settings.InvocationCounter);
                    await reader.Read(ldn, 2);
                    if (settings.TraceLevel >= TraceLevel.Info)
                    {
                        if (_logger?.IsEnabled(LogLevel.Information) == true)
                        {
                            _logger.LogInformation("Meter connected: {Value0}", ldn.Value);
                        }
                        AddLog log = new()
                        {
                            Logs = [new(TraceLevel.Info)
                            {
                                Creator = worker.RuntimeOptions.Id ,
                                Message = string.Format("Meter '{0}' connected", ldn.Value)
                            } ]
                        };
                        await worker.client.PostAsJson("/api/Log/Add?source=agent", log, cancellationToken);
                    }
                    //Get device using logical device name.
                    GXAttribute att = new()
                    {
                        Value = Convert.ToString(ldn.Value)
                    };
                    GXObject obj = new()
                    {
                        Template = new GXObjectTemplate()
                        {
                            LogicalName = ldn.LogicalName
                        },
                        Attributes = new List<GXAttribute>([att])
                    };
                    ListDevices req = new()
                    {
                        Filter = new GXDevice()
                        {
                            Objects = new List<GXObject>([obj])
                        }
                    };
                    var ret = await client.PostAsJson<ListDevicesResponse>("/api/Device/List", req, cancellationToken);
                    GXDevice device = ret?.Devices?.FirstOrDefault()
                        ?? throw new KeyNotFoundException($"Unknown device '{ldn.Value}'.");
                    _deviceId = device.Id;
                }
                if (_gatewayId == null &&
                    !settings.PreEstablished && (_deviceId == null || _deviceId == Guid.Empty))
                {
                    //If unknown device.
                    //TODO: throw new GXAMIUnknownDeviceException(Properties.Resources.UnknownDevice);
                }
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!worker.CanClaimTasks) {
                        break;
                    }
                    var response = await client.PostAsJson<GetNextTaskResponse>("/api/Task/Next", new GetNextTask
                    {
                        AgentId = RuntimeOptions.Id, DeviceId = _deviceId, GatewayId = _gatewayId,
                        Listener = true, WaitTime = 0
                    }, cancellationToken);
                    var tasks = response?.Tasks?.ToArray();
                    if (tasks == null || tasks.Length == 0) {
                        break;
                    }
                    await worker.ReadMeter(new GXActionBlock
                    {
                        Tasks = tasks, CancellationToken = cancellationToken, Media = _media, GatewayId = _gatewayId
                    }, true, settings);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                try
                {
                    if (_logger?.IsEnabled(LogLevel.Error) == true)
                    {
                        _logger.LogError("{Message}", ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                    AddLog log = new()
                    {
                        Logs = [
                                new()
                                {
                                    Creator = RuntimeOptions.Id ,
                                    Message = ex.Message,
                                    StackTrace = ex.StackTrace
                                }
                            ]
                    };
                    await client.PostAsJson("/api/Log/Add?source=agent", log, cancellationToken);
                }
                catch (Exception ex2)
                {
                    if (_logger?.IsEnabled(LogLevel.Error) == true)
                    {
                        _logger.LogError("{Message}", ex2.Message + Environment.NewLine + ex2.StackTrace);
                    }
                }
            }
            finally
            {
                _media.OnReceived -= Media_OnReceived;
                _media.OnTrace -= MediaOnTrace;
                lock (callbackGate)
                {
                    Volatile.Write(ref closing, true);
                    registration?.Dispose();
                }
                lock (traceGate) { /* Wait for any active trace callback before releasing dependencies. */ }
                _connected.Dispose();
                _media.Close();

                reader?.Close();
            }
        }

        /// <summary>
        /// Handle notification and push messages.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Media_OnReceived(object sender, ReceiveEventArgs e)
        {
            lock (callbackGate)
            {
                if (Volatile.Read(ref closing) || cancellationToken.IsCancellationRequested) {
                    return;
                }
                try { Media_OnReceivedCore(sender, e); }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
                catch (Exception error) { _logger?.LogError(error, "Processing incoming connection data failed."); }
            }
        }

        private void Media_OnReceivedCore(object sender, ReceiveEventArgs e)
        {
            GXReplyData reply = new();
            GXReplyData notify = new();
            _notify.Set((byte[])e.Data);
            //If Gateway establish the connection for the agent.
            if (_gatewayIdentificationScript != null)
            {
                try
                {
                    if (_gatewayIdentificationScript?.Script?.ByteAssembly != null &&
                        !string.IsNullOrEmpty(_gatewayIdentificationScript.Name))
                    {
                        GXAmiScript tmp = new(_serviceProvider)
                        {
                            Sender = new GXAgent() { Id = RuntimeOptions.Id }
                        };
                        //Run script.
                        var ret = worker.Scripts.RunAsync(tmp, _gatewayIdentificationScript, [e.SenderInfo, _media, _notify.Array()]).GetAwaiter().GetResult();
                        if ((_gatewayId == null || _gatewayId == Guid.Empty))
                        {
                            if (ret is GXGateway gw && gw.Id != Guid.Empty)
                            {
                                //If script returns gateway.
                                _gatewayId = gw.Id;
                                registration = _taskNotification.Register(gw.Id);
                                if (gw.Agent?.Id != RuntimeOptions.Id)
                                {
                                    //Update the gateway agent if it has changed.
                                    gw.Agent = new GXAgent() { Id = RuntimeOptions.Id };
                                    UpdateGateway tmp2 = new()
                                    {
                                        Gateways = [gw]
                                    };
                                    worker.client.PostAsJson("/api/Gateway/Update", tmp2, cancellationToken).Wait();
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
                        registration?.Dispose();
                    }
                    if (_logger?.IsEnabled(LogLevel.Error) == true)
                    {
                        _logger.LogError("{Message}", ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                    AddLog log = new()
                    {
                        Logs = [new()
                            {
                                Creator = RuntimeOptions.Id ,
                                Message = ex.Message,
                                StackTrace =ex.StackTrace
                            } ]
                    };
                    worker.client.PostAsJson("/api/Log/Add?source=agent", log, cancellationToken).Wait();
                    if (_gatewayIdentificationScript?.Script != null)
                    {
                        AddLog se = new()
                        {
                            Logs = [new()
                            {
                                Creator = _gatewayIdentificationScript.Script.Id,
                                Message = ex.Message
                            } ]
                        };
                        worker.client.PostAsJson<AddLogResponse>("/api/Log/Add?source=script", se, cancellationToken).Wait();
                    }
                }
                return;
            }

            GXDLMSSecureClient client =
                new(settings.UseLogicalNameReferencing,
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
                        GXAmiScript tmp = new(_serviceProvider)
                        {
                            Sender = new GXAgent() { Id = RuntimeOptions.Id }
                        };
                        if (settings.DefaultDeviceTemplate != null)
                        {
                            tmp.DefaultDeviceTemplate = new GXDeviceTemplate()
                            {
                                Id = settings.DefaultDeviceTemplate.Value
                            };
                        }
                        // Run the prepared script version pinned for this connection.
                        var ret = worker.Scripts.RunAsync(tmp, _autoConnectionIdentificationScript,
                            [e.SenderInfo, _media, args.Data]).GetAwaiter().GetResult();
                        if (_deviceId == null || _deviceId == Guid.Empty)
                        {
                            if (ret is Guid id)
                            {
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
                    if (_logger?.IsEnabled(LogLevel.Error) == true)
                    {
                        _logger.LogError("{Message}", ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                    AddLog log = new()
                    {
                        Logs = [new()
                        {
                            Creator = RuntimeOptions.Id,
                            Message = ex.Message,
                            StackTrace = ex.StackTrace
                        }]
                    };
                    worker.client.PostAsJson("/api/Log/Add?source=agent", log, cancellationToken).Wait();
                    if (_autoConnectionIdentificationScript?.Script != null)
                    {
                        AddLog se = new()
                        {
                            Logs = [new()
                            {
                                Creator = _autoConnectionIdentificationScript.Script.Id,
                                Message = ex.Message
                            }]
                        };
                        worker.client.PostAsJson<AddLogResponse>("/api/Log/Add?source=script", se, cancellationToken).Wait();
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
