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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Gurux.Net;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.Net.Enums;

namespace Gurux.DLMS.AMI.Agent.Worker.AutoConnect
{
    /// <summary>Accepts automatically connecting meters and owns their bounded connection sessions.</summary>
    /// <param name="logger">Records operation diagnostics without exposing authentication secrets.</param>
    /// <param name="serviceProvider">Resolves services used by accepted transport sessions.</param>
    /// <param name="taskNotification">Routes available task signals to the identified connection.</param>
    /// <param name="worker">Agent runtime being registered or reconfigured.</param>
    internal class GXAutoConnectService(ILogger<GXAutoConnectService> logger,
        IServiceProvider serviceProvider,
        IGXTaskNotification taskNotification, GXAgentWorker worker) : IHostedService, IDisposable
    {
        /// <summary>Resolves transport and repository services for this component.</summary>
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        /// <summary>Routes newly available task signals to registered device connections.</summary>
        private readonly IGXTaskNotification _taskNotification = taskNotification;
        /// <summary>Records runtime diagnostics without exposing authentication credentials.</summary>
        private readonly ILogger _logger = logger;
        /// <summary>Optional script used to identify an automatically connecting meter.</summary>
        private GXScriptMethod? _autoConnectionIdentificationScript;
        /// <summary>Optional script used to identify a connecting gateway.</summary>
        private GXScriptMethod? _gatewayIdentificationScript;

        //Listener wait incoming connections from the meters.
        /// <summary>Accepts inbound transport connections for the service.</summary>
        GXNet? _listener;
        /// <summary>Tracks active connections or script sessions until their work completes.</summary>
        private GXConnectionTasks? _sessions;
        /// <summary>Serializes service startup and shutdown operations.</summary>
        private readonly SemaphoreSlim _lifecycle = new(1, 1);
        /// <summary>Shared shutdown operation awaited by concurrent stop requests.</summary>
        private Task? _shutdown;
        /// <summary>Transport settings applied to the active connection.</summary>
        ListenerSettings? _settings;

        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _lifecycle.WaitAsync(cancellationToken);
            try
            {
                if (_shutdown != null)
                {
                    await _shutdown.WaitAsync(cancellationToken);
                    _shutdown = null;
                }
                if (_sessions != null) {
                    return;
                }
                _settings = worker.RuntimeOptions.ListenerSettings;
                _sessions = new GXConnectionTasks(worker.RuntimeOptions.MaxIncomingConnections, cancellationToken,
                    error => _logger.LogError(error, "Incoming connection failed."));
                if (_settings != null && _settings.Enabled)
                {
                    _listener = new GXNet
                    {
                        Settings = _settings.MediaSettings,
                        Server = true
                    };
                    if (_listener.Protocol == NetworkType.Tcp)
                    {
                        _listener.OnClientConnected += OnClientConnected;
                        _listener.OnClientDisconnected += OnClientDisconnected;
                    }
                    else
                    {
                        _listener.OnReceived += OnOnReceived;
                    }
                    if (_logger.IsEnabled(LogLevel.Information) == true)
                    {
                        _logger.LogInformation("{Message}", "Listening incoming connections in port:" + _listener.Port);
                    }
                    if (_settings?.ScriptMethod != null)
                    {
                        _autoConnectionIdentificationScript = await worker.Scripts.LoadAsync(
                            _settings.ScriptMethod.Value,
                            [typeof(string), typeof(GXNet), typeof(byte[])], cancellationToken);
                    }
                    else
                    {
                        _autoConnectionIdentificationScript = null;
                    }

                    // Existing auto-connect configurations still store their gateway script here.
                    // Retain this read until those saved configurations have been migrated.
    #pragma warning disable CS0618
                    Guid? legacyGatewayScriptMethod = _settings?.GatewayScriptMethod;
    #pragma warning restore CS0618
                    if (legacyGatewayScriptMethod is Guid gatewayScriptMethodId)
                    {
                        _gatewayIdentificationScript = await worker.Scripts.LoadAsync(
                            gatewayScriptMethodId,
                            [typeof(string), typeof(GXNet), typeof(byte[])], cancellationToken);
                    }
                    else
                    {
                        _gatewayIdentificationScript = null;
                    }

                    _listener.Open();
                }
                else
                {
                    _listener?.Close();
                }
                if (_listener == null || !_listener.IsOpen)
                {
                    _logger.LogInformation("Auto connect service is not used.");
                }
            }
            finally { _lifecycle.Release(); }
        }

