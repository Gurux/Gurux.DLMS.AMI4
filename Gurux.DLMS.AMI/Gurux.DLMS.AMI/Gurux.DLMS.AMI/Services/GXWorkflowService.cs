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

using Gurux.DLMS.AMI.Client.Shared;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Components;
using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Script;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Triggers;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Data.Odbc;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Gurux.DLMS.AMI.Server.Services
{
    /// <summary>
    /// Action info is used to save detailed information from the invoked action.
    /// </summary>
    class GXActionInfo
    {
        /// <summary>
        /// Trigger type.
        /// </summary>
        public required Type Trigger;
        /// <summary>
        /// Activity.
        /// </summary>
        public required string Activity;
        /// <summary>
        /// Invoker.
        /// </summary>
        public required object? Sender;
        public Guid? WorkflowId;
        public Guid EventId = Guid.NewGuid();
        public DateTimeOffset QueuedAt = DateTimeOffset.UtcNow;
        public string? ActorId;
        public bool Manual;
        public GXWorkflowEventContext? EventContext;
        public Guid ChainId;
        public int Depth;
        public Guid DeliveryId;
    }

    /// <summary>
    /// GXWorkflowService handles workflows.
    /// </summary>
    internal sealed partial class GXWorkflowService : BackgroundService, IWorkflowHandler
    {
        internal readonly List<GXWorkflow> Workflows = [];

        /// <summary>
        /// List of actions.
        /// </summary>
        internal readonly List<GXActionInfo> Actions = [];
        private readonly SemaphoreSlim signal = new(0);
        private readonly IServiceProvider _serviceProvider;
        /// <summary>
        /// Script are keeped loaded to improve the performance.
        /// </summary>
        private readonly Dictionary<Guid, AssemblyLoadContext?> LoadedScripts = [];

        public GXWorkflowService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

        }

        /// <inheritdoc />
        public void Add(GXWorkflow item)
        {
            lock (Workflows)
            {
                Workflows.RemoveAll(w => w.Id == item.Id);
                Workflows.Add(item);
            }
        }

        /// <inheritdoc />
        public void Delete(GXWorkflow item)
        {
            lock (Workflows)
            {
                Workflows.RemoveAll(w => w.Id == item.Id);
            }
        }

        /// <inheritdoc />
        public void Update(object source)
        {
            lock (Workflows)
            {
                foreach (var it in Workflows)
                {
                    if (source is GXScript script && it.ScriptMethods != null && script.Methods != null)
                    {
                        var comparer2 = new GXScriptMethodComparer();
                        List<GXScriptMethod> removedScriptMethods = [.. it.ScriptMethods.Except(script.Methods, comparer2)];
                        List<GXScriptMethod> addedScriptMethods = [.. script.Methods.Except(it.ScriptMethods, comparer2)];
                        List<GXScriptMethod> updatedScriptMethods = [.. script.Methods.Union(it.ScriptMethods, comparer2)];
                        if (removedScriptMethods.Count != 0 || addedScriptMethods.Count != 0 || updatedScriptMethods.Count != 0)
                        {
                            it.ScriptMethods.Clear();
                            UnloadScript(script.Id);
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        public void Execute(Type trigger, string activity, object? Sender, ClaimsPrincipal? actor = null)
            // Synchronous publishers can run under Blazor's synchronization context.
            => Task.Run(() => ExecuteAsync(trigger, activity, Sender, actor)).GetAwaiter().GetResult();

        /// <inheritdoc />
        public async Task ExecuteAsync(Type trigger, string activity, object? source, ClaimsPrincipal? actor = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var originator = actor ?? _serviceProvider?.GetService<IHttpContextAccessor>()?.HttpContext?.User;
            var actorId = originator?.Identity?.IsAuthenticated == true ? GXWorkflowIdentityAccess.ActorId(originator) : null;
            var context = source as GXWorkflowEventContext;
            var eventId = context?.EventId ?? Guid.NewGuid();
            if (await EnqueueDurableAsync(new GXActionInfo
            {
                Trigger = trigger,
                Activity = activity,
                Sender = source,
                ActorId = actorId,
                EventContext = context,
                EventId = eventId
            }, cancellationToken))
                {
                return;
            }
            GXWorkflow[] matches;
            lock (Workflows)
                matches = Workflows.Where(w => w.Enabled == true && w.Removed == null && w.Phase == TriggerPhase.After
                    && w.TriggerActivity?.SupportedPhases.HasFlag(TriggerPhase.After) == true
                    && w.TriggerActivity.Name == activity && w.TriggerActivity.Trigger?.ClassName == trigger.FullName).ToArray();
            lock (Actions)
                foreach (var workflow in matches)
                {
                    Actions.Add(new GXActionInfo
                    {
                        Trigger = trigger,
                        Activity = activity,
                        Sender = source,
                        WorkflowId = workflow.Id,
                        EventId = eventId,
                        ActorId = actorId,
                        EventContext = context
                    });
                    signal.Release();
                }
        }

        /// <inheritdoc />
        public void Publish(GXWorkflowEventContext context, ClaimsPrincipal? actor = null)
        {
            ArgumentNullException.ThrowIfNull(context);
            if (!GXTriggerEventCatalog.TryResolveEvent(context.EventCode, out var trigger, out var activity))
                {
                throw new ArgumentException("Unknown or unregistered workflow event.", nameof(context));
            }
            // Snapshot mutable publisher data and detach JSON values from disposable JsonDocuments.
            var snapshot = JsonSerializer.Deserialize<GXWorkflowEventContext>(JsonSerializer.Serialize(context))!;
            if (snapshot.EventId == Guid.Empty)
                {
                snapshot.EventId = Guid.NewGuid();
            }
            if (snapshot.OperationId == Guid.Empty)
                {
                snapshot.OperationId = snapshot.EventId;
            }
            if (Server.Repository.GXHistoryScope.Defer(() => Execute(trigger, activity, snapshot, actor))) {
                return;
            }
            Execute(trigger, activity, snapshot, actor);
        }
        public void ExecuteWorkflow(Guid workflowId, ClaimsPrincipal actor)
        {
            if (workflowId == Guid.Empty || actor.Identity?.IsAuthenticated != true || GXWorkflowIdentityAccess.ActorId(actor) == null)
                {
                throw new UnauthorizedAccessException("A manual workflow invocation requires an authenticated actor.");
            }
            if (EnqueueDurable(new GXActionInfo
            {
                Trigger = typeof(GXWorkflow),
                Activity = "Manual",
                Sender = actor,
                WorkflowId = workflowId,
                ActorId = GXWorkflowIdentityAccess.ActorId(actor),
                Manual = true
            }))
                {
                return;
            }
            lock (Actions)
            {
                Actions.Add(new GXActionInfo
                {
                    Trigger = typeof(GXWorkflow),
                    Activity = "Manual",
                    Sender = actor,
                    WorkflowId = workflowId,
                    ActorId = GXWorkflowIdentityAccess.ActorId(actor),
                    Manual = true
                });
                signal.Release();
            }
        }

        /// <summary>Consumes a queued invocation, retaining initiator and manual-run metadata.</summary>
        private GXActionInfo GetNextInvocation()
        {
            signal.Wait();
            lock (Actions)
            {
                var action = Actions[0];
                Actions.RemoveAt(0);
                return action;
            }
        }

        /// <summary>Returns a queued workflow snapshot for callers inspecting the queue.</summary>
        /// <param name="sender">Original event source.</param>
        public GXWorkflow? GetNextItemAsync(out object? sender)
        {
            var action = GetNextInvocation();
            sender = action.Sender;
            lock (Workflows)
                return Workflows.FirstOrDefault(w => w.Id == action.WorkflowId);
        }
        /// <summary>
        /// Wait for workflow events and execute them.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = _serviceProvider.GetRequiredService<IDbConnectionFactory>();
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                { if (await InitializeAsync(stoppingToken)) {
                    break;
                } }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                catch (Exception error) { _serviceProvider.GetRequiredService<ILogger<GXWorkflowService>>().LogWarning(error, "Workflow initialization will retry."); }
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            ClaimsPrincipal admin;
            await using (var connection = await factory.GetConnectionAsync(stoppingToken))
                admin = (await ServerSettings.GetDefaultAdminUserAsync(connection)) ?? throw new InvalidOperationException("The workflow audit account is unavailable.");
            await _serviceProvider.RunAsUserAsync(admin, async auditServices =>
            {
                Execute(typeof(ServiceTrigger), ServiceTrigger.Start, null, new ClaimsPrincipal());
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await ResumeSourcePublicationsAsync(factory, stoppingToken);
                        await FanOutAsync(factory, stoppingToken);
                        var delivery = await LeaseNextAsync(factory, stoppingToken);
                        if (delivery == null)
                        {
                            await signal.WaitAsync(TimeSpan.FromSeconds(1), stoppingToken);
                            continue;
                        }
                        using var lease = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                        var heartbeat = HeartbeatAsync(factory, delivery, lease.Token);
                        _ = heartbeat.ContinueWith(t => { if (t.IsFaulted) {
                            lease.Cancel();
                        } }, CancellationToken.None,
                            TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                        try
                        {
                            GXWorkflowExecution history;
                            GXActionInfo? invocation = null;
                            try
                            { invocation = DeserializeInvocation(delivery); }
                            catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or ArgumentException)
                            {
                                delivery.ErrorCode = "invalid-delivery";
                                delivery.ErrorMessage = error.Message;
                            }
                            if (invocation == null)
                                {
                                history = new GXWorkflowExecution
                                {
                                    ExecutionId = Guid.NewGuid(),
                                    Status = WorkflowExecutionStatus.Failed,
                                    Message = delivery.ErrorMessage,
                                    StartedAt = DateTimeOffset.UtcNow,
                                    CompletedAt = DateTimeOffset.UtcNow
                                };
                            }
                            else
                            {
                                invocation.DeliveryId = delivery.Id;
                                history = await ExecuteInvocationCoreAsync(invocation, auditServices, lease.Token);
                            }
                            lease.Cancel();
                            try
                            { await heartbeat; }
                            catch (OperationCanceledException) { }
                            await CompleteDeliveryAsync(factory, delivery, history, stoppingToken.IsCancellationRequested || heartbeat.IsFaulted, CancellationToken.None);
                        }
                        finally
                        {
                            lease.Cancel();
                            try
                            { await heartbeat; }
                            catch (OperationCanceledException) { }
                        }
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                    catch (Exception error)
                    {
                        _serviceProvider.GetRequiredService<ILogger<GXWorkflowService>>().LogError(error, "Workflow delivery will retry after its lease expires.");
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    }
                }
            }, stoppingToken);
        }

        private async Task<bool> InitializeAsync(CancellationToken stoppingToken)
        {
            var dbFactory = _serviceProvider.GetRequiredService<IDbConnectionFactory>();
            using var connection = await dbFactory.GetConnectionAsync();
            ClaimsPrincipal? cp = (await ServerSettings.GetDefaultAdminUserAsync(connection));
            if (cp == null)
            {
                //Default admin user is not yet set.
                return false;
            }
            var arg = GXSelectArgs.Select<GXRole>(s => s.Id, w => w.Name == "UserAction");
            if (connection.SingleOrDefault<GXRole>(arg) != null)
            {
                //Role names are changed to kebab case.
                var roles = connection.SelectAll<GXRole>();
                foreach (var it in roles)
                {
                    it.Name = ClientHelpers.GetKebabCase(it.Name);
                    connection.Update(GXUpdateArgs.Update(it, c => c.Name!));
                }
            }
            await _serviceProvider.RunAsUserAsync(cp, async adminServices =>
            {
                CryptoOptions? signingKey = _serviceProvider.GetRequiredService<IOptions<CryptoOptions>>().Value;
                if (signingKey?.Key == "47757275784C746447757275784C746447757275784C746447757275784C7464")
                {
                    var log = new GXLog(TraceLevel.Warning)
                    {
                        CreationTime = DateTime.Now,
                        Message = "Change the default signing key to a custom one.",
                    };
                    var repository = adminServices.GetRequiredService<ILogRepository>().ForSource("system");
                    await repository.AddAsync(TargetType.System, [log]);
                }

                //Create localized resources.
                arg = GXSelectArgs.IsEmpty<GXLocalizedResource>();
                if (connection.SingleOrDefault<bool>(arg))
                {
                    var adminUser = (await ServerSettings.GetDefaultAdminUserAsync(connection));
                    var creator = new GXUser() { Id = ClientHelpers.GetUserId(adminUser) };
                    var enumTypeRepository = adminServices.GetRequiredService<IEnumTypeRepository>();
                    var languages = connection.SelectAll<GXLanguage>();
                    await ServerHelpers.UpdateLanguageResourcesAsync(connection,
                        enumTypeRepository, languages, creator);
                }

                // Discovery is internal; execution authorization is checked for the actual execution user below.
                var discovery = GXSelectArgs.SelectAll<GXWorkflow>(w => w.Enabled == true && w.Removed == null);
                discovery.Columns.Exclude<GXWorkflow>(w => w.Properties!);
                discovery.Columns.Add<GXTriggerActivity>();
                discovery.Columns.Add<GXTrigger>();
                discovery.Columns.Exclude<GXTrigger>(t => t.Activities!);
                discovery.Joins.AddLeftJoin<GXWorkflow, GXTriggerActivity>(w => w.TriggerActivity!, a => a.Id);
                discovery.Joins.AddLeftJoin<GXTriggerActivity, GXTrigger>(a => a.Trigger!, t => t.Id);
                lock (Workflows)
                {
                    Workflows.Clear();
                    Workflows.AddRange(connection.Select<GXWorkflow>(discovery));
                }
            }, stoppingToken);
            return true;
        }
        /// <summary>Resolves current identity and permissions before executing any workflow code.</summary>
        internal Task<GXWorkflowExecution> ExecuteInvocationAsync(GXActionInfo invocation, IServiceProvider auditServices)
            => ExecuteInvocationCoreAsync(invocation, auditServices, CancellationToken.None);

        private async Task<GXWorkflowExecution> ExecuteInvocationCoreAsync(GXActionInfo invocation, IServiceProvider auditServices, CancellationToken stoppingToken)
        {
            var history = new GXWorkflowExecution
            {
                ExecutionId = Guid.NewGuid(),
                EventId = invocation.EventId,
                WorkflowId = invocation.WorkflowId ?? Guid.Empty,
                TriggerCode = invocation.Trigger.FullName,
                Activity = invocation.Activity,
                ActorId = invocation.ActorId,
                EventCode = invocation.EventContext?.EventCode,
                OperationId = invocation.EventContext?.OperationId,
                ChainId = invocation.ChainId == Guid.Empty ? invocation.EventId : invocation.ChainId,
                Depth = invocation.Depth,
                TargetId = invocation.EventContext?.TargetId ?? invocation.EventContext?.DeviceId?.ToString(),
                TargetType = invocation.EventContext?.AttributeId != null ? "Attribute"
                    : invocation.EventContext?.DeviceId != null ? "Device" : invocation.EventContext?.EventCode.Split('.')[0],
                QueuedAt = invocation.QueuedAt,
                StartedAt = DateTimeOffset.UtcNow,
                Status = WorkflowExecutionStatus.Running
            };
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(60));
            var executionToken = deadline.Token;
            var elapsed = Stopwatch.StartNew();
            var execution = Task.Run(async () =>
            {
                var previous = Current.Value;
                Current.Value = new InvocationScope(history.ChainId, invocation.Depth, invocation.DeliveryId, invocation.ActorId, executionToken);
                try
                {
                    var factory = _serviceProvider.GetRequiredService<IDbConnectionFactory>();
                    await using var connection = await factory.GetConnectionAsync(executionToken);
                    var snapshotQuery = GXSelectArgs.SelectAll<GXWorkflow>(w => w.Id == history.WorkflowId);
                    snapshotQuery.Columns.Exclude<GXWorkflow>(w => w.Properties!);
                    var snapshot = await connection.SingleOrDefaultAsync<GXWorkflow>(snapshotQuery, executionToken);
                    if (snapshot == null || snapshot.Removed != null || snapshot.Enabled != true || snapshot.Phase != TriggerPhase.After)
                        {
                        throw new InvalidOperationException("The workflow is disabled, removed or has an unsupported phase.");
                    }
                    history.WorkflowName = snapshot.Name;
                    history.WorkflowVersion = snapshot.ConcurrencyStamp;
                    history.Phase = snapshot.Phase;
                    var principal = await GXWorkflowIdentityAccess.ResolveAsync(connection, snapshot, invocation.ActorId, invocation.Manual, executionToken);
                    history.ExecutionUserId = GXWorkflowIdentityAccess.ActorId(principal);
                    await _serviceProvider.RunAsUserAsync(principal, async services =>
                    {
                        executionToken.ThrowIfCancellationRequested();
                        var authorization = services.GetRequiredService<IAuthorizationService>();
                        await authorization.EnsureAnyPolicyAsync(principal, GXWorkflowPolicies.Run);
                        var repository = services.GetRequiredService<IWorkflowRepository>();
                        var workflow = await repository.ReadAsync(snapshot.Id, true, cancellationToken: executionToken);
                        int timeout = ExecutionSetting(workflow, "execution.timeout-seconds", 60, 3600);
                        deadline.CancelAfter(TimeSpan.FromMilliseconds(Math.Max(1, timeout * 1000 - elapsed.ElapsedMilliseconds)));
                        if (invocation.Depth > ExecutionSetting(workflow, "execution.max-depth", 16, 64))
                        {
                            history.Status = WorkflowExecutionStatus.Skipped;
                            history.Message = "The workflow event chain exceeded this workflow's maximum depth.";
                            return;
                        }
                        if (workflow.Enabled != true || workflow.Removed != null || workflow.ExecutionIdentity != snapshot.ExecutionIdentity ||
                            workflow.ExecutionUserId != snapshot.ExecutionUserId || workflow.Phase != TriggerPhase.After)
                            {
                            throw new InvalidOperationException("The workflow changed while execution was starting.");
                        }
                        if (!invocation.Manual && (workflow.TriggerActivity?.Name != invocation.Activity ||
                            workflow.TriggerActivity.Trigger?.ClassName != invocation.Trigger.FullName))
                            {
                            throw new InvalidOperationException("The workflow no longer handles this event.");
                        }
                        if (workflow.TriggerActivity != null && !workflow.TriggerActivity.SupportedPhases.HasFlag(TriggerPhase.After))
                            {
                            throw new InvalidOperationException("The trigger activity no longer supports this phase.");
                        }
                        if (invocation.EventContext is { } eventContext)
                        {
                            if (!await GXWorkflowEventFilter.MatchesAsync(connection, workflow, eventContext, executionToken))
                            {
                                history.Status = WorkflowExecutionStatus.Skipped;
                                return;
                            }
                            await GXWorkflowEventFilter.EnsureEventAccessAsync(connection, authorization, principal, eventContext, executionToken);
                            await GXWorkflowEventAccess.EnsureAsync(connection, authorization, principal, eventContext, executionToken);
                        }
                        else if (!invocation.Manual && GXWorkflowEventFilter.HasFilters(workflow))
                        {
                            history.Status = WorkflowExecutionStatus.Skipped;
                            return;
                        }
                        var settings = invocation.EventContext?.EventCode == "device.delete"
                            ? new GXWorkflow { Properties = workflow.Properties?.Where(p => p.Module != null || p.Name != "event.device").ToList() } : workflow;
                        await GXWorkflowEventFilter.ValidateAsync(connection, authorization, principal, settings, executionToken);
                        await GXAutomationPolicyAccess.EnsureWorkflowReferencesAsync(connection, authorization, principal, workflow, executionToken);
                        if (workflow.TriggerMethod != null)
                        {
                            var condition = await RunWorkflowMethodAsync(connection, services, principal, workflow.TriggerMethod.Id, invocation.Sender, executionToken);
                            executionToken.ThrowIfCancellationRequested();
                            if (condition is not bool matches)
                                {
                                throw new InvalidOperationException("A workflow condition must return a Boolean result.");
                            }
                            history.ConditionMatched = matches;
                            if (!matches)
                            { history.Status = WorkflowExecutionStatus.Skipped; return; }
                        }
                        foreach (var method in workflow.ScriptMethods ?? [])
                        {
                            executionToken.ThrowIfCancellationRequested();
                            await RunWorkflowMethodAsync(connection, services, principal, method.Id, invocation.Sender, executionToken);
                        }
                        executionToken.ThrowIfCancellationRequested();
                        history.Status = WorkflowExecutionStatus.Succeeded;
                    }, executionToken);
                }
                finally { Current.Value = previous; }
            }, CancellationToken.None);
            try
            { await execution.WaitAsync(executionToken); }
            catch (OperationCanceledException) when (executionToken.IsCancellationRequested)
            {
                history.Status = stoppingToken.IsCancellationRequested ? WorkflowExecutionStatus.Cancelled : WorkflowExecutionStatus.TimedOut;
                history.Message = stoppingToken.IsCancellationRequested ? "Workflow execution interrupted by shutdown or lease loss." : "Workflow execution exceeded its time limit.";
                // The underlying task owns its scope until it really exits; noncooperative scripts cannot be forcibly killed.
                _ = execution.ContinueWith(t => { _ = t.Exception; }, CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
            catch (Exception error)
            {
                history.Status = WorkflowExecutionStatus.Failed;
                history.Message = error.Message;
            }
            history.CompletedAt = DateTimeOffset.UtcNow;
            var log = new GXLog(history.Status is WorkflowExecutionStatus.Failed or WorkflowExecutionStatus.TimedOut ? TraceLevel.Error : TraceLevel.Info)
            {
                Creator = history.WorkflowId,
                ExecutionId = history.ExecutionId,
                User = history.ExecutionUserId == null ? null : new GXUser { Id = history.ExecutionUserId },
                Message = history.Message ?? $"Workflow execution {history.Status}.",
                Data = JsonSerializer.Serialize(history),
                Duration = (int)Math.Min(int.MaxValue, (history.CompletedAt.Value - history.StartedAt!.Value).TotalMilliseconds)
            };
            try
            { await auditServices.GetRequiredService<ILogRepository>().ForSource("workflow").AddAsync("Execution", [log]); }
            catch (Exception error) { _serviceProvider.GetRequiredService<ILogger<GXWorkflowService>>().LogError(error, "Unable to persist workflow execution {ExecutionId}.", history.ExecutionId); }
            return history;
        }

        private static async Task<object?> RunWorkflowMethodAsync(GXDbConnection connection, IServiceProvider services,
            ClaimsPrincipal principal, Guid methodId, object? sender, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var query = GXSelectArgs.SelectAll<GXScriptMethod>(m => m.Id == methodId);
            query.Columns.Add<GXScript>(s => new { s.Id, s.ByteAssembly, s.Enabled, s.Removed });
            query.Joins.AddInnerJoin<GXScriptMethod, GXScript>(m => m.Script!, s => s.Id);
            var method = await connection.SingleOrDefaultAsync<GXScriptMethod>(query);
            if (method?.Script?.ByteAssembly == null || method.Script.Enabled != true || method.Script.Removed != null)
                {
                throw new InvalidOperationException("The workflow script is disabled, removed or not compiled.");
            }
            await services.GetRequiredService<IAuthorizationService>().EnsureAnyPolicyAsync(principal, GXScriptPolicies.Run);
            await GXAutomationPolicyAccess.EnsureAsync(connection, services.GetRequiredService<IAuthorizationService>(),
                principal, typeof(GXScript), method.Script.Id);
            var script = new GXAmiScript(services)
            {
                Sender = sender,
                User = new GXUser { Id = GXWorkflowIdentityAccess.ActorId(principal) }
            };
            var args = new GXScriptRunArgs
            {
                ByteAssembly = method.Script.ByteAssembly,
                MethodName = method.Name ?? throw new InvalidOperationException("The workflow script method has no name."),
                Asyncronous = method.IsAsynchronous
            };
            try
            {
                return await script.RunAsync(args, cancellationToken);
            }
            finally { args.AssemblyLoadContext?.Unload(); }
        }
        /// <inheritdoc />
        public void AddScript(Guid id, AssemblyLoadContext alc)
        {
            LoadedScripts[id] = alc;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UnloadScript(Guid id)
        {
            lock (Workflows)
            {
                if (LoadedScripts.TryGetValue(id, out var alc))
                {
                    foreach (var it in Workflows)
                    {
                        foreach (var method in (it.ScriptMethods ?? []))
                        {
                            if (method.Script != null && method.Script.Id == id)
                            {
                                it.ScriptMethods?.Clear();
                                break;
                            }
                        }
                    }
                }
                LoadedScripts.Remove(id);
                if (alc != null)
                {
                    WeakReference alcWeakRef = new(alc);
                    alc.Unload();
                    for (int i = 0; alcWeakRef.IsAlive && (i < 10); i++)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }
            }
        }

        /// <inheritdoc />
        public AssemblyLoadContext? GetScript(Guid id)
        {
            return LoadedScripts[id];
        }
    }
}
