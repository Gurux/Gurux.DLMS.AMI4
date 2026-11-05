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

namespace Gurux.DLMS.AMI.Agent.Worker.AutoConnect
{
    internal class GXAutoConnectService(ILogger<GXAutoConnectService> logger,
        IServiceProvider serviceProvider,
        IGXTaskNotification taskNotification, GXAgentWorker worker) : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IGXTaskNotification _taskNotification = taskNotification;
        private readonly ILogger _logger = logger;
        private GXScriptMethod? _autoConnectionIdentificationScript;
        private GXScriptMethod? _gatewayIdentificationScript;

        //Listener wait incoming connections from the meters.
        GXNet? listener;
        private GXConnectionTasks? sessions;
        private readonly SemaphoreSlim lifecycle = new(1, 1);
        private Task? shutdown;
        ListenerSettings? _settings;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await lifecycle.WaitAsync(cancellationToken);
            try
            {
                if (shutdown != null)
                {
                    await shutdown.WaitAsync(cancellationToken);
                    shutdown = null;
                }
                if (sessions != null) {
                    return;
                }
                _settings = worker.RuntimeOptions.ListenerSettings;
                sessions = new GXConnectionTasks(worker.RuntimeOptions.MaxIncomingConnections, cancellationToken,
                    error => _logger.LogError(error, "Incoming connection failed."));
                if (_settings != null && _settings.Enabled)
                {
                    listener = new GXNet
                    {
                        Settings = _settings.MediaSettings,
                        Server = true
                    };
                    if (listener.Protocol == NetworkType.Tcp)
                    {
                        listener.OnClientConnected += OnClientConnected;
                        listener.OnClientDisconnected += OnClientDisconnected;
                    }
                    else
                    {
                        listener.OnReceived += OnOnReceived;
                    }
                    if (_logger.IsEnabled(LogLevel.Information) == true)
                    {
                        _logger.LogInformation("{Message}", "Listening incoming connections in port:" + listener.Port);
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

                    listener.Open();
                }
                else
                {
                    listener?.Close();
                }
                if (listener == null || !listener.IsOpen)
                {
                    _logger.LogInformation("Auto connect service is not used.");
                }
            }
            finally { lifecycle.Release(); }
        }

        internal void OnClientConnected(object sender, Gurux.Common.ConnectionEventArgs e)
        {
            try { StartSession(((GXNet)sender).Attach(e), e.Info); }
            catch (Exception error) { _logger.LogError(error, "Attaching incoming meter failed."); }
        }

        internal void OnClientDisconnected(object sender, Gurux.Common.ConnectionEventArgs e)
            => sessions?.Disconnect(e.Info);

        internal void OnOnReceived(object sender, Gurux.Common.ReceiveEventArgs e)
        {
            try { StartSession(((GXNet)sender).Attach(e), e.SenderInfo); }
            catch (Exception error) { _logger.LogError(error, "Attaching incoming meter failed."); }
        }

        private void StartSession(GXNet media, string info)
        {
            var owner = sessions;
            void OnStateChanged(object sender, Gurux.Common.MediaStateEventArgs e)
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

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Task pending;
            await lifecycle.WaitAsync();
            try { pending = shutdown ??= StopCoreAsync(); }
            finally { lifecycle.Release(); }
            await pending.WaitAsync(cancellationToken);
        }

        private async Task StopCoreAsync()
        {
            await Task.Yield();
            var pending = sessions?.StopAsync() ?? Task.CompletedTask;
            try { listener?.Close(); }
            finally { await pending; }
            listener?.Dispose();
            listener = null;
            sessions = null;
        }
        public void Dispose()
        {
            StopAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
