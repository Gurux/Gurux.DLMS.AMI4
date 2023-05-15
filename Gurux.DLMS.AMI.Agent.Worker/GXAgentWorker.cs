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
using Gurux.DLMS.AMI.Agent.Notifier;
using Gurux.DLMS.AMI.Agent.Shared;
using Gurux.DLMS.AMI.Agent.Worker.Repositories;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.Enums;
using Gurux.DLMS.Objects;
using Gurux.DLMS.Secure;
using Gurux.Net;
using Gurux.Serial;
using Gurux.Terminal;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;

namespace Gurux.DLMS.AMI.Agent.Worker
{
    /// <inheritdoc />
    public class GXAgentWorker : IGXAgentWorker
    {
        /// <summary>
        /// Application is closing.
        /// </summary>
        private readonly ManualResetEvent _closing;
        /// <summary>
        /// New task is received.
        /// </summary>
        private readonly AutoResetEvent _newTask;
        private static HubConnection? _hubConnection = null;
        private static ILogger? _logger;
        static AutoResetEvent _newVersion;
        ServiceProvider? _services;
        private CancellationToken cancellationToken = new CancellationToken();
        /// <summary>
        /// Common http client.
        /// </summary>
        public static HttpClient client = new HttpClient();

        ActionBlock<GXActionBlock> _meterReads;

        /// <summary>
        /// Agent connection options.
        /// </summary>
        public static AgentOptions Options
        {
            get;
            private set;
        } = new AgentOptions();


        /// <inheritdoc />
        public bool PollTasks { get; set; }

        private class GXActionBlock
        {
            public IEnumerable<GXTask> Tasks;
            public AutoResetEvent NewTask;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXAgentWorker()
        {
            _closing = new ManualResetEvent(false);
            _newTask = new AutoResetEvent(true);
            if (Options != null && Options.ReaderSettings != null)
            {
                _meterReads = new ActionBlock<GXActionBlock>(_ => ReadMeter(_),
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Options.ReaderSettings.Threads
                });
            }
            else
            {
                //In default there is one thread.
                _meterReads = new ActionBlock<GXActionBlock>(_ => ReadMeter(_),
                    new ExecutionDataflowBlockOptions
                    {
                        MaxDegreeOfParallelism = 1
                    });
            }
        }

