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
using Gurux.DLMS.AMI.Agent.Worker;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Script;
using System.Runtime.Loader;

namespace Gurux.DLMS.AMI.Agent.Notifier
{
    /// <inheritdoc/>
    internal class GXNotifyService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GXNotifyService> _logger;
        NotifySettings? settings;
        //Notify wait push, events or notifies from the meters.
        GXNet? notify;

        static public int ExpirationTime = 0;
        /// <summary>
        /// Each client has own message queue.
        /// </summary>
        static Dictionary<string, GXNotifyClient> notifyMessages = new Dictionary<string, GXNotifyClient>();

        GXScriptMethod? scriptMethod;
        AssemblyLoadContext? loadedScript;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXNotifyService(ILogger<GXNotifyService> logger, IServiceProvider serviceProvider)
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
                        ListScripts req = new ListScripts();
                        req.Filter = new GXScript();
                        req.Filter.Methods.Add(new GXScriptMethod() { Id = settings.ScriptMethod.Value });
                        ListScriptsResponse? ret = GXAgentWorker.client.PostAsJson<ListScriptsResponse>("/api/Script/List", req).Result;
                        if (ret == null || ret.Scripts.Length != 1)
                        {
                            throw new Exception("Unknown script to execute.");
                        }
                        scriptMethod = ret.Scripts[0].Methods.SingleOrDefault();
                        //Update parent script.
                        scriptMethod.Script = ret.Scripts[0];
                    }
                    else
                    {
                        scriptMethod = null;
                    }
                    notify = new GXNet((NetworkType)net.Protocol, net.Port);
                    ExpirationTime = settings.ExpirationTime;
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
        /// Handle received notify message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void OnNotifyReceived(object sender, ReceiveEventArgs e)
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
                        settings.Interface, settings.SystemTitle, settings.BlockCipherKey);
                    notifyMessages.Add(e.SenderInfo, reply);
                }
            }
            DateTime now = DateTime.Now;
            //If received data is expired.
            if (ExpirationTime != 0 && (now - reply.DataReceived).TotalSeconds > ExpirationTime)
            {
                reply.Reply.Clear();
            }
            reply.DataReceived = now;
            reply.Reply.Set((byte[])e.Data);
            GXReplyData data = new GXReplyData();
            reply.Client.GetData(reply.Reply, data, reply.Notify);
            // If all data is received.
            if (reply.Notify.IsComplete && !reply.Notify.IsMoreData)
            {
                if (GXAgentWorker.Options.TraceLevel == System.Diagnostics.TraceLevel.Verbose)
                {
                    //Show data as XML in the console.
                    string xml;
                    GXDLMSTranslator t = new GXDLMSTranslator();
                    t.DataToXml(reply.Notify.Data, out xml);
                    _logger.LogDebug(xml);
                }
                if (settings != null && settings.TraceLevel == System.Diagnostics.TraceLevel.Verbose)
                {
                    //Meter trace.
                    GXDeviceTrace trace = new GXDeviceTrace();
                    trace.Send = false;
                    trace.Frame = reply.Notify.Data.ToHex(false, 0);
                    AddDeviceTrace it = new AddDeviceTrace();
                    it.Traces = new GXDeviceTrace[] { trace };
                    GXAgentWorker.client.PostAsJson<AddDeviceTraceResponse>("/api/DeviceTrace/Add", it).Wait();
                }
                try
                {
                    if (scriptMethod != null && scriptMethod.Script != null)
                    {
                        GXAmiScript tmp = new GXAmiScript(_serviceProvider);
                        tmp.Sender = new GXAgent() { Id = GXAgentWorker.Options.Id };
                        tmp.Data = reply.Notify.Value;
                        //Run script.
                        GXScriptRunArgs args = new GXScriptRunArgs()
                        {
                            ByteAssembly = scriptMethod.Script.ByteAssembly,
                            MethodName = scriptMethod.Name,
                            Asyncronous = scriptMethod.Asyncronous,
                            AssemblyLoadContext = loadedScript
                        };
                        tmp.RunAsync(args).Wait();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
                    AddAgentLog ae = new AddAgentLog();
                    ae.Logs = new GXAgentLog[]{new GXAgentLog()
                        {
                            Agent = new GXAgent(){Id =GXAgentWorker.Options.Id },
                            Message = ex.Message
                        } };
                    GXAgentWorker.client.PostAsJson("/api/AgentError/Add", ae).Wait();
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
                            GXAgentWorker.client.PostAsJson("/api/ScriptError/Add", se).Wait();
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
                    reply.Reply.Clear();
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
