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
using Microsoft.Extensions.Logging;
using Gurux.Net;
using Gurux.Common;
using Microsoft.Extensions.Hosting;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Script;
using System.Runtime.Loader;
using System.Diagnostics;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using System.Text;
using System.Runtime.Caching;
using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Agent.Worker.Notifier
{
    /// <inheritdoc/>
    internal class GXNotifyService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GXNotifyService> _logger;
        NotifySettings? settings;
        //Notify wait push, events or notifies from the meters.
        GXNet? notify;

        /// <summary>
        /// Each client has own message queue.
        /// </summary>
        static Dictionary<string, GXNotifyClient> notifyMessages = new Dictionary<string, GXNotifyClient>();

        GXScriptMethod? scriptMethod;
        AssemblyLoadContext? loadedScript;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXNotifyService(ILogger<GXNotifyService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (GXAgentWorker.Options.NotifySettings != null)
            {
                settings = GXAgentWorker.Options.NotifySettings;
                GXNet net = new GXNet();
                net.Settings = settings.MediaSettings;
                if (settings != null && settings.Active && net.Port != 0)
                {
                    if (settings.ScriptMethod != null)
                    {
                        //Get script when app starts.
                        ListScripts req = new ListScripts();
                        req.Filter = new GXScript();
                        req.Filter.Methods = new List<GXScriptMethod>(new GXScriptMethod[]
                        {
                        new GXScriptMethod() { Id = settings.ScriptMethod.Value } });
                        ListScriptsResponse? ret = GXAgentWorker.client.PostAsJson<ListScriptsResponse>("/api/Script/List", req).Result;
                        if (ret?.Scripts == null || ret.Scripts.Length != 1)
                        {
                            throw new Exception("Unknown script to execute.");
                        }
                        scriptMethod = ret.Scripts[0].Methods.FirstOrDefault();
                        //Update parent script.
                        scriptMethod.Script = ret.Scripts[0];
                    }
                    else
                    {
                        scriptMethod = null;
                    }
                    notify = new GXNet((NetworkType)net.Protocol, net.Port);
                    notify.OnReceived += OnNotifyReceived;
                    _logger.LogInformation("Listening notifications in port: " + notify.Port);
                    notify.Open();
                }
            }
            if (settings == null || !settings.Active || notify == null)
            {
                _logger.LogInformation("Notify service is not used.");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Get block cipher or authentication key.
        /// </summary>
        private byte[]? GetKey(
            IEnumerable<GXKeyManagement>? managers,
            KeyManagementType type)
        {
            if (managers != null)
            {
                foreach (var manager in managers)
                {
                    if (manager.Keys != null)
                    {
                        foreach (var it in manager.Keys)
                        {
                            if (it.KeyType == type)
                            {
                                if (string.IsNullOrEmpty(it.Data))
                                {
                                    throw new Exception("Invalid key.");
                                }
                                if (it.IsHex.GetValueOrDefault(false))
                                {
                                    return GXDLMSTranslator.HexToBytes(it.Data);
                                }
                                return ASCIIEncoding.ASCII.GetBytes(it.Data);
                            }
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Read devices are saved to cache.
        /// </summary>
        private MemoryCache _cachedDevices = new MemoryCache("Devices");

        /// <summary>
        /// Get certificate keys from the server.
        /// </summary>
        private void OnGetKeys(object sender, GXCryptoKeyParameter args)
        {
            string st = GXCommon.ToHex(args.SystemTitle, false);
            //Check is device already read.            
            GXDevice? device = (GXDevice)_cachedDevices.Get(st);
            if (device != null)
            {
                if ((args.KeyType & Enums.CryptoKeyType.Authentication) != 0)
                {
                    args.AuthenticationKey = GetKey(device.Keys, KeyManagementType.Authentication);
                    if (args.AuthenticationKey == null)
                    {
                        string msg = string.Format(Worker.Properties.Resources.UnknownKeyForSystemTitle, st);
                        throw new Exception(msg);
                    }
                }
                if ((args.KeyType & Enums.CryptoKeyType.BlockCipher) != 0)
                {
                    args.BlockCipherKey = GetKey(device.Keys, KeyManagementType.Broadcast);
                    if (args.BlockCipherKey == null)
                    {
                        args.BlockCipherKey = GetKey(device.Keys, KeyManagementType.BlockCipher);
                    }
                    if (args.BlockCipherKey == null)
                    {
                        string msg = string.Format(Properties.Resources.UnknownKeyForSystemTitle, st);
                        throw new Exception(msg);
                    }
                }
                return;
            }
            ListKeyManagements req = new ListKeyManagements()
            {
                Filter = new GXKeyManagement()
                {
                    SystemTitle = st
                },
                Select = (TargetType.KeyManagementKey | TargetType.Device | TargetType.Object | TargetType.ObjectTemplate | TargetType.Attribute | TargetType.AttributeTemplate)
            };
            var ret = GXAgentWorker.client.PostAsJson<ListKeyManagementsResponse>("/api/KeyManagement/List", req).Result;
            if (ret != null)
            {
                if ((args.KeyType & Enums.CryptoKeyType.Authentication) != 0)
                {
                    args.AuthenticationKey = GetKey(ret.KeyManagements, KeyManagementType.Authentication);
                    if (args.AuthenticationKey == null)
                    {
                        string msg = string.Format(Worker.Properties.Resources.UnknownKeyForSystemTitle, st);
                        throw new Exception(msg);
                    }
                }
                if ((args.KeyType & Enums.CryptoKeyType.BlockCipher) != 0)
                {
                    args.BlockCipherKey = GetKey(ret.KeyManagements, KeyManagementType.Broadcast);
                    if (args.BlockCipherKey == null)
                    {
                        args.BlockCipherKey = GetKey(ret.KeyManagements, KeyManagementType.BlockCipher);
                    }
                    if (args.BlockCipherKey == null)
                    {
                        string msg = string.Format(Properties.Resources.UnknownKeyForSystemTitle, st);
                        throw new Exception(msg);
                    }
                }
                //Read device and add it to the cache.
                if (ret.KeyManagements?.FirstOrDefault() is GXKeyManagement f)
                {
                    if (f?.Device == null)
                    {
                        string msg = string.Format(Properties.Resources.UnknownDeviceForSystemTitle, st);
                        throw new Exception(msg);
                    }
                    f.Device.Keys = new List<GXKeyManagement>(ret.KeyManagements);
                    var cacheItem = new CacheItem(st, f.Device);
                    var cacheItemPolicy = new CacheItemPolicy
                    {
                        AbsoluteExpiration = DateTimeOffset.Now.AddHours(1)
                    };
                    _cachedDevices.Add(cacheItem, cacheItemPolicy);
                }
            }
        }

        /// <summary>
        /// Handle received notify message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void OnNotifyReceived(object sender, ReceiveEventArgs e)
        {
            try
            {
                GXNotifyClient reply;
                lock (notifyMessages)
                {
                    if (notifyMessages.ContainsKey(e.SenderInfo))
                    {
                        reply = notifyMessages[e.SenderInfo];
                    }
                    else
                    {
                        reply = new GXNotifyClient(settings.UseLogicalNameReferencing,
                            settings.Interface, null);
                        notifyMessages.Add(e.SenderInfo, reply);
                    }
                }
                DateTime now = DateTime.Now;
                //If received data is expired.
                if (settings.ExpirationTime != 0 && (now - reply.DataReceived).TotalSeconds > settings.ExpirationTime)
                {
                    reply.Reply.Clear();
                }
                reply.DataReceived = now;
                reply.Reply.Set((byte[])e.Data);
                //Send agent verbose trace.
                if (settings != null && settings.TraceLevel == TraceLevel.Verbose)
                {
                    //Agent trace.
                    GXAgentLog trace = new GXAgentLog(TraceLevel.Verbose);
                    trace.Agent = new GXAgent() { Id = GXAgentWorker.Options.Id };
                    trace.Message = GXCommon.ToHex((byte[])e.Data);
                    AddAgentLog it = new AddAgentLog();
                    it.Logs = new GXAgentLog[] { trace };
                    GXAgentWorker.client.PostAsJson<AddDeviceTraceResponse>("/api/AgentLog/Add", it).Wait();
                }

                try
                {
                    reply.Client.OnKeys += OnGetKeys;
                    //There might be multiple frames waiting. Read them all.
                    while (reply.Client.GetData(reply.Reply, reply.Notify) && reply.Reply.Available != 0) ;
                }
                finally
                {
                    reply.Client.OnKeys -= OnGetKeys;
                }
                // If all data is received.
                if (reply.Notify.IsComplete && !reply.Notify.IsMoreData)
                {
                    if (GXAgentWorker.Options.TraceLevel == TraceLevel.Verbose)
                    {
                        //Show data as XML in the console.
                        string xml;
                        GXDLMSTranslator t = new GXDLMSTranslator();
                        t.DataToXml(reply.Notify.Data, out xml);
                        _logger.LogDebug(xml);
                    }
                    //Send agent trace.
                    if (settings != null && settings.TraceLevel >= TraceLevel.Info)
                    {
                        string xml;
                        GXDLMSTranslator t = new GXDLMSTranslator();
                        t.DataToXml(reply.Notify.Data, out xml);
                        //Agent trace.
                        GXAgentLog trace = new GXAgentLog(TraceLevel.Info);
                        trace.Agent = new GXAgent() { Id = GXAgentWorker.Options.Id };
                        trace.Message = xml;
                        AddAgentLog it = new AddAgentLog();
                        it.Logs = new GXAgentLog[] { trace };
                        GXAgentWorker.client.PostAsJson<AddDeviceTraceResponse>("/api/AgentLog/Add", it).Wait();
                    }
                    try
                    {
                        if (scriptMethod != null && scriptMethod.Script != null)
                        {
                            //Get the device from the cache.
                            string st = GXCommon.ToHex(reply.Notify.SystemTitle, false);
                            GXDevice? device = (GXDevice)_cachedDevices.Get(st);

                            GXAmiScript tmp = new GXAmiScript(_serviceProvider);
                            tmp.Sender = new GXAgent() { Id = GXAgentWorker.Options.Id };
                            //Run script.
                            GXScriptRunArgs args = new GXScriptRunArgs()
                            {
                                ByteAssembly = scriptMethod.Script.ByteAssembly,
                                MethodName = scriptMethod.Name,
                                Asyncronous = scriptMethod.Asyncronous,
                                AssemblyLoadContext = loadedScript,
                                Parameters = new object[]
                                {
                                    e.SenderInfo,
                                    device,
                                    reply.Notify.Time,
                                    reply.Notify.Value,
                                    }
                            };
                            tmp.RunAsync(args).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
                        AddAgentLog log = new AddAgentLog();
                        log.Logs = new GXAgentLog[]{new GXAgentLog()
                        {
                            Agent = new GXAgent(){Id =GXAgentWorker.Options.Id },
                            Message = ex.Message,
                            StackTrace =ex.StackTrace
                        } };
                        GXAgentWorker.client.PostAsJson("/api/AgentLog/Add", log).Wait();
                        if (scriptMethod != null && scriptMethod.Script != null)
                        {
                            AddScriptLog se = new AddScriptLog();
                            se.Logs = new GXScriptLog[]{new GXScriptLog()
                        {
                            Script = scriptMethod.Script,
                            Message = ex.Message
                        } };
                            //Reset parent so it doesn't cause problems with JSON.
                            GXScript original = scriptMethod.Script;
                            scriptMethod.Script = null;
                            try
                            {
                                GXAgentWorker.client.PostAsJson<AddAgentLogResponse>("/api/ScriptLog/Add", se).Wait();
                            }
                            finally
                            {
                                scriptMethod.Script = original;
                            }
                        }
                    }
                    finally
                    {
                        reply.Notify.Clear();
                        //Don't call clear here.
                        //There might be bytes from the next frame waiting.
                        reply.Reply.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
                AddAgentLog log = new AddAgentLog();
                log.Logs = new GXAgentLog[]{new GXAgentLog()
                        {
                            Agent = new GXAgent(){Id =GXAgentWorker.Options.Id },
                            Message = ex.Message,
                            StackTrace =ex.StackTrace
                        } };
                try
                {
                    GXAgentWorker.client.PostAsJson("/api/AgentLog/Add", log).Wait();
                }
                catch (Exception ex2)
                {
                    //If connection is lost for the server.
                    _logger.LogError(ex2.Message + Environment.NewLine + ex2.StackTrace);
                }
            }
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogWarning("Notify service is stopping.");
            if (notify != null)
            {
                notify.OnReceived -= OnNotifyReceived;
                notify.Close();
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (notify != null)
            {
                notify.Dispose();
                notify = null;
            }
        }
    }
}
