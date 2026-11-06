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
using Microsoft.Extensions.Logging;
using Gurux.Net;
using Gurux.Common;
using Microsoft.Extensions.Hosting;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Script;
using System.Diagnostics;



using System.Runtime.Caching;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using DeviceSettings = Gurux.DLMS.AMI.Shared.DTOs.Device.GXDLMSSettings;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Agent.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Gurux.DLMS.AMI.Agent.Worker.Notifier
{
    /// <inheritdoc/>
    /// <summary>
    /// Constructor.
    /// </summary>
    internal class GXNotifyService(ILogger<GXNotifyService> logger,
        IServiceProvider serviceProvider, GXAgentWorker worker) : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly ILogger<GXNotifyService> _logger = logger;
        NotifySettings? settings;
        private readonly SemaphoreSlim lifecycle = new(1, 1);
        //Notify wait push, events or notifies from the meters.
        GXNet? notify;
        private GXRawNotificationSender? rawSender;
        private int disposeStarted;
        private CancellationTokenSource scriptLifetime = new();

        /// <summary>
        /// Each client has own message queue.
        /// </summary>
        private readonly Dictionary<string, GXNotifyClient> notifyMessages = [];

        GXScriptMethod? scriptMethod;
        internal Guid? ScriptId => scriptMethod?.Script?.Id;

        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref disposeStarted) != 0, this);
            await lifecycle.WaitAsync(cancellationToken);
            try
            {
                if (notify != null)
                {
                    return;
                }

                if (scriptLifetime.IsCancellationRequested)
                {
                    scriptLifetime.Dispose();
                    scriptLifetime = new CancellationTokenSource();
                }

                if (worker.RuntimeOptions.NotifySettings != null)
                {
                    settings = worker.RuntimeOptions.NotifySettings;
                    GXNet net = new()
                    {
                        Settings = settings.MediaSettings
                    };
                    if (settings != null && settings.Enabled && net.Port != 0)
                    {
                        if (!settings.PassThroughData && settings.ScriptMethod != null)
                        {
                            var timeType = typeof(GXReplyData).GetProperty(nameof(GXReplyData.Time))!.PropertyType;
                            scriptMethod = await worker.Scripts.LoadAsync(settings.ScriptMethod.Value, [typeof(string), typeof(GXDevice), Nullable.GetUnderlyingType(timeType) ?? timeType, null ], cancellationToken);
                        }
                        else
                        {
                            scriptMethod = null;
                        }

                        notify = new GXNet((NetworkType)net.Protocol, net.Port);
                        if (settings.PassThroughData)
                        {
                            rawSender = new GXRawNotificationSender(worker.client, worker.RuntimeOptions.Id, settings.Interface, _logger, options: _serviceProvider.GetService<IOptions<GXRawDeliveryOptions>>()?.Value);
                        }

                        notify.OnReceived += OnNotifyReceived;
                        notify.OnClientConnected += OnRawClientConnected;
                        notify.OnClientDisconnected += OnRawClientDisconnected;
                        if (worker.RuntimeOptions.TraceLevel > TraceLevel.Error)
                        {
                            if (_logger.IsEnabled(LogLevel.Information) == true)
                            {
                                _logger.LogInformation("Listening push notifications in port: {Value0}.", notify.Port);
                            }

                            if (worker.RuntimeOptions.NotifySettings.TraceLevel > TraceLevel.Error)
                            {
                                AddLog log = new()
                                {
                                    Type = "Push listener",
                                    Logs = [new GXLog(TraceLevel.Verbose)
                                    {
                                        Creator = worker.RuntimeOptions.Id,
                                        Message = string.Format("Listening push notifications in port: {0}.", notify.Port)
                                    }

                                    ]
                                };
                                await worker.client.PostAsJson("/api/Log/Add?source=agent", log, cancellationToken);
                            }
                        }

                        notify.Open();
                    }
                }

                if (settings == null || !settings.Enabled || notify == null)
                {
                    _logger.LogInformation("Notify service is not used.");
                }
            }
            finally
            {
                lifecycle.Release();
            }
        }

        // Cache the most recently resolved device for the notification parser, not as a source of keys.
        private readonly MemoryCache _cachedDevices = new("Devices");

        private void OnGetKeys(object sender, GXCryptoKeyParameter args)
        {
            if ((args.KeyType & Enums.CryptoKeyType.Authentication) != 0) {
                args.AuthenticationKey = null;
            }
            if ((args.KeyType & Enums.CryptoKeyType.BlockCipher) != 0) {
                args.BlockCipherKey = null;
            }
            string st = GXCommon.ToHex(args.SystemTitle, false);
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var response = worker.client.GetAsJsonAsync<GetDeviceResponse>(
                $"/api/Device/BySystemTitle?systemTitle={Uri.EscapeDataString(st)}", deadline.Token).GetAwaiter().GetResult();
            var device = response?.Item ?? throw new InvalidOperationException(string.Format(Properties.Resources.UnknownDeviceForSystemTitle, st));
            var keys = System.Text.Json.JsonSerializer.Deserialize<DeviceSettings>(device.Settings ?? "null")
                ?? throw new InvalidOperationException("Device DLMS settings are missing.");
            if (keys.DeviceSystemTitle is { Length: > 0 } && !keys.DeviceSystemTitle.SequenceEqual(args.SystemTitle))
                {
                throw new InvalidOperationException("Device system title does not match the push sender.");
            }
            if ((args.KeyType & Enums.CryptoKeyType.Authentication) != 0)
                {
                args.AuthenticationKey = Required(keys.AuthenticationKey, st);
            }
            if ((args.KeyType & Enums.CryptoKeyType.BlockCipher) != 0)
                {
                args.BlockCipherKey = Required(keys.BroadcastKey is { Length: > 0 } ? keys.BroadcastKey : keys.BlockCipherKey, st);
            }
            _cachedDevices.Set(st, device, DateTimeOffset.UtcNow.AddMinutes(1));
        }

        private static byte[] Required(byte[]? key, string systemTitle)
            => key is { Length: > 0 } ? key.ToArray()
                : throw new InvalidOperationException(string.Format(Properties.Resources.UnknownKeyForSystemTitle, systemTitle));
        /// <summary>
        /// Handle received notify message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void OnNotifyReceived(object sender, ReceiveEventArgs e)
        {
            NotifySettings? currentSettings = settings;
            if (currentSettings == null)
            {
                return;
            }
            // Forward before constructing a DLMS client, loading keys or executing a parser script.
            if (currentSettings.PassThroughData)
            {
                try
                {
                    rawSender?.Receive(e.SenderInfo, (byte[])e.Data, notify?.Protocol == NetworkType.Udp);
                }
                catch (OperationCanceledException) when (rawSender?.IsIntakeStopped == true)
                {
                    _logger.LogDebug("Raw receiver stopped while waiting for local storage capacity.");
                }
                return;
            }
            try
            {
                GXNotifyClient reply;
                lock (notifyMessages)
                {
                    if (notifyMessages.TryGetValue(e.SenderInfo, out GXNotifyClient? value))
                    {
                        reply = value;
                    }
                    else
                    {
                        reply = new GXNotifyClient(currentSettings.UseLogicalNameReferencing,
                            currentSettings.Interface, null, worker.Scripts.Current(scriptMethod));
                        notifyMessages.Add(e.SenderInfo, reply);
                    }
                }
                var selectedScript = reply.ScriptMethod;
                DateTime now = DateTime.Now;
                //If received data is expired.
                if (currentSettings.ExpirationTime != 0 && (now - reply.DataReceived).TotalSeconds > currentSettings.ExpirationTime)
                {
                    reply.Reply.Clear();
                }
                reply.DataReceived = now;
                reply.Reply.Set((byte[])e.Data);
                //Send agent verbose trace.
                if (currentSettings.TraceLevel == TraceLevel.Verbose)
                {
                    //Agent log from the received data.
                    GXLog log = new(TraceLevel.Verbose)
                    {
                        Creator = worker.RuntimeOptions.Id,
                        Message = GXCommon.ToHex((byte[])e.Data)
                    };
                    AddLog it = new()
                    {
                        Type = "Notify data",
                        Logs = [log]
                    };
                    worker.client.PostAsJson<AddDeviceTraceResponse>("/api/Log/Add?source=agent", it).Wait();
                }

                try
                {
                    reply.Client.OnKeys += OnGetKeys;
                    //There might be multiple frames waiting. Read them all.
                    while (reply.Client.GetData(reply.Reply, reply.Notify) && reply.Reply.Available != 0)
                    {
                        ;
                    }
                }
                finally
                {
                    reply.Client.OnKeys -= OnGetKeys;
                }
                // If all data is received.
                if (reply.Notify.IsComplete && !reply.Notify.IsMoreData)
                {
                    if (worker.RuntimeOptions.TraceLevel == TraceLevel.Verbose)
                    {
                        //Show data as XML in the console.
                        GXDLMSTranslator t = new();
                        t.DataToXml(reply.Notify.Data, out string xml);
                        if (_logger.IsEnabled(LogLevel.Debug) == true)
                        {
                            _logger.LogDebug("{Message}", xml);
                        }
                    }
                    //Send agent trace.
                    if (currentSettings.TraceLevel >= TraceLevel.Info)
                    {
                        GXDLMSTranslator t = new();
                        t.DataToXml(reply.Notify.Data, out string xml);
                        //Agent trace.
                        GXLog trace = new(TraceLevel.Info)
                        {
                            Creator = worker.RuntimeOptions.Id,
                            Message = xml
                        };
                        AddLog it = new()
                        {
                            Logs = [trace]
                        };
                        worker.client.PostAsJson<AddDeviceTraceResponse>("/api/Log/Add?source=agent", it).Wait();
                    }
                    try
                    {
                        if (selectedScript != null && selectedScript.Script != null)
                        {
                            //Get the device from the cache.
                            string st = GXCommon.ToHex(reply.Notify.SystemTitle, false);
                            GXDevice? device = (GXDevice)_cachedDevices.Get(st);

                            GXAmiScript tmp = new(_serviceProvider)
                            {
                                Sender = new GXAgent() { Id = worker.RuntimeOptions.Id }
                            };
                            //Run script.
                            worker.Scripts.RunAsync(tmp, selectedScript,
                                [e.SenderInfo, device, reply.Notify.Time, reply.Notify.Value], scriptLifetime.Token).GetAwaiter().GetResult();
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_logger.IsEnabled(LogLevel.Error) == true)
                        {
                            _logger.LogError("{Message}", ex.Message + Environment.NewLine + ex.StackTrace);
                        }
                        AddLog log = new()
                        {
                            Logs = [new()
                            {
                                Creator = worker.RuntimeOptions.Id ,
                                Message = ex.Message,
                                StackTrace =ex.StackTrace
                            } ]
                        };
                        worker.client.PostAsJson("/api/Log/Add?source=agent", log).Wait();
                        if (selectedScript != null && selectedScript.Script != null)
                        {
                            AddLog se = new()
                            {
                                Logs = [new()
                            {
                                Creator = selectedScript.Script.Id,
                                Message = ex.Message
                            } ]
                            };
                            worker.client.PostAsJson<AddLogResponse>("/api/Log/Add?source=script", se).Wait();
                        }
                    }
                    finally
                    {
                        reply.Notify.Clear();
                        //Don't call clear here.
                        //There might be bytes from the next frame waiting.
                        reply.Reply.Trim();
                        if (notify?.Protocol == NetworkType.Udp)
                        {
                            reply.ScriptMethod = worker.Scripts.Current(scriptMethod);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Error) == true)
                {
                    _logger.LogError("{Message}", ex.Message + Environment.NewLine + ex.StackTrace);
                }
                AddLog log = new()
                {
                    Logs = [new()
                            {
                                Creator = worker.RuntimeOptions.Id ,
                                Message = ex.Message,
                                StackTrace =ex.StackTrace
                            } ]
                };
                try
                {
                    worker.client.PostAsJson("/api/Log/Add?source=agent", log).Wait();
                }
                catch (Exception ex2)
                {
                    //If connection is lost for the server.
                    if (_logger.IsEnabled(LogLevel.Error) == true)
                    {
                        _logger.LogError("{Message}", ex2.Message + Environment.NewLine + ex2.StackTrace);
                    }
                }
            }
        }

        private void OnRawClientConnected(object sender, ConnectionEventArgs e)
        {
            if (rawSender != null) { rawSender.Close(e.Info); return; }
            if (settings == null)
            {
                return;
            }

            lock (notifyMessages)
            {
                notifyMessages[e.Info] = new GXNotifyClient(settings.UseLogicalNameReferencing, settings.Interface, null,
                    worker.Scripts.Current(scriptMethod));
            }
        }

        private void OnRawClientDisconnected(object sender, ConnectionEventArgs e)
        {
            if (rawSender != null)
            {
                rawSender.Close(e.Info);
            }
            else
            {
                lock (notifyMessages)
                {
                    notifyMessages.Remove(e.Info);
                }
            }
        }
        /// <inheritdoc/>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await lifecycle.WaitAsync(cancellationToken);
            try
            {
                rawSender?.StopIntake();
                await scriptLifetime.CancelAsync();
                if (notify != null)
                {
                    notify.OnReceived -= OnNotifyReceived;
                    notify.OnClientConnected -= OnRawClientConnected;
                    notify.OnClientDisconnected -= OnRawClientDisconnected;
                    notify.Close();
                    notify.Dispose();
                    notify = null;
                }

                if (rawSender != null)
                {
                    try
                    {
                        await rawSender.CompleteAsync(cancellationToken);
                    }
                    finally
                    {
                        await rawSender.DisposeAsync();
                        rawSender = null;
                    }
                }

                if (worker.RuntimeOptions.TraceLevel > TraceLevel.Error)
                {
                    _logger.LogWarning("Notify service is stopping.");
                    if (worker.RuntimeOptions.NotifySettings?.TraceLevel > TraceLevel.Error)
                    {
                        AddLog log = new()
                        {
                            Type = "Push listener",
                            Logs = [new GXLog(TraceLevel.Verbose)
                            {
                                Creator = worker.RuntimeOptions.Id,
                                Message = "Stop Listening push notifications."
                            }

                            ]
                        };
                        await worker.client.PostAsJson("/api/Log/Add?source=agent", log, cancellationToken);
                    }
                }
            }
            finally
            {
                lifecycle.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposeStarted, 1) != 0)
            {
                return;
            }
            rawSender?.StopIntake();
            scriptLifetime.Cancel();
            scriptLifetime.Dispose();
            if (notify != null)
            {
                notify.OnReceived -= OnNotifyReceived;
                notify.OnClientConnected -= OnRawClientConnected;
                notify.OnClientDisconnected -= OnRawClientDisconnected;
                notify.Dispose();
                notify = null;
            }
            rawSender?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            rawSender = null;
            _cachedDevices.Dispose();
        }
    }
}
