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
using Gurux.DLMS.AMI.Http;
using Gurux.DLMS.AMI.Agent.Shared;
using Gurux.DLMS.AMI.Agent.Worker.AutoConnect;
using Gurux.DLMS.AMI.Agent.Worker.Notifier;
using Gurux.DLMS.AMI.Agent.Worker.Repositories;
using Gurux.DLMS.AMI.Gateway.Worker.Repositories;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;

namespace Gurux.DLMS.AMI.Agent.Worker
{
    /// <inheritdoc />
    public partial class GXAgentWorker : IGXAgentWorker
    {
        /// <summary>
        /// Application is closing.
        /// </summary>
        private readonly ManualResetEvent _closing;
        /// <summary>
        /// New task is received.
        /// </summary>
        private readonly AutoResetEvent _newTask;
        private HubConnection? _hubConnection = null;
        private ILogger? _logger;
        private IGXTaskNotification? _taskNotification;
        private IDisposable? taskSubscription;
        private System.Threading.Tasks.Task? taskPoller;
        private GXTaskCompletionOutbox? completionOutbox;
        private CancellationTokenSource lifetime = new();
        private readonly SemaphoreSlim settingsGate = new(1, 1);
        private readonly SemaphoreSlim updateGate = new(1, 1);
        private bool restartPending;
        internal GXAgentNotification Notification { get; private set; } = new();
        private IGXAgentHostNotifications? _hostNotifications;
        IServiceProvider? _services;
        private GXAgentCapabilities capabilities = new(Array.Empty<string>());
        /// <inheritdoc />
        public GXAgentCapabilities Capabilities => capabilities;
        private CancellationToken cancellationToken = new();

        /// <summary>
        /// Gets the HTTP client used by this worker and its dependencies.
        /// </summary>
        /// <remarks>
        /// Initialization configures the base address and authorization header. Do not share this client with another worker.
        /// A supplied client remains owned by its caller and is accessed through an authentication-monitoring wrapper.
        /// </remarks>

        public HttpClient client { get; }
        private readonly HttpClient? suppliedClient;
        internal GXAgentScripts Scripts { get; }

        ActionBlock<GXActionBlock> _meterReads;

        /// <inheritdoc />
        public bool PollTasks { get; set; }

        internal class GXActionBlock
        {
            public required IEnumerable<GXTask> Tasks;
            public AutoResetEvent? NewTask;
            public IGXMedia? Media;
            public Guid? GatewayId;
            public CancellationToken CancellationToken;
        }

        /// <summary>
        /// Creates an uninitialized worker that owns its HTTP client.
        /// </summary>
        public GXAgentWorker() : this(new HttpClientHandler(), null) { }

        /// <summary>Creates a worker using a host-owned HTTP client.</summary>
        /// <param name="httpClient">A client dedicated to this worker. The caller must keep it alive until worker cleanup finishes.</param>
        /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> is <see langword="null"/>.</exception>
        /// <remarks>The worker owns a monitoring wrapper but does not dispose the supplied client. Initialization updates both clients' base address and authorization header.</remarks>
        public GXAgentWorker(HttpClient httpClient) : this(new GXHostHttpHandler(httpClient ?? throw new ArgumentNullException(nameof(httpClient))), httpClient) { }

        private readonly GXAuthenticationState authentication;
        internal bool AuthenticationRejected => authentication.IsRejected;

        private GXAgentWorker(HttpMessageHandler transport, HttpClient? httpClient)
        {
            authentication = new GXAuthenticationState(() => _hostNotifications?.NotifyAuthenticationRequired());
            suppliedClient = httpClient;
            client = new HttpClient(new GXAuthenticationHandler(transport, authentication));
            if (httpClient != null)
            {
                client.BaseAddress = httpClient.BaseAddress;
                client.Timeout = httpClient.Timeout;
                client.DefaultRequestVersion = httpClient.DefaultRequestVersion;
                client.DefaultVersionPolicy = httpClient.DefaultVersionPolicy;
            }
            Scripts = new GXAgentScripts(client);
            _closing = new ManualResetEvent(false);
            _newTask = new AutoResetEvent(true);
            _meterReads = new ActionBlock<GXActionBlock>(action => ReadMeter(action));
        }

        /// <inheritdoc />
        public void ConfigureServices(IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            ObjectDisposedException.ThrowIf(disposed, this);
            services.TryAddSingleton(new GXAgentUpdaterOptions());
            services.TryAddSingleton(new GXCompletionOutboxOptions());
            services.TryAddSingleton<IGXAgentUpdateService, GXAgentUpdateService>();
            services.AddSingleton(this);
            services.AddSingleton(client);
            services.AddSingleton<GXNotifyService>();
            services.AddSingleton<GXRawNotificationService>();
            services.AddSingleton<GXAutoConnectService>();
            services.AddSingleton<GXGatewayService>();
            services.AddTransient<IAgentRepository, GXAgentRepository>();
            services.AddTransient<ILogRepository, GXLogRepository>();
            services.AddTransient<IDeviceRepository, GXDeviceRepository>();
            services.AddTransient<IObjectRepository, GXObjectRepository>();
            services.AddTransient<IValueRepository, GXValueRepository>();
            services.AddTransient<IGatewayRepository, GXGatewayRepository>();
            services.AddSingleton<IGXTaskNotification, GXTaskNotification>();
        }