        /// <inheritdoc />
        public void Init(
            IServiceCollection services,
            AgentOptions options,
            AutoResetEvent newVersion)
        {
            _closing.Reset();
            Options = options;
            services.AddSingleton<GXNotifyService>();
            services.AddTransient<IDeviceErrorRepository, GXDeviceErrorRepository>();
            services.AddTransient<IAgentLogRepository, GXAgentLogRepository>();
            services.AddTransient<IAgentRepository, GXAgentRepository>();
            services.AddTransient<IDeviceRepository, GXDeviceRepository>();
            services.AddTransient<IObjectRepository, GXObjectRepository>();
            services.AddTransient<IValueRepository, GXValueRepository>();

            _services = services.BuildServiceProvider();
            _logger = _services.GetRequiredService<ILogger<GXAgentWorker>>();

            _newVersion = newVersion;
            client.BaseAddress = new Uri(Options.Address);
            if (Options.Token != null)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Options.Token);
            }
        }

        /// <inheritdoc />
        public async Task<Guid> AddAgentAsync(string name)
        {
            GXAgent agent = new GXAgent() { Name = name };
            UpdateAgent req = new UpdateAgent() { Agents = new GXAgent[] { agent } };
            if (client.DefaultRequestHeaders.Authorization == null && Options.Token != null)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GXAgentWorker.Options.Token);
            }
            UpdateAgentResponse? response = await client.PostAsJson<UpdateAgentResponse>("/api/Agent/Add", req);
            if (response == null || response.AgentIds == null || !response.AgentIds.Any())
            {
                return Guid.Empty;
            }
            return response.AgentIds[0];
        }

        private static string DownloadAgent(string uri)
        {
            string compressedFile = Path.GetTempFileName();
            //Load with http or https.
            WebClient client = new WebClient();
            client.DownloadFile(uri, compressedFile);
            return compressedFile;
        }

        /// <summary>
        /// Install new version from the agent if it exists.
        /// </summary>
        private async System.Threading.Tasks.Task InstallNewVersion()
        {
            try
            {
                GXAgent? agent;
                GetAgentResponse? res = await Helpers.GetAsync<GetAgentResponse>(string.Format("/api/Agent/?Id={0}", Options.Id));
                if (res == null)
                {
                    //Read with old way.
                    agent = await Helpers.GetAsync<GXAgent>(string.Format("/api/Agent/?Id={0}", Options.Id));
                }
                else
                {
                    agent = res.Item;
                }
                if (agent != null)
                {
                    if (!string.IsNullOrEmpty(agent.ReaderSettings))
                    {
                        var tmp = JsonSerializer.Deserialize<ReaderSettings?>(agent.ReaderSettings);
                        if (tmp != null)
                        {
                            Options.ReaderSettings = tmp;
                            _logger?.LogInformation("Reader settings: " + Options.ReaderSettings.ToString());
                            _meterReads.Complete();
                            if (!string.IsNullOrEmpty(agent.SerialPort))
                            {
                                //There is only one thread when serial port is used.
                                tmp.Threads = 1;
                            }
                            _meterReads = new ActionBlock<GXActionBlock>(_ => ReadMeter(_),
                               new ExecutionDataflowBlockOptions
                               {
                                   MaxDegreeOfParallelism = tmp.Threads
                               });
                        }
                    }
                    if (!string.IsNullOrEmpty(agent.ListenerSettings))
                    {
                        var tmp = JsonSerializer.Deserialize<ListenerSettings?>(agent.ListenerSettings);
                        if (tmp != null)
                        {
                            Options.ListenerSettings = tmp;
                            _logger?.LogInformation("Listener settings: " + tmp.ToString());
                        }
                    }
                    if (!string.IsNullOrEmpty(agent.NotifySettings))
                    {
                        var tmp = JsonSerializer.Deserialize<NotifySettings?>(agent.NotifySettings);
                        if (tmp != null)
                        {
                            Options.NotifySettings = tmp;
                            _logger?.LogInformation("Notify settings: " + tmp.ToString());
                        }
                    }
                    Options.SerialPort = agent.SerialPort;
                    Options.SerialPorts = agent.SerialPorts;
                    await UpdateAgentSerialPorts();
                    _logger?.LogInformation("Agent '{0}' started.", agent.Name);
                }
                if (!string.IsNullOrEmpty(agent?.UpdateVersion) &&
                    //The current version will not be updated again.
                    Options.Version != agent.UpdateVersion)
                {
                    DownloadAgent request = new DownloadAgent();
                    request.Agent = new GXAgent() { Id = agent.Id, UpdateVersion = agent.UpdateVersion };
                    DownloadAgentResponse? ret = await GXAgentWorker.client.PostAsJson<DownloadAgentResponse>("/api/Agent/Download", request);
                    if (ret != null && ret.Urls != null && ret.Urls.Any())
                    {
                        //Download the new agent version.
                        foreach (var it in ret.Urls)
                        {
                            await UpdateAgentStatusAsync(AgentStatus.Downloading);
                            string compressedFile = DownloadAgent(it);
                            await UpdateAgentStatusAsync(AgentStatus.Updating);
                            //Unzip the file to the version mumber folder.
                            ZipFile.ExtractToDirectory(compressedFile, "bin" + agent.UpdateVersion, true);
                            Options.Version = agent.UpdateVersion;
                            agent.Version = agent.UpdateVersion;
                            agent.UpdateVersion = null;
                            agent.Status = AgentStatus.Restarting;
                            agent.Tasks?.Clear();
                            agent.AgentGroups?.Clear();
                            agent.Logs?.Clear();
                            agent.Versions?.Clear();
                            await UpdateAgentAsync(agent);
                            _logger?.LogInformation("The new version installed and the agent is restarting.");
                            //Restart application.
                            _newVersion.Set();
                            break;
                        }
                    }
                }
                else if (agent != null && agent.Version != Options.Version)
                {
                    //Update agent version.
                    agent.Version = Options.Version;
                    await UpdateAgentAsync(agent);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("Install new version failed. " + ex.Message);
            }
        }

        /// <summary>
        /// Get existed tasks.
        /// </summary>
        /// <returns></returns>
        private async System.Threading.Tasks.Task TaskPoller()
        {
            while (!_closing.WaitOne(0))
            {
                try
                {
                    if (Options.ReaderSettings != null && Options.ReaderSettings.Active &&
                        //If the buffer is full.
                        _meterReads.InputCount < Options.ReaderSettings.Threads &&
                        AutoResetEvent.WaitAny(new WaitHandle[] { _newTask, _closing }) == 0)
                    {
                        GetNextTask req = new GetNextTask();
                        req.AgentId = Options.Id;
                        GetNextTaskResponse? response = await client.PostAsJson<GetNextTaskResponse>("/api/Task/Next", req);
                        if (response != null && response.Tasks != null && response.Tasks.Any())
                        {
                            //Read the meter if it's not read at the moment with other thread.                            
                            await _meterReads.SendAsync(new GXActionBlock()
                            {
                                Tasks = response.Tasks,
                                NewTask = _newTask
                            });
                        }
                    }
                    else
                    {
                        //Wait 10 seconds before next attempt.
                        if (!_closing.WaitOne(0))
                        {
                            _closing.WaitOne(10000);
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger?.LogError("Get next task failed. " + ex.Message);
                    //Server is closed. Wait 10 seconds before next attempt.
                    _closing.WaitOne(10000);
                }
                catch (Exception ex)
                {
                    _logger?.LogError("Get next task failed. " + ex.Message);
                }
            }
        }

        private static async System.Threading.Tasks.Task UpdateAgentAsync(GXAgent agent)
        {
            UpdateAgent req = new UpdateAgent();
            req.Agents = new GXAgent[] { agent };
            await client.PostAsJson("/api/Agent/Update", req);
        }

        private static async System.Threading.Tasks.Task UpdateAgentStatusAsync(AgentStatus status)
        {
            UpdateAgentStatus req = new UpdateAgentStatus();
            req.Id = Options.Id;
            req.Status = status;
            await client.PostAsJson("/api/Agent/UpdateStatus", req);
        }

        /// <summary>
        /// Serial ports are added or removed from the agent.
        /// </summary>
        /// <returns></returns>
        private static async System.Threading.Tasks.Task UpdateAgentSerialPorts()
        {
            string ports = string.Join(";", GXSerial.GetPortNames());
            if (ports != Options.SerialPorts)
            {
                UpdateAgentStatus req = new UpdateAgentStatus();
                req.Id = Options.Id;
                req.Status = AgentStatus.SerialPortChange;
                req.Data = string.Join(";", ports);
                await client.PostAsJson("/api/Agent/UpdateStatus", req);
            }
        }


        private static async System.Threading.Tasks.Task ExecuteTasks(GXDevice dev, GXDLMSReader reader, GXDLMSSecureClient cl, GXTask task)
        {
            GXDLMSObject obj;
            if (task.Object != null && task.Object.Template != null)
            {
                obj = GXDLMSClient.CreateObject((ObjectType)task.Object.Template.ObjectType);
                obj.Version = task.Object.Template.Version;
                obj.LogicalName = task.Object.Template.LogicalName;
                obj.ShortName = task.Object.Template.ShortName.GetValueOrDefault();
            }
            else if (task.Attribute != null && task.Attribute.Object != null &&
                task.Attribute.Object.Template != null)
            {
                obj = GXDLMSClient.CreateObject((ObjectType)task.Attribute.Object.Template.ObjectType);
                obj.Version = task.Attribute.Object.Template.Version;
                obj.LogicalName = task.Attribute.Object.Template.LogicalName;
                obj.ShortName = task.Attribute.Object.Template.ShortName.GetValueOrDefault();
            }
            else if (task.Device != null)
            {
                foreach (var it in task.Device.Objects)
                {
                    task.Object = it;
                    await ExecuteTasks(dev, reader, cl, task);
                }
                return;
            }
            else
            {
                throw new ArgumentException("Invalid object.");
            }
            if (task.TaskType == TaskType.Write)
            {
                DataType dt = obj.GetDataType(2);
                DataType uiDt = obj.GetUIDataType(2);
                //If date-time value is updated.
                if (uiDt == DataType.DateTime ||
                    uiDt == DataType.Date ||
                    uiDt == DataType.Time)
                {
                    if (dt == DataType.OctetString)
                    {
                        //If value is null currect date time is used.
                        if (string.IsNullOrEmpty(task.Data))
                        {
                            if (dt == DataType.OctetString || dt == DataType.DateTime || dt == DataType.Date || dt == DataType.Time)
                            {
                                cl.UpdateValue(obj, task.Index.GetValueOrDefault(0), DateTime.Now);
                            }
                            else
                            {
                                cl.UpdateValue(obj, task.Index.GetValueOrDefault(0), GXDateTime.ToUnixTime(DateTime.UtcNow));
                            }
                        }
                        else
                        {
                            if (uiDt == DataType.DateTime)
                            {
                                cl.UpdateValue(obj, task.Index.GetValueOrDefault(0), new GXDateTime(task.Data, CultureInfo.InvariantCulture));
                            }
                            else if (uiDt == DataType.Date)
                            {
                                cl.UpdateValue(obj, task.Index.GetValueOrDefault(0), new GXDate(task.Data, CultureInfo.InvariantCulture));
                            }
                            else if (uiDt == DataType.Time)
                            {
                                cl.UpdateValue(obj, task.Index.GetValueOrDefault(0), new GXTime(task.Data, CultureInfo.InvariantCulture));
                            }
                        }
                    }
                }
                else
                {
                    cl.UpdateValue(obj, task.Index.GetValueOrDefault(0), GXDLMSTranslator.XmlToValue(task.Data));
                }
                reader.Write(obj, task.Index.GetValueOrDefault(0));
            }
            else if (task.TaskType == TaskType.Action)
            {
                reader.Method(obj, task.Index.GetValueOrDefault(0), GXDLMSTranslator.XmlToValue(task.Data), DataType.None);
            }
            else if (task.TaskType == TaskType.Read)
            {
                try
                {
                    if (task.Attribute != null)
                    {
                        if (task.Attribute.Template == null)
                        {
                            throw new ArgumentException("Attribute template is missing.");
                        }
                        if (task.Attribute.Template.DataType != 0)
                        {
                            obj.SetDataType(task.Attribute.Template.Index, (DataType)task.Attribute.Template.DataType);
                        }
                        if (task.Attribute.Template.UIDataType != 0)
                        {
                            obj.SetUIDataType(task.Attribute.Template.Index, (DataType)task.Attribute.Template.UIDataType);
                        }
                        task.Object = task.Attribute.Object;
                        if (task.Object == null)
                        {
                            throw new ArgumentException("Task object is missing.");
                        }
                        if (task.Object.Attributes == null)
                        {
                            task.Object.Attributes = new List<GXAttribute>();
                        }
                        task.Object.Attributes.Add(task.Attribute);
                        task.Index = task.Attribute.Template.Index;
                        await Reader.Read(_logger, reader, task, obj);
                        if (task.Attribute.Template.DataType == 0)
                        {
                            task.Attribute.Template.DataType = (int)obj.GetDataType(task.Index.GetValueOrDefault(0));
                            if (task.Attribute.Template.UIDataType == 0)
                            {
                                task.Attribute.Template.UIDataType = (int)obj.GetUIDataType(task.Index.GetValueOrDefault(0));
                            }
                            UpdateDatatype u = new UpdateDatatype()
                            {
                                Items = new GXAttribute[] { new GXAttribute()
                                    {
                                        Id = task.Attribute.Id,
                                        Template = new GXAttributeTemplate()
                                        {
                                            Id = task.Attribute.Template.Id,
                                            DataType = task.Attribute.Template.DataType
                                        }
                                    }
                                }
                            };
                            await client.PostAsJson("/api/Attribute/UpdateDatatype", u);
                        }
                    }
                    else if (task.Object != null)
                    {
                        if (task.Object.Attributes == null)
                        {
                            throw new ArgumentException("Object attributes is missing.");
                        }
                        foreach (var attribute in task.Object.Attributes)
                        {
                            if (attribute.Template == null)
                            {
                                throw new ArgumentException("Attribute template is missing.");
                            }
                            if ((attribute.Template.AccessLevel & (int)Enums.AccessMode.Read) == 0)
                            {
                                _logger?.LogInformation($"Ignoring attribute {attribute.Template.Index}");
                                continue;
                            }
                            if (attribute.Template.DataType != 0)
                            {
                                obj.SetDataType(attribute.Template.Index, (DataType)attribute.Template.DataType);
                            }
                            if (attribute.Template.UIDataType != 0)
                            {
                                obj.SetUIDataType(attribute.Template.Index, (DataType)attribute.Template.UIDataType);
                            }
                            task.Object = task.Object;
                            task.Index = attribute.Template.Index;
                            await Reader.Read(_logger, reader, task, obj);
                            if (attribute.Template.DataType == 0)
                            {
                                attribute.Template.DataType = (int)obj.GetDataType(task.Index.GetValueOrDefault(0));
                                if (attribute.Template.UIDataType == 0)
                                {
                                    attribute.Template.UIDataType = (int)obj.GetUIDataType(task.Index.GetValueOrDefault(0));
                                }
                                UpdateDatatype u = new UpdateDatatype()
                                {
                                    Items = new GXAttribute[] { new GXAttribute()
                                    {
                                        Id = attribute.Id,
                                        Template = new GXAttributeTemplate()
                                        {
                                            Id = attribute.Template.Id,
                                            DataType = attribute.Template.DataType
                                        }
                                    }
                                }
                                };
                                await client.PostAsJson("/api/Attribute/UpdateDatatype", u);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    task.Result = ex.Message;
                    await ReportDeviceException(dev, ex, "Failed to " + task.TaskType + " " + obj.LogicalName + ":" + task.Index + ". ");
                }
            }
        }

        private static async System.Threading.Tasks.Task ReadMeter(GXActionBlock action)
        {
            GXDLMSSecureClient cl;
            IGXMedia? media;
            int pos;
            GXDLMSReader reader;
            IEnumerable<GXTask> tasks = action.Tasks;
            GXTask firstTask = tasks.First();
            GXDevice dev;
            GetDeviceResponse ret;
            try
            {
                ret = await Helpers.GetAsync<GetDeviceResponse>(string.Format("/api/Device/?Id={0}", firstTask.TargetDevice));
                if (ret == null)
                {
                    //Read using old way.
                    dev = await Helpers.GetAsync<GXDevice>(string.Format("/api/Device/?Id={0}", firstTask.TargetDevice));
                }
                else
                {
                    dev = ret.Item;
                }
            }
            catch (Exception ex)
            {
                //Remove all tasks if meter reading fails.
                foreach (GXTask task in tasks)
                {
                    task.Result = ex.Message;
                }
                await ReportDone(tasks);
                _logger?.LogError(ex.Message);
                action.NewTask.Set();
                return;
            }
            try
            {
                try
                {
                    if (string.Compare(dev.MediaType, typeof(GXNet).FullName, true) == 0)
                    {
                        media = new GXNet();
                    }
                    else if (string.Compare(dev.MediaType, typeof(GXSerial).FullName, true) == 0)
                    {
                        media = new GXSerial();
                    }
                    else if (string.Compare(dev.MediaType, typeof(GXTerminal).FullName, true) == 0)
                    {
                        media = new GXTerminal();
                    }
                    else
                    {
                        Type type = Type.GetType(dev.MediaType);
                        if (type == null)
                        {
                            string ns = "";
                            pos = dev.MediaType.LastIndexOf('.');
                            if (pos != -1)
                            {
                                ns = dev.MediaType.Substring(0, pos);
                            }
                            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                            {
                                if (assembly.GetName().Name == ns)
                                {
                                    if (assembly.GetType(dev.MediaType, false, true) != null)
                                    {
                                        type = assembly.GetType(dev.MediaType);
                                    }
                                }
                            }
                        }
                        if (type == null)
                        {
                            throw new Exception("Invalid media type: " + dev.MediaType);
                        }
                        media = (IGXMedia?)Activator.CreateInstance(type);
                    }
                    if (media == null)
                    {
                        throw new Exception("Unknown media type '" + dev.MediaType + "'.");
                    }
                    media.Settings = dev.MediaSettings;
                    if (media is GXSerial serial)
                    {
                        //Update used serial port from the agent options.
                        serial.PortName = Options.SerialPort;
                    }
                    var settings = JsonSerializer.Deserialize<AMI.Shared.DTOs.GXDLMSSettings>(dev.Settings);
                    var templateSettings = JsonSerializer.Deserialize<AMI.Shared.DTOs.GXDLMSSettings>(dev.Settings);
                    int deviceAddress;
                    if (settings.HDLCAddressing == (int)ManufacturerSettings.HDLCAddressType.SerialNumber)
                    {
                        deviceAddress = GXDLMSClient.GetServerAddressFromSerialNumber(settings.PhysicalAddress,
                            settings.LogicalAddress,
                            settings.SerialNumberFormula);
                    }
                    else
                    {
                        if (settings.LogicalAddress != 0 &&
                            (settings.InterfaceType == (int)InterfaceType.HDLC || settings.InterfaceType == (int)InterfaceType.HdlcWithModeE))
                        {
                            deviceAddress = GXDLMSClient.GetServerAddress(settings.LogicalAddress, settings.PhysicalAddress);
                        }
                        else
                        {
                            deviceAddress = settings.PhysicalAddress;
                        }
                    }
                    TraceLevel consoleTrace = Options.TraceLevel;
                    TraceLevel deviceTrace = dev.TraceLevel.GetValueOrDefault(TraceLevel.Off);
                    //Read frame counter from the meter.
                    if (templateSettings.Security != 0)
                    {
                        cl = new GXDLMSSecureClient(templateSettings.UseLogicalNameReferencing, 16, deviceAddress,
                            Authentication.None, null, (InterfaceType)settings.InterfaceType);
                        reader = new GXDLMSReader(cl, media, _logger, consoleTrace, deviceTrace, dev.WaitTime, dev.ResendCount, dev);
                        media.Open();
                        reader.InitializeConnection();
                        //Read Innovation counter.
                        GXDLMSData d = new GXDLMSData(settings.FrameCounter);
                        await reader.Read(d, 2);
                        settings.InvocationCounter = 1 + Convert.ToUInt32(d.Value);
                        reader.Disconnect();
                        media.Close();
                    }

                    cl = new GXDLMSSecureClient(templateSettings.UseLogicalNameReferencing, templateSettings.ClientAddress, deviceAddress,
                        (Authentication)templateSettings.Authentication, null, (InterfaceType)settings.InterfaceType);
                    if (cl.InterfaceType == InterfaceType.HDLC || cl.InterfaceType == InterfaceType.HdlcWithModeE)
                    {
                        cl.HdlcSettings.MaxInfoTX = templateSettings.MaxInfoTX;
                        cl.HdlcSettings.MaxInfoRX = templateSettings.MaxInfoRX;
                        cl.HdlcSettings.WindowSizeRX = templateSettings.WindowSizeRX;
                        cl.HdlcSettings.WindowSizeTX = templateSettings.WindowSizeTX;
                    }
                    cl.Password = settings.HexPassword;
                    cl.UseUtc2NormalTime = templateSettings.UtcTimeZone;
                    cl.Standard = (Standard)templateSettings.Standard;
                    cl.Ciphering.SystemTitle = GXCommon.HexToBytes(settings.ClientSystemTitle);
                    if (cl.Ciphering.SystemTitle != null && cl.Ciphering.SystemTitle.Length == 0)
                    {
                        cl.Ciphering.SystemTitle = null;
                    }
                    cl.Ciphering.BlockCipherKey = GXCommon.HexToBytes(settings.BlockCipherKey);
                    if (cl.Ciphering.BlockCipherKey != null && cl.Ciphering.BlockCipherKey.Length == 0)
                    {
                        cl.Ciphering.BlockCipherKey = null;
                    }
                    cl.Ciphering.AuthenticationKey = GXCommon.HexToBytes(settings.AuthenticationKey);
                    if (cl.Ciphering.AuthenticationKey != null && cl.Ciphering.AuthenticationKey.Length == 0)
                    {
                        cl.Ciphering.AuthenticationKey = null;
                    }
                    cl.ServerSystemTitle = GXCommon.HexToBytes(settings.DeviceSystemTitle);
                    if (cl.ServerSystemTitle != null && cl.ServerSystemTitle.Length == 0)
                    {
                        cl.ServerSystemTitle = null;
                    }
                    cl.Ciphering.InvocationCounter = settings.InvocationCounter;
                    cl.Ciphering.Security = (Security)templateSettings.Security;
                    reader = new GXDLMSReader(cl, media, _logger,
                        consoleTrace, deviceTrace, dev.WaitTime, dev.ResendCount, dev);
                    media.Open();
                    reader.InitializeConnection();
                }
                catch (Exception ex)
                {
                    foreach (GXTask task in tasks)
                    {
                        task.Result = "Failed to establish the connection." + ex.Message;
                    }
                    await ReportDone(tasks);
                    await ReportDeviceException(dev, ex, "Failed to establish the connection.");
                    return;
                }
                int count = tasks.Count();
                pos = 0;
                foreach (GXTask task in tasks)
                {
                    ++pos;
                    try
                    {
                        await ExecuteTasks(dev, reader, cl, task);
                    }
                    catch (Exception ex)
                    {
                        task.Result = ex.Message;
                        await ReportDeviceException(dev, ex, "Failed to execute task " + task);
                    }
                    task.Ready = DateTime.Now;
                    //Report each task invidially.
                    try
                    {
                        await ReportDone(new GXTask[] { task });
                    }
                    catch (Exception ex)
                    {
                        await ReportDeviceException(dev, ex, "Meter read failed.");
                    }
                    //Close connection after last task is executed.
                    //This must done because there might be new task to execute.
                    if (count == pos)
                    {
                        try
                        {
                            reader.Close();
                        }
                        catch (Exception ex)
                        {
                            await ReportDeviceException(dev, ex, "Meter read failed.");
                        }
                    }
                }
            }
            catch (Exception)
            {
                //ReportDone failed for some reason. Ignore it.
            }
            finally
            {
                action.NewTask.Set();
            }
        }

        /// <summary>
        /// Report task exception.
        /// </summary>
        /// <param name="dev">Executed device.</param>
        /// <param name="ex">Exception.</param>
        /// <param name="message">Exception message.</param>
        private static async System.Threading.Tasks.Task ReportDeviceException(
            GXDevice dev,
            Exception ex,
            string message)
        {
            AddDeviceError error = new AddDeviceError();
            error.Errors = new GXDeviceError[]{new GXDeviceError()
                        {
                            Device = dev,
                            Message = message + " " + ex.Message
                        } };
            _logger?.LogError(error.Errors[0].Message);
            await client.PostAsJson("/api/DeviceError/Add", error);
        }

        /// <summary>
        /// Report that task is done.
        /// </summary>
        /// <param name="tasks">Executed tasks.</param>
        private static async System.Threading.Tasks.Task ReportDone(IEnumerable<GXTask> tasks)
        {
            List<GXTask> list = new List<GXTask>();
            foreach (GXTask task in tasks)
            {
                list.Add(new GXTask()
                {
                    Id = task.Id,
                    Result = task.Result
                });
            }
            await client.PostAsJson<TaskDoneResponse>("/api/task/Done", new TaskDone() { Tasks = list.ToArray() });
        }

        /// <inheritdoc />
        public async System.Threading.Tasks.Task StartAsync()
        {
            Assembly asm = typeof(GXAgentWorker).Assembly;
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(asm.Location);
            _logger?.LogInformation(string.Format("Starting Gurux.DLMS.Agent.Worker version {0}", string.Join(";", info.FileVersion)));
            Uri uri = new Uri(new Uri(Options.Address), "/guruxami");
            _hubConnection = new HubConnectionBuilder()
            .WithUrl(uri, o =>
            {
                o.AccessTokenProvider = async () =>
                {
                    return Options.Token;
                };
            })
            .WithAutomaticReconnect()
            .Build();
            _hubConnection.On<IEnumerable<GXTask>>("TaskAdd", async (tasks) =>
            {
                _logger?.LogInformation("New task added.");
                _newTask.Set();
            });
            _hubConnection.On<IEnumerable<GXAgent>>("AgentUpdate", async (agents) =>
            {
                foreach (GXAgent agent in agents)
                {
                    if (agent.Id == Options.Id)
                    {
                        _logger?.LogInformation("Agent '{0}' updated.", agent.Name);
                        if (!string.IsNullOrEmpty(agent.ReaderSettings) &&
                            JsonSerializer.Serialize(Options.ReaderSettings) != agent.ReaderSettings)
                        {
                            //MIKKO agent.SerialPort
                            int threadCount = Options.ReaderSettings.Threads;
                            ReaderSettings? rs = JsonSerializer.Deserialize<ReaderSettings>(agent.ReaderSettings);
                            if (rs != null)
                            {
                                _logger?.LogInformation("Agent reader settings updated. " +
                                    Environment.NewLine + rs.ToString());
                                Options.ReaderSettings = rs;
                                if (threadCount != Options.ReaderSettings.Threads)
                                {
                                    _meterReads.Complete();
                                    _meterReads = new ActionBlock<GXActionBlock>(_ => ReadMeter(_),
                                    new ExecutionDataflowBlockOptions
                                    {
                                        MaxDegreeOfParallelism = Options.ReaderSettings.Threads
                                    });
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(agent.NotifySettings) &&
                        JsonSerializer.Serialize(Options.NotifySettings) != agent.NotifySettings)
                        {
                            NotifySettings? s = JsonSerializer.Deserialize<NotifySettings>(agent.NotifySettings);
                            if (s != null)
                            {
                                _logger?.LogInformation("Agent notify settings updated. " +
                                    Environment.NewLine + s.ToString());
                                Options.NotifySettings = s;
                                GXNotifyService? ns = _services?.GetRequiredService<GXNotifyService>();
                                ns?.StopAsync(cancellationToken);
                                ns?.StartAsync(cancellationToken);
                            }
                        }
                        if (!string.IsNullOrEmpty(agent.ListenerSettings) &&
                        JsonSerializer.Serialize(Options.ListenerSettings) != agent.ListenerSettings)
                        {
                            var ls = JsonSerializer.Deserialize<ListenerSettings>(agent.ListenerSettings);
                            if (ls != null)
                            {
                                _logger?.LogInformation("Agent listener settings updated. " +
                                    Environment.NewLine + ls.ToString());
                                Options.ListenerSettings = ls;
                            }
                        }
                        //if the version is updated.
                        if (!string.IsNullOrEmpty(agent.UpdateVersion))
                        {
                            _logger?.LogInformation(string.Format("Agent version {0} upgraded to version {1}.", Options.Version, agent.UpdateVersion));
                            await InstallNewVersion();
                        }
                    }
                }
            });
            _hubConnection.Reconnecting += _hubConnection_Reconnecting;
            _hubConnection.Reconnected += _hubConnection_Reconnected;
            _hubConnection.Closed += _hubConnection_Closed;
            await _hubConnection.StartAsync();
            _logger?.LogInformation("Waiting tasks from the Gurux.DLMS.AMI.");
            await UpdateAgentStatusAsync(AgentStatus.Connected);
            //Check new versions to install.
            await InstallNewVersion();
            GXNotifyService? ns = _services?.GetRequiredService<GXNotifyService>();
            ns?.StartAsync(cancellationToken).Wait();
            await System.Threading.Tasks.Task.Factory.StartNew(TaskPoller);
            _logger?.LogInformation("Agent started");
        }

        private async System.Threading.Tasks.Task _hubConnection_Closed(Exception? arg)
        {
            if (_hubConnection != null && !cancellationToken.IsCancellationRequested && !_closing.WaitOne(0))
            {
                _logger?.LogInformation("Reconnecting for the Gurux.DLMS.AMI.");
                while (true)
                {
                    try
                    {
                        if (_hubConnection != null && !cancellationToken.IsCancellationRequested &&
                            !_closing.WaitOne(0))
                        {
                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        //Wait minute before try to re-connect.
                        _logger?.LogError(ex.Message);
                        await System.Threading.Tasks.Task.Delay(1000);
                    }
                }
            }
        }

        private System.Threading.Tasks.Task _hubConnection_Reconnected(string? arg)
        {
            _logger?.LogInformation("Reconnected for the Gurux.DLMS.AMI.");
            return System.Threading.Tasks.Task.CompletedTask;
        }

        private System.Threading.Tasks.Task _hubConnection_Reconnecting(Exception? arg)
        {
            _logger?.LogInformation("Reconnecting for the Gurux.DLMS.AMI.");
            return System.Threading.Tasks.Task.CompletedTask;
        }

        /// <inheritdoc />
        public async System.Threading.Tasks.Task StopAsync()
        {
            try
            {
                _logger?.LogInformation("Stopping Gurux.DLMS.Agent.Worker.");
                _closing.Set();
                GXNotifyService? ns = _services?.GetRequiredService<GXNotifyService>();
                ns?.StopAsync(cancellationToken).Wait();
                if (_hubConnection != null)
                {
                    await _hubConnection.DisposeAsync();
                    _hubConnection = null;
                    await UpdateAgentStatusAsync(AgentStatus.Offline);
                }
            }
            catch (Exception)
            {
                //It's OK if this fails.
                _hubConnection = null;
            }
        }
    }
}