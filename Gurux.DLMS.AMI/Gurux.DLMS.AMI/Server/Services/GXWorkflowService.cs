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
using Gurux.DLMS.AMI.Script;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Server.Triggers;
using Gurux.DLMS.AMI.Shared.DTOs;
using System.Reflection;
using Gurux.DLMS.AMI.Server.Internal;
using System.Runtime.Loader;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Diagnostics;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.DTOs.Script;

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
        public Type Trigger;
        /// <summary>
        /// Activity.
        /// </summary>
        public string Activity;
        /// <summary>
        /// Invoker.
        /// </summary>
        public object Sender;
    }

    /// <summary>
    /// GXWorkflowService handles workflows.
    /// </summary>
    internal sealed class GXWorkflowService : IWorkflowHandler
    {
        internal readonly List<GXWorkflow> Workflows = new List<GXWorkflow>();

        /// <summary>
        /// List of actions.
        /// </summary>
        internal readonly List<GXActionInfo> Actions = new();
        private static SemaphoreSlim signal = new SemaphoreSlim(0);
        private readonly IServiceProvider _serviceProvider;
        /// <summary>
        /// Script are keeped loaded to improve the performance.
        /// </summary>
        private Dictionary<Guid, AssemblyLoadContext?> LoadedScripts = new Dictionary<Guid, AssemblyLoadContext?>();

        public GXWorkflowService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            Thread t = new Thread(() => WorkflowJobHandler());
            t.Start();
        }

        /// <inheritdoc />
        public void Add(GXWorkflow item)
        {
            lock (Workflows)
            {
                Workflows.Add(item);
            }
        }

        /// <inheritdoc />
        public void Delete(GXWorkflow item)
        {
            lock (Workflows)
            {
                Workflows.Remove(item);
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
                        List<GXScriptMethod> removedScriptMethods = it.ScriptMethods.Except(script.Methods, comparer2).ToList();
                        List<GXScriptMethod> addedScriptMethods = script.Methods.Except(it.ScriptMethods, comparer2).ToList();
                        List<GXScriptMethod> updatedScriptMethods = script.Methods.Union(it.ScriptMethods, comparer2).ToList();
                        if (removedScriptMethods.Any() || addedScriptMethods.Any() || updatedScriptMethods.Any())
                        {
                            it.ScriptMethods.Clear();
                            UnloadScript(script.Id);
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        public void Execute(Type trigger, string activity, object? Sender)
        {
            lock (Actions)
            {
                Actions.Add(new GXActionInfo() { Trigger = trigger, Activity = activity, Sender = Sender });
                signal.Release(1);
            }
        }

        /// <summary>
        /// Get next workflow item to execute.
        /// </summary>
        /// <param name="sender">Invoker.</param>
        /// <returns>Executed workflow item.</returns>
        public GXWorkflow? GetNextItemAsync(out object? sender)
        {
            signal.Wait();
            lock (Actions)
            {
                lock (Workflows)
                {
                    while (Actions.Count != 0)
                    {
                        var action = Actions[0];
                        Actions.RemoveAt(0);
                        foreach (var wf in Workflows)
                        {
                            if (wf.TriggerActivity != null &&
                                wf.TriggerActivity.Trigger != null &&
                                wf.TriggerActivity.Name == action.Activity &&
                                wf.TriggerActivity.Trigger.ClassName == action.Trigger.FullName)
                            {
                                sender = action.Sender;
                                return wf;
                            }
                        }
                    }
                }
            }
            sender = null;
            return null;
        }

        /// <summary>
        /// Wait for workflow events and execute them.
        /// </summary>
        private async void WorkflowJobHandler()
        {
            ClaimsPrincipal? cp = null;
            Execute(typeof(ServiceTrigger), ServiceTrigger.Start, null);
            GXWorkflow[] items = new GXWorkflow[0];
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                IGXHost host = scope.ServiceProvider.GetRequiredService<IGXHost>();
                cp = ServerSettings.GetDefaultAdminUser(host);
                ListWorkflows req = new ListWorkflows() { Filter = new GXWorkflow() { Active = true } };
                IWorkflowRepository workflowRepository = scope.ServiceProvider.GetRequiredService<IWorkflowRepository>();
                if (cp != null)
                {
                    items = await workflowRepository.ListAsync(cp, req, null, true);
                }
            }
            lock (Workflows)
            {
                Workflows.Clear();
                Workflows.AddRange(items);
            }
            while (true)
            {
                GXWorkflow? item = GetNextItemAsync(out object? sender);
                if (item != null)
                {
                    var user = ServerHelpers.CreateClaimsPrincipalFromUser(item.Creator);
                    //Execute workflow scripts.
                    try
                    {
                        if (item.ScriptMethods == null || item.ScriptMethods.Count == 0)
                        {
                            //ScriptMethods are null for the first time or if the script is updated.
                            using IServiceScope scope = _serviceProvider.CreateScope();
                            IWorkflowRepository workflowRepository = scope.ServiceProvider.GetRequiredService<IWorkflowRepository>();
                            GXWorkflow tmp = await workflowRepository.ReadAsync(user, item.Id, true);
                            item.ScriptMethods = tmp.ScriptMethods;
                        }
                        foreach (GXScriptMethod method in item.ScriptMethods)
                        {
                            try
                            {
                                if (method.Script == null)
                                {
                                    continue;
                                }
                                if (!method.Script.Active.HasValue || !method.Script.Active.Value)
                                {
                                    //If script is not active.
                                    continue;
                                }
                                if (method.Script.ByteAssembly == null)
                                {
                                    throw new Exception(string.Format("Script {0} is not compiled.", method.Name));
                                }
                                GXAmiScript tmp = new GXAmiScript(_serviceProvider);
                                tmp.Sender = sender;
                                AssemblyLoadContext? loadedScript = null;
                                LoadedScripts.TryGetValue(method.Script.Id, out loadedScript);
                                GXScriptRunArgs args;
                                args = new GXScriptRunArgs()
                                {
                                    ByteAssembly = method.Script.ByteAssembly,
                                    MethodName = method.Name,
                                    Asyncronous = method.Asyncronous,
                                    AssemblyLoadContext = loadedScript
                                };
                                await tmp.RunAsync(args);
                                LoadedScripts[method.Script.Id] = args.AssemblyLoadContext;
                            }
                            catch (TargetInvocationException ex)
                            {
                                UnloadScript(method.Script.Id);
                                GXWorkflowLog error = new GXWorkflowLog(TraceLevel.Error);
                                error.Workflow = item;
                                if (ex.InnerException != null)
                                {
                                    error.Message = ex.InnerException.Message;
                                }
                                else
                                {
                                    error.Message = ex.Message;
                                }
                                using (IServiceScope scope = _serviceProvider.CreateScope())
                                {
                                    IWorkflowLogRepository repository = scope.ServiceProvider.GetRequiredService<IWorkflowLogRepository>();
                                    await repository.AddAsync(user, new GXWorkflowLog[] { error });
                                }
                            }
                            catch (Exception ex)
                            {
                                UnloadScript(method.Script.Id);
                                GXWorkflowLog error = new GXWorkflowLog(TraceLevel.Error);
                                error.Workflow = item;
                                error.Message = ex.Message;
                                if (user == null)
                                {
                                    user = cp;
                                }
                                using (IServiceScope scope = _serviceProvider.CreateScope())
                                {
                                    IWorkflowLogRepository repository = scope.ServiceProvider.GetRequiredService<IWorkflowLogRepository>();
                                    await repository.AddAsync(user, new GXWorkflowLog[] { error });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        GXSystemLog error = new GXSystemLog();
                        error.Message = ex.Message;
                        using (IServiceScope scope = _serviceProvider.CreateScope())
                        {
                            ISystemLogRepository systemErrorRepository = scope.ServiceProvider.GetRequiredService<ISystemLogRepository>();
                            if (user == null)
                            {
                                user = cp;
                            }
                            await systemErrorRepository.AddAsync(user, new GXSystemLog[] { error });
                        }
                    }
                }
            }
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
                        foreach (var method in it.ScriptMethods)
                        {
                            if (method.Script != null && method.Script.Id == id)
                            {
                                it.ScriptMethods.Clear();
                                break;
                            }
                        }
                    }
                }
                LoadedScripts.Remove(id);
                if (alc != null)
                {
                    WeakReference alcWeakRef = new WeakReference(alc);
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