        /// <inheritdoc />
        public void Init(IServiceProvider services, GXAgentOptions options, IGXAgentHostNotifications notifications)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(notifications);
            ObjectDisposedException.ThrowIf(disposed, this);
            if (initialized || stopping)
                {
                throw new InvalidOperationException("A worker instance can only be initialized once, before it is stopped.");
            }
            options.Validate(requireAgentId: false);
            PublishConfiguration(options, requireAgentId: false);
            cancellationToken = lifetime.Token;

            Scripts.Clear();
            Notification = new GXAgentNotification();
            _meterReads.Complete();
            _meterReads = CreateMeterReads(RuntimeOptions.ReaderSettings?.Threads ?? 1);
            _services = services;
            _logger = _services.GetRequiredService<ILogger<GXAgentWorker>>();
            if (options.Id != Guid.Empty)
                {
                EnsureCompletionOutbox();
            }
            _taskNotification = _services.GetRequiredService<IGXTaskNotification>();
            taskSubscription?.Dispose();
            taskSubscription = Notification.On("task.add", () =>
            {
                _logger?.LogInformation("New task added.");
                _newTask.Set();
                // Event identifiers are task IDs, not meter IDs. Wake connections to fetch their own work.
                _taskNotification.NotifyAll();
            });
            _hostNotifications = notifications;
            client.BaseAddress = new Uri(RuntimeOptions.Address);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", RuntimeOptions.Token);
            if (suppliedClient != null)
            {
                suppliedClient.BaseAddress = client.BaseAddress;
                suppliedClient.DefaultRequestHeaders.Authorization = client.DefaultRequestHeaders.Authorization;
            }
            initialized = true;
            Volatile.Write(ref workerState, (int)GXAgentWorkerState.Initialized);
        }

