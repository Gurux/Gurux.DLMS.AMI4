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

using Gurux.DLMS.AMI.Agent.Worker.Gateway;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Gurux.Net.Enums;

namespace Gurux.DLMS.AMI.Agent.Worker.AutoConnect
{
    /// <summary>Accepts gateway connections and reports identified gateway connectivity to the server.</summary>
    /// <param name="logger">Records operation diagnostics without exposing authentication secrets.</param>
    /// <param name="serviceProvider">Resolves services used by accepted transport sessions.</param>
    /// <param name="taskNotification">Routes available task signals to the identified connection.</param>
    /// <param name="worker">Agent runtime being registered or reconfigured.</param>
    internal class GXGatewayService(ILogger<GXGatewayService> logger,
        IServiceProvider serviceProvider,
        IGXTaskNotification taskNotification, GXAgentWorker worker) : IHostedService, IDisposable
    {
        /// <summary>Resolves transport and repository services for this component.</summary>
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        /// <summary>Routes newly available task signals to registered device connections.</summary>
        private readonly IGXTaskNotification _taskNotification = taskNotification;
        /// <summary>Records runtime diagnostics without exposing authentication credentials.</summary>
        private readonly ILogger _logger = logger;
        /// <summary>Optional script used to identify a connecting gateway.</summary>
        private GXScriptMethod? _gatewayIdentificationScript;

        //Gateway wait incoming connections from the meters.
        /// <summary>Accepts inbound transport connections for the service.</summary>
        GXNet? _listener;
        /// <summary>Tracks active connections or script sessions until their work completes.</summary>
        private GXConnectionTasks? _sessions;
        /// <summary>Serializes service startup and shutdown operations.</summary>
        private readonly SemaphoreSlim _lifecycle = new(1, 1);
        /// <summary>Protects publication of current status and measurements.</summary>
        private readonly SemaphoreSlim _statusGate = new(1, 1);
        /// <summary>Connections grouped by the gateway identifier they established.</summary>
        private readonly Dictionary<Guid, HashSet<object>> _identifiedConnections = [];
        /// <summary>Shared shutdown operation awaited by concurrent stop requests.</summary>
        private Task? _shutdown;
        /// <summary>Transport settings applied to the active connection.</summary>
        GatewaySettings? _settings;

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
                if (_sessions != null)
                {
                    return;
                }
                _settings = worker.RuntimeOptions.GatewaySettings;
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
                        _logger.LogInformation("{Message}", "Gateway in port:" + _listener.Port);
                    }
                    if (_settings.ScriptMethod is Guid scriptMethodId)
                    {
                        //Always use the assembly compiled by the server, including after updates.
                        _gatewayIdentificationScript = await worker.Scripts.LoadAsync(
                            scriptMethodId,
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
                    _logger.LogInformation("Gateway service is not used.");
                }
            }
            finally { _lifecycle.Release(); }
        }

        // Diagnostics are best effort and cannot prevent the reader from starting or cleaning up.
        /// <summary>Publishes a gateway transport status transition to the server.</summary>
        /// <param name="status">HTTP or runtime status being recorded.</param>
        /// <param name="info">Process start settings or transport description used by the operation.</param>
        /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
        /// <param name="gatewayId">Optional gateway identifier associated with the status transition.</param>
        /// <returns>A task representing this operation: Publishes a gateway transport status transition to the server.</returns>
        internal async Task ReportGatewayEventAsync(GatewayStatus status, string info, CancellationToken token, Guid? gatewayId = null)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(5));
            try
            {
                if ((gatewayId ?? _settings?.Id) is Guid id && id != Guid.Empty)
                {
                    await worker.Client.PostAsJson("/api/Gateway/UpdateStatus", new UpdateGatewayStatus { Id = id, Status = status }, deadline.Token);
                }
            }
            catch (OperationCanceledException) when (deadline.IsCancellationRequested) { }
            catch (Exception error) { _logger.LogWarning(error, "Updating gateway status failed."); }
            try
            {
                if (worker.RuntimeOptions.TraceLevel > TraceLevel.Warning)
                {
                    await worker.Client.PostAsJson("/api/Log/Add?source=agent", new AddLog
                    {
                        Type = "Gateway",
                        Logs = [new GXLog(TraceLevel.Verbose) { Creator = worker.RuntimeOptions.Id, Message = $"Client {info}: {status}." }]
                    }, deadline.Token);
                }
            }
            catch (OperationCanceledException) when (deadline.IsCancellationRequested) { }
            catch (Exception error) { _logger.LogWarning(error, "Writing gateway event failed."); }
        }

        /// <summary>Updates the identified gateway's connectivity while tracking its active connections.</summary>
        /// <param name="id">Identity of the agent, device or registered work being processed.</param>
        /// <param name="connection">Connection whose work or persisted records are processed.</param>
        /// <param name="connected">Whether the identified transport connection is currently active.</param>
        /// <param name="info">Process start settings or transport description used by the operation.</param>
        /// <param name="token">Cancels the operation when its owning lifetime ends.</param>
        /// <returns>A task representing this operation: Updates the identified gateway's connectivity while tracking its active connections.</returns>
        private async Task ReportIdentifiedGatewayAsync(Guid id, object connection, bool connected, string info, CancellationToken token)
        {
            // Serialize reporting with membership changes so predecessor cleanup cannot overwrite a newer connection.
            // Membership cleanup must complete even after the connection is canceled.
            await _statusGate.WaitAsync(CancellationToken.None);
            try
            {
                if (connected)
                {
                    if (!_identifiedConnections.TryGetValue(id, out var active))
                    {
                        active = [];
                        _identifiedConnections.Add(id, active);
                    }
                    if (active.Add(connection))
                    {
                        await ReportGatewayEventAsync(GatewayStatus.Connected, info, token, id);
                    }
                }
                else if (_identifiedConnections.TryGetValue(id, out var active) && active.Remove(connection) && active.Count == 0)
                {
                    _identifiedConnections.Remove(id);
                    await ReportGatewayEventAsync(GatewayStatus.Offline, info, CancellationToken.None, id);
                }
            }
            finally
            {
                _statusGate.Release();
            }
        }

        /// <summary>Starts a tracked session for a newly accepted transport connection.</summary>
        /// <param name="sender">Transport identity or event source that produced the notification.</param>
        /// <param name="e">Transport event data identifying the connection, received bytes or trace packet.</param>
        internal void OnClientConnected(object? sender, Gurux.Common.ConnectionEventArgs e)
        {
            try
            { string info = e.Info ?? throw new ArgumentException("Connection endpoint is missing.");
                GXNet attached = ((GXNet)(sender ?? throw new ArgumentNullException(nameof(sender)))).Attach(info);
                StartSession(attached, info); }
            catch (Exception error) { _logger.LogError(error, "Attaching incoming gateway failed."); }
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
            try
            { string info = e.SenderInfo ?? throw new ArgumentException("Connection endpoint is missing.");
                GXNet attached = ((GXNet)(sender ?? throw new ArgumentNullException(nameof(sender)))).Attach(info);
                StartSession(attached, info); }
            catch (Exception error) { _logger.LogError(error, "Attaching incoming gateway failed."); }
        }

        /// <summary>Admits an accepted transport connection to bounded session processing.</summary>
        /// <param name="media">Accepted transport used for connection protocol exchanges.</param>
        /// <param name="info">Process start settings or transport description used by the operation.</param>
        private void StartSession(GXNet media, string info)
        {
            var owner = _sessions;
            void OnStateChanged(object? sender, Gurux.Common.MediaStateEventArgs e)
            {
                if (e.State == Gurux.Common.MediaState.Closed)
                {
                    owner?.Disconnect(info);
                }
            }
            media.OnMediaStateChange += OnStateChanged;
            if (owner?.TryStart(info, async token =>
            {
                var configuredId = _settings?.Id;
                Guid? identifiedId = configuredId;
                var statusOwner = new object();
                var diagnostics = ReportGatewayEventAsync(GatewayStatus.Connected, info, token, configuredId);
                try
                {
                    var reader = new GXGatewayReader(_serviceProvider, _logger, media, info,
                        _gatewayIdentificationScript, null, _taskNotification, worker, token, async id =>
                        {
                            identifiedId = id;
                            if (configuredId == null)
                            {
                                await ReportIdentifiedGatewayAsync(id, statusOwner, true, info, token);
                            }
                        });
                    await reader.ReadMeter();
                }
                finally
                {
                    await diagnostics;
                    if (configuredId == null && identifiedId is Guid id)
                    {
                        await ReportIdentifiedGatewayAsync(id, statusOwner, false, info, CancellationToken.None);
                    }
                    else
                    {
                        await ReportGatewayEventAsync(GatewayStatus.Offline, info, CancellationToken.None, identifiedId);
                    }
                }
            }, () => { media.OnMediaStateChange -= OnStateChanged; media.Close(); }, _settings?.Id?.ToString()) != true)
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
            try
            { pending = _shutdown ??= StopCoreAsync(); }
            finally { _lifecycle.Release(); }
            await pending.WaitAsync(cancellationToken);
        }

        /// <summary>Stops the listener and waits for all owned connection sessions to finish.</summary>
        /// <returns>A task representing this operation: Stops the listener and waits for all owned connection sessions to finish.</returns>
        private async Task StopCoreAsync()
        {
            await Task.Yield();
            var pending = _sessions?.StopAsync() ?? Task.CompletedTask;
            try
            { _listener?.Close(); }
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
