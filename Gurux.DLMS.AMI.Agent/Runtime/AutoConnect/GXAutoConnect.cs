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
        /// <summary>Cancels operations when the enclosing host or connection stops.</summary>
        private readonly CancellationToken _cancellationToken;
        /// <summary>Task notification registration belonging to the identified connection.</summary>
        private GXTaskRegistration? _registration;
        /// <summary>Serializes callbacks with connection shutdown.</summary>
        private readonly object _callbackGate = new();
        /// <summary>Protects concurrent trace output for the connection.</summary>
        private readonly object _traceGate = new();
        /// <summary>Indicates that connection shutdown has started.</summary>
        private bool _closing;
        /// <summary>Agent runtime controlled by this host or connection.</summary>
        private readonly GXAgentWorker _worker;
        /// <summary>Gets the current validated worker configuration without copying it.</summary>
        private Gurux.DLMS.AMI.Agent.Shared.GXAgentOptions RuntimeOptions => _worker.RuntimeOptions;
        /// <summary>Gets the HTTP client owned by the current worker.</summary>
        private HttpClient Client => _worker.Client;
        /// <summary>Resolves transport and repository services for this component.</summary>
        private readonly IServiceProvider _serviceProvider;
        /// <summary>Records runtime diagnostics without exposing authentication credentials.</summary>
        private readonly ILogger? _logger;
        /// <summary>Optional script used to identify an automatically connecting meter.</summary>
        private readonly GXScriptMethod? _autoConnectionIdentificationScript;
        /// <summary>Optional script used to identify a connecting gateway.</summary>
        private readonly GXScriptMethod? _gatewayIdentificationScript;
        /// <summary>Routes newly available task signals to registered device connections.</summary>
        private readonly IGXTaskNotification _taskNotification;
        /// <summary>Transport used to exchange bytes with the connected device.</summary>
        private readonly GXNet _media;
        /// <summary>Connection description attached to diagnostic output.</summary>
        private readonly string _info;
        /// <summary>Transport settings applied to the active connection.</summary>
        private readonly ListenerSettings _settings;
        /// <summary>Accumulates incoming bytes until a notification can be decoded.</summary>
        readonly GXByteBuffer _notify = new();
        /// <summary>Reads protocol requests for the connected device.</summary>
        private readonly GXDLMSReader _reader;
        /// <summary>
        /// Device identifier.
        /// </summary>
        private Guid? _deviceId;
        /// <summary>
        /// Gateway identifier.
        /// </summary>
        private Guid? _gatewayId;
        /// <summary>Signals completion of connection establishment.</summary>
        private readonly AutoResetEvent _connected = new(false);

        /// <summary>Initializes an accepted automatic device connection and its protocol reader.</summary>
        /// <param name="serviceProvider">Resolves services used by accepted transport sessions.</param>
        /// <param name="logger">Records operation diagnostics without exposing authentication secrets.</param>
        /// <param name="media">Accepted transport used for connection protocol exchanges.</param>
        /// <param name="info">Process start settings or transport description used by the operation.</param>
        /// <param name="autoConnectionIdentificationScript">Optional script identifying an automatically connecting meter.</param>
        /// <param name="gatewayIdentificationScript">Optional script identifying the accepted gateway.</param>
        /// <param name="loadedScript">Retains the loaded identification script for the session lifetime.</param>
        /// <param name="taskNotification">Routes available task signals to the identified connection.</param>
        /// <param name="worker">Agent runtime being registered or reconfigured.</param>
        /// <param name="cancellationToken">Cancels the operation when its owning lifetime ends.</param>
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
            this._cancellationToken = cancellationToken;
            this._worker = worker;
            _serviceProvider = serviceProvider;
            _settings = RuntimeOptions.ListenerSettings
                ?? throw new InvalidOperationException("Listener settings are missing.");
            _logger = logger;
            _media = media;
            _info = info;
            _autoConnectionIdentificationScript = worker.Scripts.Current(autoConnectionIdentificationScript);
            //Pin the prepared version for this connection; edits apply to later connections.
            _gatewayIdentificationScript = worker.Scripts.Current(gatewayIdentificationScript);
            _taskNotification = taskNotification;
            _media.OnReceived += Media_OnReceived;
            if (_settings.TraceLevel == TraceLevel.Verbose)
            {
                _media.OnTrace += MediaOnTrace;
                _media.Trace = TraceLevel.Verbose;
            }
            GXDLMSSecureClient client = new(_settings.UseLogicalNameReferencing, _settings.ClientAddress, _settings.ServerAddress, (Authentication)_settings.Authentication, _settings.Password, (InterfaceType)_settings.Interface);
            _reader = new GXDLMSReader(client, _media, _logger, TraceLevel.Off,
                _settings.TraceLevel, 60, 3, null, worker.Client) { CancellationToken = cancellationToken };
        }

        /// <summary>Serializes transport trace callbacks with connection shutdown.</summary>
        /// <param name="sender">Transport identity or event source that produced the notification.</param>
        /// <param name="e">Transport event data identifying the connection, received bytes or trace packet.</param>
        private void MediaOnTrace(object? sender, TraceEventArgs e)
        {
            lock (_traceGate)
            {
                if (Volatile.Read(ref _closing) || _cancellationToken.IsCancellationRequested) {
                    return;
                }
                MediaOnTraceCore(sender, e);
            }
        }

        /// <summary>Writes transport trace data using the configured diagnostic destinations.</summary>
        /// <param name="sender">Transport identity or event source that produced the notification.</param>
        /// <param name="e">Transport event data identifying the connection, received bytes or trace packet.</param>
        private void MediaOnTraceCore(object? sender, TraceEventArgs e)
        {
            try
            {
                string? str = null;
                if (e.Type == TraceTypes.Sent && e.Data is byte[])
                {
                    str = string.Format("Meter TX: {0}", GXCommon.ToHex((byte[])e.Data));

                }
                else if (e.Type == TraceTypes.Received && e.Data is byte[])
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
                                Creator = _worker.RuntimeOptions.Id ,
                                Message = str
                            } ]
                    };
                    _worker.Client.PostAsJson("/api/Log/Add?source=agent", log, _cancellationToken).GetAwaiter().GetResult();
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
        /// <returns>A task completing after the connected meter's admitted work finishes.</returns>
        public async System.Threading.Tasks.Task ReadMeter()
        {
            try
            {
                _cancellationToken.ThrowIfCancellationRequested();
                ListenerSettings settings = RuntimeOptions.ListenerSettings
                    ?? throw new InvalidOperationException("Listener settings are missing.");
                if (settings.IdentifyWaitTime != 0)
                {
                    await GXConnectionTasks.WaitAsync(_connected, TimeSpan.FromSeconds(settings.IdentifyWaitTime), _cancellationToken);
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
                                Creator = _worker.RuntimeOptions.Id ,
                                Message = string.Format("Meter Meter connected from address '{0}'", _info)
                            } ]
                    };
                    await Client.PostAsJson("/api/Log/Add?source=agent", log, _cancellationToken);
                }
                if (_gatewayId == null && !settings.PreEstablished &&
                    (_deviceId == null || _deviceId == Guid.Empty && settings.IdentifyWaitTime == 0))
                {
                    GXDLMSObjectCollection objects = [];
                    GXDLMSData ldn = new("0.0.42.0.0.255");
                    ldn.SetUIDataType(2, DataType.String);
                    await _reader.InitializeConnection(false, false, settings.InvocationCounter);
                    await _reader.Read(ldn, 2);
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
                                Creator = _worker.RuntimeOptions.Id ,
                                Message = string.Format("Meter '{0}' connected", ldn.Value)
                            } ]
                        };
                        await _worker.Client.PostAsJson("/api/Log/Add?source=agent", log, _cancellationToken);
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
                    var ret = await Client.PostAsJson<ListDevicesResponse>("/api/Device/List", req, _cancellationToken);
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
                while (!_cancellationToken.IsCancellationRequested)
                {
                    if (!_worker.CanClaimTasks) {
                        break;
                    }
                    var response = await Client.PostAsJson<GetNextTaskResponse>("/api/Task/Next", new GetNextTask
                    {
                        AgentId = RuntimeOptions.Id, DeviceId = _deviceId, GatewayId = _gatewayId,
                        Listener = true, WaitTime = 0
                    }, _cancellationToken);
                    var tasks = response?.Tasks?.ToArray();
                    if (tasks == null || tasks.Length == 0) {
                        break;
                    }
                    await _worker.ReadMeter(new GXActionBlock
                    {
                        Tasks = tasks, CancellationToken = _cancellationToken, Media = _media, GatewayId = _gatewayId
                    }, settings);
                }
            }
            catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested) { }
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
                    await Client.PostAsJson("/api/Log/Add?source=agent", log, _cancellationToken);
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
                lock (_callbackGate)
                {
                    Volatile.Write(ref _closing, true);
                    _registration?.Dispose();
                }
                lock (_traceGate) { /* Wait for any active trace callback before releasing dependencies. */ }
                _connected.Dispose();
                _media.Close();

                _reader?.Close();
            }
        }

        /// <summary>
        /// Handle notification and push messages.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Media_OnReceived(object? sender, ReceiveEventArgs e)
        {
            lock (_callbackGate)
            {
                if (Volatile.Read(ref _closing) || _cancellationToken.IsCancellationRequested) {
                    return;
                }
                try { Media_OnReceivedCore(sender, e); }
                catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested) { }
                catch (Exception error) { _logger?.LogError(error, "Processing incoming connection data failed."); }
            }
        }

        /// <summary>Decodes received transport bytes and advances connection identification or device work.</summary>
        /// <param name="sender">Transport identity or event source that produced the notification.</param>
        /// <param name="e">Transport event data identifying the connection, received bytes or trace packet.</param>
        private void Media_OnReceivedCore(object? sender, ReceiveEventArgs e)
        {
            GXReplyData reply = new();
            GXReplyData notify = new();
            if (e.Data is not byte[] received)
            {
                return;
            }
            _notify.Set(received);
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
                        var ret = _worker.Scripts.RunAsync(tmp, _gatewayIdentificationScript, [e.SenderInfo, _media, _notify.Array()], _cancellationToken).GetAwaiter().GetResult();
                        if ((_gatewayId == null || _gatewayId == Guid.Empty))
                        {
                            if (ret is GXGateway gw && gw.Id != Guid.Empty)
                            {
                                //If script returns gateway.
                                _gatewayId = gw.Id;
                                _registration = _taskNotification.Register(gw.Id);
                                if (gw.Agent?.Id != RuntimeOptions.Id)
                                {
                                    //Update the gateway agent if it has changed.
                                    gw.Agent = new GXAgent() { Id = RuntimeOptions.Id };
                                    UpdateGateway tmp2 = new()
                                    {
                                        Gateways = [gw]
                                    };
                                    _worker.Client.PostAsJson("/api/Gateway/Update", tmp2, _cancellationToken).Wait();
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
                        _registration?.Dispose();
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
                    _worker.Client.PostAsJson("/api/Log/Add?source=agent", log, _cancellationToken).Wait();
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
                        _worker.Client.PostAsJson<AddLogResponse>("/api/Log/Add?source=script", se, _cancellationToken).Wait();
                    }
                }
                return;
            }

            GXDLMSSecureClient client =
                new(_settings.UseLogicalNameReferencing,
                0,
                0,
                (Authentication)_settings.Authentication,
                _settings.Password,
                (InterfaceType)_settings.Interface);
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
                        if (_settings.DefaultDeviceTemplate != null)
                        {
                            tmp.DefaultDeviceTemplate = new GXDeviceTemplate()
                            {
                                Id = _settings.DefaultDeviceTemplate.Value
                            };
                        }
                        // Run the prepared script version pinned for this connection.
                        var ret = _worker.Scripts.RunAsync(tmp, _autoConnectionIdentificationScript,
                            [e.SenderInfo, _media, args.Data], _cancellationToken).GetAwaiter().GetResult();
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
                    _worker.Client.PostAsJson("/api/Log/Add?source=agent", log, _cancellationToken).Wait();
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
                        _worker.Client.PostAsJson<AddLogResponse>("/api/Log/Add?source=script", se, _cancellationToken).Wait();
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