        /// <inheritdoc />
        public async Task<Guid> AddAgentAsync(string name, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (!initialized || stopping)
                {
                throw new InvalidOperationException("Initialize an active worker before registration.");
            }
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            cancellationToken.ThrowIfCancellationRequested();
            using var registration = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token);
            GXAgent agent = new() { Name = name };
            UpdateAgent req = new() { Agents = [agent] };
            if (client.DefaultRequestHeaders.Authorization == null && RuntimeOptions.Token != null)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", RuntimeOptions.Token);
            }
            UpdateAgentResponse? response = await client.PostAsJson<UpdateAgentResponse>("/api/Agent/Add", req, registration.Token);
            if (response == null || response.AgentIds == null || !response.AgentIds.Any())
            {
                return Guid.Empty;
            }
            var id = response.AgentIds.FirstOrDefault();
            if (id != Guid.Empty)
                {
                await PublishIdentityAsync(id: id);
            }
            return id;
        }

        private bool updaterWarningLogged;

        private async Task<GXAgent> ReadRegisteredAgentAsync()
        {
            try
            {
                var response = await client.GetAsJsonAsync<GetAgentResponse>($"/api/Agent/?Id={RuntimeOptions.Id}", cancellationToken);
                return response?.Item ?? throw MissingRegistration();
            }
            catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.NotFound)
            {
                throw MissingRegistration(error);
            }
        }

        private InvalidOperationException MissingRegistration(Exception? inner = null) => new(
            $"Registered agent {RuntimeOptions.Id} was not found or is not accessible on the AMI server. " +
            "The saved registration and PAT have been retained. Check the server address and access rights, " +
            "or run the agent with Configure to register again.", inner);

        private async Task<bool> CheckForApplicationUpdateAsync()
        {
            await updateGate.WaitAsync(cancellationToken);
            try
            {
                if (restartPending)
                    {
                    return true;
                }
                if (completionOutbox?.Status.UnpersistedCount > 0)
                {
                    _logger?.LogWarning("Deferring application update until in-memory task results are persisted");
                    return false;
                }
                var agent = await ReadRegisteredAgentAsync();
                await ApplyAgentOptionsAsync(agent, restartServices: State == GXAgentWorkerState.Running);
                await UpdateAgentSerialPorts(agent.SerialPorts);
                if (string.IsNullOrWhiteSpace(agent.UpdateVersion) || agent.UpdateVersion == RuntimeOptions.Version)
                {
                    if (State == GXAgentWorkerState.Running)
                        {
                        await ConfirmRunningVersionAsync();
                    }
                    return false;
                }
                var updater = _services!.GetRequiredService<IGXAgentUpdateService>();
                if (updater.UnavailableReason is string reason)
                {
                    if (!updaterWarningLogged)
                        {
                        _logger?.LogWarning("Agent update remains pending: {Reason}", reason);
                    }
                    updaterWarningLogged = true;
                    return false;
                }
                try
                {
                    var download = await client.PostAsJson<DownloadAgentResponse>("/api/Agent/Download", new DownloadAgent
                    { Agent = new GXAgent { Id = agent.Id, UpdateVersion = agent.UpdateVersion } }, cancellationToken);
                    await UpdateAgentStatusAsync(AgentStatus.Downloading);
                    await updater.PrepareAsync(agent.UpdateVersion, download?.Urls ?? [], cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch (Exception error)
                {
                    _logger?.LogError(error, "Preparing the application update failed; the current agent keeps running and the requested version remains pending");
                    return false;
                }
                restartPending = true;
                MarkRestartPending();
                _hostNotifications?.NotifyVersionAvailable();
                return true;
            }
            finally { updateGate.Release(); }
        }

        private async System.Threading.Tasks.Task ConfirmRunningVersionAsync()
        {
            var agent = await ReadRegisteredAgentAsync();
            if (agent.Version == RuntimeOptions.Version && agent.UpdateVersion != RuntimeOptions.Version)
                {
                return;
            }
            agent.Version = RuntimeOptions.Version;
            agent.Status = AgentStatus.Detected;
            await UpdateAgentAsync(agent);
        }
        /// <summary>
        /// Get existed tasks.
        /// </summary>
        /// <returns></returns>
        private async System.Threading.Tasks.Task TaskPoller()
        {
            while (!cancellationToken.IsCancellationRequested && !_closing.WaitOne(0))
            {
                try
                {
                    // Initial wake and server events start work promptly; timeout recovers missed events.
                    if (WaitHandle.WaitAny([_newTask, _closing], 10000) == 1)
                    {
                        break;
                    }

                    var updaterOptions = _services?.GetService<GXAgentUpdaterOptions>();
                    if (updaterOptions != null && updaterOptions.RetryAfter != default && updaterOptions.RetryAfter <= DateTimeOffset.UtcNow)
                    {
                        updaterOptions.RetryAfter = default;
                        if (await CheckForApplicationUpdateAsync())
                            {
                            return;
                        }
                    }
                    var options = RuntimeOptions;
                    if (options.ReaderSettings?.Enabled != true)
                    {
                        continue;
                    }

                    var next = await ClaimPolledTasksAsync(cancellationToken);
                    if (next != null)
                    {
                        await QueueMeterReadAsync(next, cancellationToken);
                        _newTask.Set();
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Get next task failed");
                    if (_closing.WaitOne(1000))
                    {
                        break;
                    }

                    _newTask.Set();
                }
            }
        }
        private async System.Threading.Tasks.Task UpdateAgentAsync(GXAgent agent)
        {
            // Status/serial-port reports can advance the concurrency stamp after
            // the update check read this agent. Merge only runtime-owned fields
            // into the latest record, preserving edits and newer update requests.
            var response = await client.GetAsJsonAsync<GetAgentResponse>($"/api/Agent/?Id={agent.Id}", cancellationToken);
            var current = response?.Item ?? throw new GXAmiNotFoundException("Unknown agent.");
            current.Version = agent.Version;
            if (current.UpdateVersion == agent.Version)
                {
                current.UpdateVersion = null;
            }
            current.Status = agent.Status;
            current.Tasks = null;
            current.AgentGroups = null;
            current.Logs = null;
            current.Versions = null;
            UpdateAgent req = new()
            {
                Agents = [current]
            };
            await client.PostAsJson("/api/Agent/Update", req, cancellationToken);
        }

        private async System.Threading.Tasks.Task UpdateAgentStatusAsync(AgentStatus status)
        {
            UpdateAgentStatus req = new()
            {
                Id = RuntimeOptions.Id,
                Status = status
            };
            await client.PostAsJson("/api/Agent/UpdateStatus", req, cancellationToken);
        }

        /// <summary>
        /// Serial ports are added or removed from the agent.
        /// </summary>
        /// <returns></returns>
        private async System.Threading.Tasks.Task UpdateAgentSerialPorts(string? reportedPorts)
        {
            capabilities = new GXAgentCapabilities(GXSerial.GetPortNames());
            string ports = string.Join(";", capabilities.SerialPorts);
            if (!capabilities.SerialPorts.SequenceEqual(new GXAgentCapabilities((reportedPorts ?? "").Split(';')).SerialPorts))
            {
                UpdateAgentStatus req = new()
                {
                    Id = RuntimeOptions.Id,
                    Status = AgentStatus.SerialPortChange,
                    Data = ports
                };
                await client.PostAsJson("/api/Agent/UpdateStatus", req, cancellationToken);
            }
        }


        private async System.Threading.Tasks.Task ExecuteTasks(GXDevice dev, GXDLMSReader reader, GXDLMSSecureClient cl, GXTask task, CancellationToken token)
        {
            try
            {
                await ExecuteTasksCore(dev, reader, cl, task, token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception error)
            {
                GXTaskFailure.Record(task, dev, error.Message);
                await ReportDeviceException(dev, error, "Failed to " + task.TaskType + ". " + GXTaskFailure.Describe(task, dev) + ".");
            }
        }

        private async System.Threading.Tasks.Task ExecuteTasksCore(GXDevice dev, GXDLMSReader reader, GXDLMSSecureClient cl, GXTask task, CancellationToken token)
        {
            GXDLMSObject obj;
            if (task.Object != null && task.Object.Template != null)
            {
                if (task.Object.Template.ObjectType == null)
                {
                    throw new Exception("Invalid object type.");
                }
                obj = GXDLMSClient.CreateObject((ObjectType)task.Object.Template.ObjectType);
                obj.Version = task.Object.Template.Version.GetValueOrDefault(0);
                obj.LogicalName = task.Object.Template.LogicalName;
                obj.ShortName = task.Object.Template.ShortName.GetValueOrDefault();
            }
            else if (task.Attribute != null && task.Attribute.Object != null &&
                task.Attribute.Object.Template != null)
            {
                if (task.Attribute.Object.Template.ObjectType == null)
                {
                    throw new Exception("Invalid object type.");
                }
                obj = GXDLMSClient.CreateObject((ObjectType)task.Attribute.Object.Template.ObjectType);
                // obj.Version = task.Object.Template.Version.GetValueOrDefault(0);
                obj.LogicalName = task.Attribute.Object.Template.LogicalName;
                obj.ShortName = task.Attribute.Object.Template.ShortName.GetValueOrDefault();
            }
            else if (task.Device?.Objects != null)
            {
                foreach (var it in task.Device.Objects)
                {
                    task.Object = it;
                    task.Index = 0;
                    await ExecuteTasks(dev, reader, cl, task, token);
                }
                return;
            }
            else
            {
                throw new ArgumentException($"Invalid object for task {task.Id}: object={task.Object?.Id}, " +
                    $"template={task.Object?.Template?.Id}, attribute={task.Attribute?.Id}, " +
                    $"attributeObject={task.Attribute?.Object?.Id}, attributeTemplate={task.Attribute?.Object?.Template?.Id}, device={task.Device?.Id}.");
            }
            if (task.TaskType == TaskType.Write)
            {
                GXTaskWriteValue.Apply(cl, obj, task.Index.GetValueOrDefault(0), task.Data);
                reader.Write(obj, task.Index.GetValueOrDefault(0));
            }
            else if (task.TaskType == TaskType.Action)
            {
                int index = task.Index.GetValueOrDefault(0);
                object data = GXDLMSTranslator.XmlToValue(task.Data);
                if (obj is GXDLMSImageTransfer it && index == 1 && data is GXStructure d
                    && d.Count == 2 && d[0] is byte[] identifier && d[1] is byte[] image)
                {
                    reader.ImageUpdate(it, identifier, image);
                }
                else
                {
                    reader.Method(obj, index, data, DataType.None);
                }
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
                        task.Index = task.Attribute.Template.Index;
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
                            task.Object.Attributes = [];
                        }
                        task.Object.Attributes.Add(task.Attribute);
                        await Reader.Read(client, _logger, reader, task, obj, token);
                        if (task.Attribute.Template.DataType == 0)
                        {
                            task.Attribute.Template.DataType = (int)obj.GetDataType(task.Index.GetValueOrDefault(0));
                            if (task.Attribute.Template.UIDataType == 0)
                            {
                                task.Attribute.Template.UIDataType = (int)obj.GetUIDataType(task.Index.GetValueOrDefault(0));
                            }
                            UpdateDatatype u = new()
                            {
                                Items = [ new()
                                    {
                                        Id = task.Attribute.Id,
                                        Template = new GXAttributeTemplate()
                                        {
                                            Id = task.Attribute.Template.Id,
                                            DataType = task.Attribute.Template.DataType
                                        }
                                    }
                                ]
                            };
                            await client.PostAsJson("/api/Attribute/UpdateDatatype", u, token);
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
                                if (_logger?.IsEnabled(LogLevel.Information) == true)
                                {
                                    _logger.LogInformation("{Message}", $"Ignoring attribute {attribute.Template.Index}");
                                }
                                continue;
                            }
                            task.Index = attribute.Template.Index;
                            if (attribute.Template.DataType != 0)
                            {
                                obj.SetDataType(attribute.Template.Index, (DataType)attribute.Template.DataType);
                            }
                            if (attribute.Template.UIDataType != 0)
                            {
                                obj.SetUIDataType(attribute.Template.Index, (DataType)attribute.Template.UIDataType);
                            }
                            task.Object = task.Object;
                            await Reader.Read(client, _logger, reader, task, obj, token);
                            if (attribute.Template.DataType == 0)
                            {
                                attribute.Template.DataType = (int)obj.GetDataType(task.Index.GetValueOrDefault(0));
                                if (attribute.Template.UIDataType == 0)
                                {
                                    attribute.Template.UIDataType = (int)obj.GetUIDataType(task.Index.GetValueOrDefault(0));
                                }
                                UpdateDatatype u = new()
                                {
                                    Items = [ new()
                                    {
                                        Id = attribute.Id,
                                        Template = new GXAttributeTemplate()
                                        {
                                            Id = attribute.Template.Id,
                                            DataType = attribute.Template.DataType
                                        }
                                    }
                                ]
                                };
                                await client.PostAsJson("/api/Attribute/UpdateDatatype", u, token);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    GXTaskFailure.Record(task, dev, ex.Message);
                    await ReportDeviceException(dev, ex, "Failed to " + task.TaskType + ". " + GXTaskFailure.Describe(task, dev) + ".");
                }
            }
        }

        internal async Task<GXDevice?> ReadMeter(GXActionBlock action,
            bool autoConnect = false,
            ListenerSettings? listenerSettings = null)
        {
            using var readLifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, action.CancellationToken);
            action.CancellationToken = readLifetime.Token;
            try
            {
                action.CancellationToken.ThrowIfCancellationRequested();
                return await ReadMeterCore(action, autoConnect, listenerSettings);
            }
            catch (OperationCanceledException)
            {

                var pending = action.Tasks.Where(t => t.Ready == null).ToArray();
                foreach (var task in pending)
                    {
                    task.Result = "Read cancelled.";
                }
                try
                { if (pending.Length != 0) {
                    await ReportDone(pending);
                } }
                catch (Exception ex) { _logger?.LogError(ex, "Reporting cancelled tasks failed"); }
                throw;
            }
            catch (Exception ex)
            {
                var pending = action.Tasks.Where(t => t.Ready == null).ToArray();
                foreach (var task in pending)
                    {
                    GXTaskFailure.Record(task, task.Device, ex.Message);
                }
                try
                { if (pending.Length != 0) {
                    await ReportDone(pending);
                } }
                catch (Exception reportError) { _logger?.LogError(reportError, "Reporting failed tasks failed"); }
                _logger?.LogError(ex, "Starting the read operation failed");
                return null;
            }
            finally
            {
                action.NewTask?.Set();
            }
        }
        private async Task<GXDevice?> ReadMeterCore(GXActionBlock action,
            bool autoConnect = false,
            ListenerSettings? listenerSettings = null)
        {
            var options = RuntimeOptions;
            GXDLMSSecureClient cl;
            IGXMedia? media = action.Media;
            int pos;
            GXDLMSReader? reader = null;
            IEnumerable<GXTask> tasks = action.Tasks;
            GXTask firstTask = tasks.First();
            GXDevice dev;
            GetDeviceResponse? ret;
            try
            {
                ret = await client.GetAsJsonAsync<GetDeviceResponse>(string.Format("/api/Device/?Id={0}&includeKeys=true", firstTask.TargetDevice), cancellationToken: action.CancellationToken);
                dev = ret.Item ?? throw new KeyNotFoundException("Device was not returned.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                //Remove all tasks if meter reading fails.
                foreach (GXTask task in tasks)
                {
                    GXTaskFailure.Record(task, task.Device, ex.Message);
                }
                await ReportDone(tasks);
                if (_logger?.IsEnabled(LogLevel.Error) == true)
                {
                    _logger.LogError("{Message}", DateTime.Now + "\t" + ex.Message);
                }
                action.NewTask?.Set();
                return null;
            }
            bool ownsMedia = media == null;
            var session = new MeterTaskSession();
            session.Add(tasks);
            action.Tasks = session.Tasks();
            bool registered = false;
            try
            {
                try
                {
                    registered = await RegisterMeterSessionAsync(dev.Id, session, action.CancellationToken);
                    if (!registered)
                    {
                        return dev;
                    }
                    if (media == null)
                    {
                        if (string.IsNullOrEmpty(dev.MediaType))
                        {
                            throw new Exception("Device media type is missing.");
                        }
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
                            Type? type = Type.GetType(dev.MediaType);
                            if (type == null)
                            {
                                string ns = "";
                                pos = dev.MediaType.LastIndexOf('.');
                                if (pos != -1)
                                {
                                    ns = dev.MediaType[..pos];
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
                            serial.PortName = options.SerialPort;
                        }
                        media.Open();
                    }
                    var settings = JsonSerializer.Deserialize<AMI.Shared.DTOs.Device.GXDLMSSettings>(
                        dev.Settings ?? throw new Exception("Device settings are missing.")) ?? throw new Exception("Failed to serialize settings.");
                    var templateSettings = JsonSerializer.Deserialize<AMI.Shared.DTOs.Device.GXDLMSSettings>(
                        dev.Template?.Settings ?? throw new Exception("Device template settings are missing.")) ?? throw new Exception("Failed to serialize template settings.");
                    //Use interface type that is defined for agent.
                    if (listenerSettings != null && settings.InterfaceType != listenerSettings.Interface)
                    {
                        foreach (var it in templateSettings.Profiles)
                        {
                            if (it.InterfaceType == listenerSettings.Interface)
                            {
                                settings.InterfaceType = it.InterfaceType;
                                templateSettings.ClientAddress = settings.ClientAddress = it.ClientAddress;
                                settings.LogicalAddress = it.LogicalAddress;
                                if (settings.HDLCAddressing != (int)ManufacturerSettings.HDLCAddressType.SerialNumber)
                                {
                                    settings.PhysicalAddress = it.PhysicalAddress;
                                }
                                else if (settings.HDLCAddressing == (int)ManufacturerSettings.HDLCAddressType.SerialNumber &&
                                    settings.PhysicalAddress == 0)
                                {
                                    //If serial number is not given.
                                    settings.HDLCAddressing = (int)ManufacturerSettings.HDLCAddressType.Default;
                                    settings.PhysicalAddress = it.PhysicalAddress;
                                }
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (var it in templateSettings.Profiles)
                        {
                            if (it.InterfaceType == settings.InterfaceType)
                            {
                                templateSettings.ClientAddress = settings.ClientAddress = it.ClientAddress;
                                settings.LogicalAddress = it.LogicalAddress;
                                if (settings.HDLCAddressing != (int)ManufacturerSettings.HDLCAddressType.SerialNumber)
                                {
                                    settings.PhysicalAddress = it.PhysicalAddress;
                                }
                                else if (settings.HDLCAddressing == (int)ManufacturerSettings.HDLCAddressType.SerialNumber &&
                                    settings.PhysicalAddress == 0)
                                {
                                    //If serial number is not given.
                                    settings.HDLCAddressing = (int)ManufacturerSettings.HDLCAddressType.Default;
                                    settings.PhysicalAddress = it.PhysicalAddress;
                                }
                                break;
                            }
                        }
                    }
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
                            deviceAddress = GXDLMSClient.GetServerAddress(settings.LogicalAddress, (int)settings.PhysicalAddress);
                        }
                        else
                        {
                            deviceAddress = (int)settings.PhysicalAddress;
                        }
                    }
                    TraceLevel consoleTrace = options.TraceLevel;
                    TraceLevel deviceTrace = dev.TraceLevel.GetValueOrDefault(TraceLevel.Off);
                    cl = new GXDLMSSecureClient(templateSettings.UseLogicalNameReferencing, templateSettings.ClientAddress, deviceAddress,
                        (Authentication)templateSettings.Authentication, null, (InterfaceType)settings.InterfaceType);
                    if (cl.InterfaceType == InterfaceType.HDLC || cl.InterfaceType == InterfaceType.HdlcWithModeE)
                    {
                        cl.HdlcSettings.MaxInfoTX = templateSettings.MaxInfoTX;
                        cl.HdlcSettings.MaxInfoRX = templateSettings.MaxInfoRX;
                        cl.HdlcSettings.WindowSizeRX = templateSettings.WindowSizeRX;
                        cl.HdlcSettings.WindowSizeTX = templateSettings.WindowSizeTX;
                    }
                    if (settings.HDLCAddressing == (int)ManufacturerSettings.HDLCAddressType.SerialNumber)
                    {
                        cl.ServerAddressSize = 4;
                    }
                    GXDLMSReader.ApplyDeviceKeys(cl, settings);
                    cl.UseUtc2NormalTime = templateSettings.UtcTimeZone;
                    cl.Standard = (Standard)templateSettings.Standard;
                    cl.Ciphering.SystemTitle = settings.ClientSystemTitle;
                    if (cl.Ciphering.SystemTitle != null && cl.Ciphering.SystemTitle.Length == 0)
                    {
                        cl.Ciphering.SystemTitle = null;
                    }
                    if (cl.Ciphering.BlockCipherKey != null && cl.Ciphering.BlockCipherKey.Length == 0)
                    {
                        cl.Ciphering.BlockCipherKey = null;
                    }
                    if (cl.Ciphering.AuthenticationKey != null && cl.Ciphering.AuthenticationKey.Length == 0)
                    {
                        cl.Ciphering.AuthenticationKey = null;
                    }
                    cl.ServerSystemTitle = settings.DeviceSystemTitle;
                    if (cl.ServerSystemTitle != null && cl.ServerSystemTitle.Length == 0)
                    {
                        cl.ServerSystemTitle = null;
                    }
                    cl.Ciphering.InvocationCounter = settings.InvocationCounter;
                    cl.Ciphering.Security = (Security)templateSettings.Security;
                    cl.Ciphering.SecuritySuite = (Gurux.DLMS.Objects.Enums.SecuritySuite)templateSettings.SecuritySuite;
                    cl.Ciphering.Signing = (Gurux.DLMS.Enums.Signing)templateSettings.Signing;
                    reader = new GXDLMSReader(cl, media, _logger,
                        consoleTrace, deviceTrace, dev.WaitTime, dev.ResendCount, dev, client)
                    { CancellationToken = action.CancellationToken };
                    await reader.InitializeConnection(settings.PreEstablished, settings.IgnoreSNRMWithPreEstablished,
                        settings.FrameCounter);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    var failed = await SealMeterSessionAsync(session);
                    foreach (GXTask task in failed)
                    {
                        GXTaskFailure.Record(task, dev, "Failed to establish the connection. " + ex.Message);
                    }
                    await ReportDone(failed);
                    if (dev != null)
                    {
                        await ReportDeviceException(dev, ex, "Failed to establish the connection.");
                    }
                    return dev;
                }
                while (session.Pending.TryDequeue(out var task))
                {
                    action.CancellationToken.ThrowIfCancellationRequested();
                    try
                    { await ExecuteTasks(dev, reader, cl, task, action.CancellationToken); }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception error)
                    {
                        GXTaskFailure.Record(task, dev, error.Message);
                        await ReportDeviceException(dev, error, "Failed to execute task " + task);
                    }
                    task.Ready = DateTimeOffset.UtcNow;
                    await ReportDone([task]);
                    await CheckConnectedMeterTasksAsync(dev.Id, session, action, listenerSettings != null);
                }
            }
            finally
            {
                try
                {
                    if (ownsMedia && media != null)
                    {
                        try
                        {
                            if (reader != null)
                                {
                                reader.Close();
                            }
                            else
                                {
                                media.Close();
                            }
                        }
                        finally { (media as IDisposable)?.Dispose(); }
                    }
                }
                finally
                {
                    if (registered)
                        {
                        await UnregisterMeterSessionAsync(dev.Id, session);
                    }
                    action.NewTask?.Set();
                }
            }
            return dev;
        }

        /// <summary>
        /// Report task exception.
        /// </summary>
        /// <param name="dev">Executed device.</param>
        /// <param name="ex">Exception.</param>
        /// <param name="message">Exception message.</param>
        private async System.Threading.Tasks.Task ReportDeviceException(
            GXDevice dev,
            Exception ex,
            string message)
        {
            AddLog error = new()
            {
                Type = TraceLevel.Verbose.ToString(),
                Logs = [
                    new GXLog(TraceLevel.Error)
                            {
                    //Only device ID is needed to report error.
                    //Other properties are not needed and can cause problems if they are not correct.
                                Creator = dev.Id,
                                Message = message + " " + ex.Message
                            } ]
            };
            if (_logger?.IsEnabled(LogLevel.Error) == true)
            {
                _logger.LogError("{Message}", DateTime.Now + "\t" + error.Logs.FirstOrDefault()?.Message);
            }
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await client.PostAsJson("/api/Log/Add?source=device-error", error, deadline.Token);
        }

        /// <summary>
        /// Report that task is done.
        /// </summary>
        /// <param name="tasks">Executed tasks.</param>
        private System.Threading.Tasks.Task ReportDone(IEnumerable<GXTask> tasks) => EnsureCompletionOutbox().ReportAsync(tasks);

        internal bool CanClaimTasks => EnsureCompletionOutbox().CanAcceptTasks;

        private GXTaskCompletionOutbox EnsureCompletionOutbox()
        {
            lock (lifetime)
            {
                if (completionOutbox != null)
                    {
                    return completionOutbox;
                }
                var options = RuntimeOptions;
                if (options.Id == Guid.Empty)
                    {
                    throw new InvalidOperationException("Register the agent before reporting task completions.");
                }
                string server = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(options.Address)))[..16];
                string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(root))
                    {
                    root = AppContext.BaseDirectory;
                }
                completionOutbox = new GXTaskCompletionOutbox(client,
                    Path.Combine(root, "Gurux", "DLMS.AMI", "task-completions", server, options.Id.ToString("N")),
                    _logger, TimeSpan.FromSeconds(2), _services?.GetService<GXCompletionOutboxOptions>());
                return completionOutbox;
            }
        }
        /// <inheritdoc />
        private async System.Threading.Tasks.Task StartCoreAsync()
        {
            // A queued update must be applied before opening services or accepting work.
            if (await CheckForApplicationUpdateAsync())
                {
                return;
            }
            //Create GXNet and GXDate so they are found from the assembly.
            _ = new GXNet();
            _ = new GXDate();
            Assembly asm = typeof(GXAgentWorker).Assembly;
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(asm.Location);
            if (_logger?.IsEnabled(LogLevel.Information) == true)
            {
                _logger.LogInformation("{Message}", string.Format("Starting Gurux.DLMS.Agent.Worker version {0}", string.Join(";", info.FileVersion)));
            }
            Uri uri = new(new Uri(RuntimeOptions.Address), "/guruxami");
            _hubConnection = new HubConnectionBuilder()
            .WithUrl(uri, o =>
            {
                o.HttpMessageHandlerFactory = handler => new GXAuthenticationHandler(new GXMaintenanceHandler(handler), authentication);
                o.AccessTokenProvider = async () =>
                {
                    return RuntimeOptions.Token;
                };
            })
            .Build();

            _hubConnection.On<IEnumerable<GXScript>>("ScriptUpdate", async scripts =>
            {
                foreach (var id in scripts.Select(s => s.Id).Where(id => id != Guid.Empty).Distinct())
                {
                    await PrepareScriptUpdateAsync(id);
                }
            });
            _hubConnection.On<Guid, string[]?>("ClearCache", (id, names) =>
            {
                _logger?.LogInformation("Agent cache cleared.");
            });

            _hubConnection.On<IEnumerable<GXTarget>>(nameof(IGXHubEvents.OnUpdate), async updates =>
            {
                var batch = updates.ToArray();
                if (batch.Any(u => u.Type == "agent.update" && u.Id == RuntimeOptions.Id.ToString()) &&
                    await CheckForApplicationUpdateAsync())
                    {
                    return;
                }
                Notification.Publish(batch);
                foreach (var update in batch.Where(u => u.Type == "script.edit"))
                {
                    if (Guid.TryParse(update.Id, out var scriptId))
                    {
                        await PrepareScriptUpdateAsync(scriptId);
                    }
                }

                if (batch.Any(u => u.Type == "agent.edit" && u.Id == RuntimeOptions.Id.ToString()))
                {
                    var response = await client.GetAsJsonAsync<GetAgentResponse>($"/api/Agent/?Id={RuntimeOptions.Id}", cancellationToken);
                    if (response?.Item != null)
                    {
                        await ApplyAgentUpdatesAsync([response.Item]);
                    }
                }
                if (batch.Any(u => u.Type.StartsWith("agent-group.", StringComparison.Ordinal)))
                {
                    _newTask.Set();
                    _services?.GetRequiredService<GXRawNotificationService>().Signal();
                }
            });
            _hubConnection.On<IEnumerable<GXAgent>>("AgentUpdate", ApplyAgentUpdatesAsync);
            _hubConnection.On<IEnumerable<GXAgentGroup>>("AgentGroupUpdate", (groups) =>
            {
                //Read tasks if agent mapping is changed.
                _newTask.Set();
            });
            _hubConnection.Reconnecting += HubConnectionReconnecting;
            _hubConnection.Reconnected += HubConnectionReconnected;
            _hubConnection.Closed += HubConnectionClosed;
            SetConnectionState(GXAgentConnectionState.Connecting);
            await _hubConnection.StartAsync(cancellationToken);
            await _hubConnection.InvokeAsync("RegisterAgent", RuntimeOptions.Id, cancellationToken);
            SetConnectionState(GXAgentConnectionState.Connected);
            _logger?.LogInformation("Waiting tasks from the Gurux.DLMS.AMI.");
            //Check new versions to install.
            if (await CheckForApplicationUpdateAsync())
                {
                return;
            }
            GXNotifyService? ns = _services?.GetRequiredService<GXNotifyService>();
            if (ns != null)
            {
                await ns.StartAsync(cancellationToken);
            }

            GXAutoConnectService? ac = _services?.GetRequiredService<GXAutoConnectService>();
            if (ac != null)
            {
                await ac.StartAsync(cancellationToken);
            }

            GXGatewayService? gw = _services?.GetRequiredService<GXGatewayService>();
            if (gw != null)
            {
                await gw.StartAsync(cancellationToken);
            }

            var rawService = _services?.GetRequiredService<GXRawNotificationService>();
            if (rawService != null)
            {
                await rawService.StartAsync(cancellationToken);
            }

            await ConfirmRunningVersionAsync();
            taskPoller = System.Threading.Tasks.Task.Run(TaskPoller);
            _logger?.LogInformation("Agent started");
        }

        private async System.Threading.Tasks.Task ApplyAgentUpdatesAsync(IEnumerable<GXAgent> agents)
        {
            foreach (var agent in agents)
            {
                if (agent.Id != RuntimeOptions.Id)
                    {
                    continue;
                }
                await ApplyAgentOptionsAsync(agent, restartServices: true);
                _newTask.Set();
            }
        }
        private async System.Threading.Tasks.Task HubConnectionClosed(Exception? error)
        {
            SetConnectionState(GXAgentConnectionState.Reconnecting);
            Exception? lastError = error;
            bool maintenanceRetry = error is GXMaintenanceException;
            while (!cancellationToken.IsCancellationRequested && !_closing.WaitOne(0))
            {
                var hub = _hubConnection;
                if (hub == null)
                {
                    return;
                }

                try
                {
                    if (lastError is GXMaintenanceException maintenance)
                    {
                        await GXMaintenanceRetry.WaitAsync(maintenance, cancellationToken);
                    }
                    else
                    {
                        await System.Threading.Tasks.Task.Delay(maintenanceRetry ? 60000 : 1000, cancellationToken);
                    }
                    await hub.StartAsync(cancellationToken);
                    await HubConnectionReconnected(hub.ConnectionId);
                    return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception reconnectError)
                {
                    lastError = reconnectError;
                    maintenanceRetry |= reconnectError is GXMaintenanceException;
                    _logger?.LogWarning(reconnectError, "Reconnecting to Gurux.DLMS.AMI failed; retrying");
                }
            }
        }
        private async System.Threading.Tasks.Task PrepareScriptUpdateAsync(Guid scriptId)
        {
            try
            {
                await Scripts.RefreshAsync(scriptId, cancellationToken);
                if (_logger?.IsEnabled(LogLevel.Information) == true)
                {
                    _logger.LogInformation("Script {Script} prepared for new connections", scriptId);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception error)
            {
                _logger?.LogError(error, "Script {Script} update was rejected; existing connections keep their version", scriptId);
            }
        }

        private async System.Threading.Tasks.Task HubConnectionReconnected(string? arg)
        {
            await _hubConnection!.InvokeAsync("RegisterAgent", RuntimeOptions.Id, cancellationToken);
            SetConnectionState(GXAgentConnectionState.Connected);
            if (await CheckForApplicationUpdateAsync())
                {
                return;
            }
            _logger?.LogInformation("Reconnected for the Gurux.DLMS.AMI.");
            _newTask.Set();
            _services?.GetRequiredService<GXRawNotificationService>().Signal();
            foreach (var scriptId in Scripts.ScriptIds)
            {
                await PrepareScriptUpdateAsync(scriptId);
            }
        }
        private System.Threading.Tasks.Task HubConnectionReconnecting(Exception? arg)
        {
            SetConnectionState(GXAgentConnectionState.Reconnecting);
            _logger?.LogInformation("Reconnecting for the Gurux.DLMS.AMI.");
            return System.Threading.Tasks.Task.CompletedTask;
        }

        /// <inheritdoc />
        private async System.Threading.Tasks.Task StopCoreAsync()
        {
            _logger?.LogInformation("Stopping Gurux.DLMS.Agent.Worker.");
            _closing.Set();
            _newTask.Set();
            _taskNotification?.Close();
            taskSubscription?.Dispose();
            taskSubscription = null;
            // Let in-flight settings/update callbacks leave their critical sections before
            // stopping their services or releasing synchronization resources.
            await settingsGate.WaitAsync();
            settingsGate.Release();
            await updateGate.WaitAsync();
            updateGate.Release();
            // The host controls its shutdown deadline. Cleanup remains alive if the caller stops waiting.
            try
            {
                if (_services != null)
                {
                    await StopServiceAsync(() => _services.GetRequiredService<GXRawNotificationService>().StopAsync(CancellationToken.None), "raw parser");
                    await StopServiceAsync(() => _services.GetRequiredService<GXNotifyService>().StopAsync(CancellationToken.None), "notification receiver");
                    await StopServiceAsync(() => _services.GetRequiredService<GXAutoConnectService>().StopAsync(CancellationToken.None), "auto connect");
                    await StopServiceAsync(() => _services.GetRequiredService<GXGatewayService>().StopAsync(CancellationToken.None), "gateway");
                }
            }
            finally
            {
                lifetime.Cancel();
                if (taskPoller != null)
                {
                    await StopServiceAsync(() => taskPoller, "task poller");
                }

                _meterReads.Complete();
                await StopServiceAsync(() => _meterReads.Completion, "meter reads");

                if (completionOutbox != null)
                {
                    await StopServiceAsync(async () =>
                    {
                        try
                        {
                            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                            await completionOutbox.FlushAsync(deadline.Token);
                        }
                        finally { await completionOutbox.DisposeAsync(); }
                    }, "task completions");
                }
                Scripts.Clear();
                var hub = _hubConnection;
                _hubConnection = null;
                if (hub != null)
                {
                    await StopServiceAsync(() => hub.DisposeAsync().AsTask(), "server notifications");
                }
                if (completionOutbox?.Status.UnpersistedCount > 0)
                    {
                    throw new IOException("Cannot complete shutdown safely while task results remain only in memory.");
                }
            }
        }

        private async System.Threading.Tasks.Task StopServiceAsync(Func<System.Threading.Tasks.Task> stop, string name)
        {
            try
            {
                await stop();
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
            {
                // Normal lifetime cancellation is expected during shutdown.
            }
            catch (Exception error)
            {
                _logger?.LogWarning(error, "Could not fully stop {Service}", name);
            }
        }
    }
}