        /// <summary>Starts a tracked session for a newly accepted transport connection.</summary>
        /// <param name="sender">Transport identity or event source that produced the notification.</param>
        /// <param name="e">Transport event data identifying the connection, received bytes or trace packet.</param>
        internal void OnClientConnected(object? sender, Gurux.Common.ConnectionEventArgs e)
        {
            try { string info = e.Info ?? throw new ArgumentException("Connection endpoint is missing.");
                GXNet attached = ((GXNet)(sender ?? throw new ArgumentNullException(nameof(sender)))).Attach(info);
                StartSession(attached, info); }
            catch (Exception error) { _logger.LogError(error, "Attaching incoming meter failed."); }
        }

        /// <summary>Cancels the session belonging to the disconnected transport connection.</summary>
        /// <param name="sender">Transport identity or event source that produced the notification.</param>
        /// <param name="e">Transport event data identifying the connection, received bytes or trace packet.</param>
        internal void OnClientDisconnected(object? sender, Gurux.Common.ConnectionEventArgs e)
        {
            if (e.Info != null)
            {
                _sessions?.Disconnect(e.Info);
            }
        }

        /// <summary>Starts or wakes the tracked session for received transport data.</summary>
        /// <param name="sender">Transport identity or event source that produced the notification.</param>
        /// <param name="e">Transport event data identifying the connection, received bytes or trace packet.</param>
        internal void OnOnReceived(object? sender, Gurux.Common.ReceiveEventArgs e)
        {
            try { string info = e.SenderInfo ?? throw new ArgumentException("Connection endpoint is missing.");
                GXNet attached = ((GXNet)(sender ?? throw new ArgumentNullException(nameof(sender)))).Attach(info);
                StartSession(attached, info); }
            catch (Exception error) { _logger.LogError(error, "Attaching incoming meter failed."); }
        }

        /// <summary>Admits an accepted transport connection to bounded session processing.</summary>
        /// <param name="media">Accepted transport used for connection protocol exchanges.</param>
        /// <param name="info">Process start settings or transport description used by the operation.</param>
        private void StartSession(GXNet media, string info)
        {
            var owner = _sessions;
            void OnStateChanged(object? sender, Gurux.Common.MediaStateEventArgs e)
            {
                if (e.State == Gurux.Common.MediaState.Closed) {
                    owner?.Disconnect(info);
                }
            }
            media.OnMediaStateChange += OnStateChanged;
            if (owner?.TryStart(info, async token =>
            {
                var reader = new GXAutoConnect(_serviceProvider, _logger, media, info,
                    _autoConnectionIdentificationScript, _gatewayIdentificationScript, null,
                    _taskNotification, worker, token);
                await reader.ReadMeter();
            }, () => { media.OnMediaStateChange -= OnStateChanged; media.Close(); }) != true)
            {
                media.OnMediaStateChange -= OnStateChanged;
                media.Close();
            }
        }

        /// <inheritdoc/>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Task pending;
            // Always request shutdown; the caller's token only cancels its wait below.
            await _lifecycle.WaitAsync(CancellationToken.None);
            try { pending = _shutdown ??= StopCoreAsync(); }
            finally { _lifecycle.Release(); }
            await pending.WaitAsync(cancellationToken);
        }

        /// <summary>Stops the listener and waits for all owned connection sessions to finish.</summary>
        /// <returns>A task representing this operation: Stops the listener and waits for all owned connection sessions to finish.</returns>
        private async Task StopCoreAsync()
        {
            await Task.Yield();
            var pending = _sessions?.StopAsync() ?? Task.CompletedTask;
            try { _listener?.Close(); }
            finally { await pending; }
            _listener?.Dispose();
            _listener = null;
            _sessions = null;
        }
        /// <inheritdoc/>
        public void Dispose()
        {
            StopAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
