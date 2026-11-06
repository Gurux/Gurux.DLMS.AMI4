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
using GXActionBlock = Gurux.DLMS.AMI.Agent.Worker.GXAgentWorker.GXActionBlock;

namespace Gurux.DLMS.AMI.Agent.Worker.Gateway
{
    /// <summary>
    /// Auto connect is used when the meter establishes the connection for the server.
    /// </summary>
    internal class GXGatewayReader
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
        private readonly GXScriptMethod? _gatewayIdentificationScript;
        private readonly IGXTaskNotification _taskNotification;
        private readonly GXNet _media;
        private readonly string _info;
        private readonly GatewaySettings? settings;
        readonly GXByteBuffer _notify = new();
        /// <summary>
        /// Gateway identifier.
        /// </summary>
        private Guid? _gatewayId;
        private readonly AutoResetEvent _connected = new(false);

        public GXGatewayReader(IServiceProvider serviceProvider,
            ILogger? logger,
            GXNet media,
            string info,
            GXScriptMethod? gatewayIdentificationScript,
            AssemblyLoadContext? loadedScript,
            IGXTaskNotification taskNotification, GXAgentWorker worker, CancellationToken cancellationToken = default)
        {
            // Prepared scripts now own their load contexts; retain the legacy constructor parameter.
            _ = loadedScript;
            this.cancellationToken = cancellationToken;
            this.worker = worker;
            _serviceProvider = serviceProvider;
            settings = RuntimeOptions.GatewaySettings;
            _gatewayId = RuntimeOptions.GatewaySettings?.Id;
            _logger = logger;
            _media = media;
            _info = info;
            //Pin the prepared version for this connection; edits apply to later connections.
            _gatewayIdentificationScript = worker.Scripts.Current(gatewayIdentificationScript);
            _taskNotification = taskNotification;
            _media.OnReceived += Media_OnReceived;
            if (settings != null && settings.TraceLevel == TraceLevel.Verbose)
            {
                _media.OnTrace += MediaOnTrace;
                _media.Trace = TraceLevel.Verbose;
            }
            _logger?.LogDebug(new EventId(1001, "GatewayReceiverReady"), "Gateway receiver ready for {Connection}", info);
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
                        Logs = [new GXLog(TraceLevel.Verbose)
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
                GatewaySettings? settings = RuntimeOptions.GatewaySettings;
                if (settings?.TraceLevel > TraceLevel.Warning)
                {
                    if (_logger?.IsEnabled(LogLevel.Information) == true)
                    {
                        _logger.LogInformation("Gateway {Value0} is connected.", _info);
                    }
                }
                if (settings == null)
                {
                    return;
                }
                _gatewayId = settings.Id;
                if (_gatewayId is Guid id2)
                {
                    //If only one gateway is connecting.
                    registration = _taskNotification.Register(id2);
                }
                else if (settings.IdentifyWaitTime != 0)
                {
                    await GXConnectionTasks.WaitAsync(_connected, TimeSpan.FromSeconds(settings.IdentifyWaitTime), cancellationToken);
                    if (_gatewayId == null)
                    {
                        try
                        {
                            if (settings.TraceLevel > TraceLevel.Warning)
                            {
                                if (_logger?.IsEnabled(LogLevel.Information) == true)
                                {
                                    _logger.LogInformation("Gateway {Value0} is connected.", _info);
                                }
                            }
                            AddLog log = new()
                            {
                                Type = settings.TraceLevel.ToString(),
                                Logs = [
                                        new GXLog()
                                {
                                    Creator = RuntimeOptions.Id ,
                                    Message = string.Format("Unknown gateway connected {0}.", _info)
                                }
                                    ]
                            };
                            await client.PostAsJson("/api/Log/Add?source=agent", log, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            if (_logger?.IsEnabled(LogLevel.Error) == true)
                            {
                                _logger.LogError("{Message}", ex.Message + Environment.NewLine + ex.StackTrace);
                            }
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
                                GXAmiScript tmp = new(_serviceProvider)
                                {
                                    Sender = new GXAgent() { Id = RuntimeOptions.Id }
                                };
                                //Run script.
                                var ret = await worker.Scripts.RunAsync(tmp, _gatewayIdentificationScript, [_info, _media, null], cancellationToken);
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
                                Type = TraceLevel.Error.ToString(),
                                Logs = [new GXLog()
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
                                    Type = TraceLevel.Error.ToString(),
                                    Logs = [new GXLog()
                            {
                                Creator = _gatewayIdentificationScript.Script.Id,
                                Message = ex.Message
                            } ]
                                };
                                worker.client.PostAsJson<AddLogResponse>("/api/Log/Add?source=script", se, cancellationToken).Wait();
                            }
                        }
                        if (_gatewayId == null) {
                            return;
                        }
                    }
                }
                if (_gatewayId == null)
                {
                    return;
                }
                //if the connecting gateway is identify with script.
                if (settings.Id == null && settings != null && settings.TraceLevel == TraceLevel.Verbose)
                {
                    if (_logger?.IsEnabled(LogLevel.Information) == true)
                    {
                        _logger.LogInformation("Gateway connected from address: {Value0}", _info);
                    }
                    AddLog log = new()
                    {
                        Type = "Connected",
                        Logs = [new GXLog(TraceLevel.Verbose)
                            {
                                Creator = _gatewayId.Value ,
                                Message = string.Format("Gateway connected from address '{0}'", _info)
                            } ]
                    };
                    await client.PostAsJson("/api/Log/Add?source=gateway", log, cancellationToken);
                }
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!worker.CanClaimTasks) {
                        break;
                    }
                    var response = await client.PostAsJson<GetNextTaskResponse>("/api/Task/Next", new GetNextTask
                    {
                        AgentId = RuntimeOptions.Id, DeviceId = null, GatewayId = _gatewayId,
                        Listener = false, WaitTime = 0
                    }, cancellationToken);
                    var tasks = response?.Tasks?.ToArray();
                    if (tasks == null || tasks.Length == 0) {
                        break;
                    }
                    await worker.ReadMeter(new GXActionBlock
                    {
                        Tasks = tasks, CancellationToken = cancellationToken, Media = _media, GatewayId = _gatewayId
                    }, true, null);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                try
                {
                    if (settings != null && settings.TraceLevel != TraceLevel.Off)
                    {
                        if (_logger?.IsEnabled(LogLevel.Error) == true)
                        {
                            _logger.LogError("{Message}", ex.Message + Environment.NewLine + ex.StackTrace);
                        }
                    }
                    AddLog log = new()
                    {
                        Type = "Error",
                        Logs = [
                            new GXLog(){
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
            _ = new GXReplyData();

            _ = new GXReplyData();
            _notify.Set((byte[])e.Data);
            //If meter establish the connection for the gateway.
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
                        var ret = worker.Scripts.RunAsync(tmp, _gatewayIdentificationScript, [e.SenderInfo, _media, _notify.Array()], cancellationToken).GetAwaiter().GetResult();
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
                                    worker.client.PostAsJson("/api/Gateway/Update", tmp2, cancellationToken)
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
                        registration?.Dispose();
                    }
                    if (_logger?.IsEnabled(LogLevel.Error) == true)
                    {
                        _logger.LogError("{Message}", ex.Message + Environment.NewLine + ex.StackTrace);
                    }
                    AddLog log = new()
                    {
                        Type = TraceLevel.Error.ToString(),
                        Logs = [new GXLog()
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
                            Type = TraceLevel.Error.ToString(),
                            Logs = [new GXLog()
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
        }
    }
}
