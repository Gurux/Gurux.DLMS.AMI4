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

using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Threading;

namespace Gurux.DLMS.AMI.Script
{
    /// <summary>
    /// This class implements Roslyn scripting engine.
    /// </summary>
    /// <remarks>
    /// Constructor.
    /// </remarks>
    public class GXAmiScript(IServiceProvider serviceProvider) : IGXAmi
    {
        object? _sender;
        private readonly IServiceProvider? _serviceProvider = serviceProvider;

        /// <inheritdoc/>
        public object? Sender
        {
            get
            {
                return _sender;
            }
            set
            {
                _sender = value;
                if (_sender is GXUser u)
                {
                    User = u;
                }
            }
        }

        /// <inheritdoc />
        public GXUser? User { get; set; }

        /// <inheritdoc />
        public GXDeviceTemplate? DefaultDeviceTemplate { get; set; }

        /// <inheritdoc />
        public async Task AddAsync(object value, CancellationToken cancellationToken)
        {
            if (_serviceProvider == null)
            {
                throw new ArgumentException(nameof(_serviceProvider));
            }
            using IServiceScope scope = _serviceProvider.CreateScope();
            if (value is GXLog se)
            {
                ILogRepository repository = scope.ServiceProvider.GetRequiredService<ILogRepository>();
                await repository.AddAsync("Script", [se], cancellationToken);
            }

            else if (value is GXDeviceGroup dg)
            {
                IDeviceGroupRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceGroupRepository>();
                await repository.UpdateAsync([dg], null, cancellationToken);
            }
            else if (value is GXDevice d)
            {
                await AddDeviceAsync(d, false, cancellationToken);
            }
            else if (value is GXObject o)
            {
                IObjectRepository repository = scope.ServiceProvider.GetRequiredService<IObjectRepository>();
                await repository.UpdateAsync([o], null, cancellationToken);
            }
            else if (value is GXValue v)
            {
                IValueRepository repository = scope.ServiceProvider.GetRequiredService<IValueRepository>();
                await repository.AddAsync([v], cancellationToken);
            }
            else if (value is GXTask t)
            {
                ITaskRepository repository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
                t.Id = (await repository.AddAsync([t], cancellationToken)).FirstOrDefault();
            }

            else if (value is GXDeviceTrace dt)
            {
                IDeviceTraceRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceTraceRepository>();
                await repository.AddAsync("Script", [dt], cancellationToken);
            }
            else if (value is GXAgentGroup ag)
            {
                IAgentGroupRepository repository = scope.ServiceProvider.GetRequiredService<IAgentGroupRepository>();
                await repository.UpdateAsync([ag], null, cancellationToken);
            }
            else if (value is GXAgent a)
            {
                IAgentRepository repository = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
                await repository.UpdateAsync([a], null, cancellationToken);
            }
            else if (value is GXUserGroup ug)
            {
                IUserGroupRepository repository = scope.ServiceProvider.GetRequiredService<IUserGroupRepository>();
                await repository.UpdateAsync([ug], null, cancellationToken);
            }
            else if (value is GXUser u)
            {
                IUserRepository repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                await repository.UpdateAsync([u], null, cancellationToken);
            }
            else if (value is GXScheduleGroup sg)
            {
                IScheduleGroupRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleGroupRepository>();
                await repository.UpdateAsync([sg], null, cancellationToken);
            }
            else if (value is GXSchedule s)
            {
                IScheduleRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
                await repository.UpdateAsync([s], null, cancellationToken);
            }


            else if (value is IEnumerable<GXLog> seList)
            {
                ILogRepository repository = scope.ServiceProvider.GetRequiredService<ILogRepository>();
                await repository.AddAsync("Script", seList, cancellationToken);
            }

            else if (value is IEnumerable<GXDeviceGroup> dgList)
            {
                IDeviceGroupRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceGroupRepository>();
                var ret = await repository.UpdateAsync(dgList, null, cancellationToken);
                for (int pos = 0; pos < ret.Count(); ++pos)
                {
                    dgList.ElementAt(pos).Id = ret.ElementAt(pos);
                }
            }
            else if (value is IEnumerable<GXDevice> dList)
            {
                IDeviceRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                var ret = await repository.UpdateAsync(dList, default);
                for (int pos = 0; pos < ret.Count(); ++pos)
                {
                    dList.ElementAt(pos).Id = ret.ElementAt(pos);
                }
            }
            else if (value is IEnumerable<GXObject> oList)
            {
                IObjectRepository repository = scope.ServiceProvider.GetRequiredService<IObjectRepository>();
                await repository.UpdateAsync(oList, null, cancellationToken);
            }
            else if (value is IEnumerable<GXValue> vList)
            {
                IValueRepository repository = scope.ServiceProvider.GetRequiredService<IValueRepository>();
                await repository.AddAsync(vList, cancellationToken);
            }
            else if (value is IEnumerable<GXTask> tList)
            {
                ITaskRepository repository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
                await repository.AddAsync(tList, cancellationToken);
            }

            else if (value is IEnumerable<GXDeviceTrace> dtList)
            {
                IDeviceTraceRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceTraceRepository>();
                await repository.AddAsync("Script", dtList, cancellationToken);
            }
            else if (value is IEnumerable<GXAgentGroup> agList)
            {
                IAgentGroupRepository repository = scope.ServiceProvider.GetRequiredService<IAgentGroupRepository>();
                await repository.UpdateAsync(agList, null, cancellationToken);
            }
            else if (value is IEnumerable<GXAgent> aList)
            {
                IAgentRepository repository = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
                await repository.UpdateAsync(aList, null, cancellationToken);
            }
            else if (value is IEnumerable<GXUserGroup> ugList)
            {
                IUserGroupRepository repository = scope.ServiceProvider.GetRequiredService<IUserGroupRepository>();
                await repository.UpdateAsync(ugList, null, cancellationToken);
            }
            else if (value is IEnumerable<GXUser> uList)
            {
                IUserRepository repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                await repository.UpdateAsync(uList, null, cancellationToken);
            }
            else if (value is IEnumerable<GXScheduleGroup> sgList)
            {
                IScheduleGroupRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleGroupRepository>();
                await repository.UpdateAsync(sgList, null, cancellationToken);
            }
            else if (value is IEnumerable<GXSchedule> sList)
            {
                IScheduleRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
                await repository.UpdateAsync(sList, null, cancellationToken);
            }


            else if (value is IEnumerable<GXGatewayGroup> gwgList)
            {
                IGatewayGroupRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayGroupRepository>();
                var ret = await repository.UpdateAsync(gwgList, null, cancellationToken);
                for (int pos = 0; pos < ret.Count(); ++pos)
                {
                    gwgList.ElementAt(pos).Id = ret.ElementAt(pos);
                }
            }
            else if (value is IEnumerable<GXGateway> gwList)
            {
                if (Sender is GXAgent agent)
                {
                    foreach (var gw in gwList)
                    {
                        gw.Agent = new GXAgent() { Id = agent.Id };
                    }
                }
                IGatewayRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayRepository>();
                var ret = await repository.UpdateAsync(gwList, null, cancellationToken);
                for (int pos = 0; pos < ret.Count(); ++pos)
                {
                    gwList.ElementAt(pos).Id = ret.ElementAt(pos);
                }
            }
            else if (value is GXGateway gw)
            {
                if (Sender is GXAgent agent)
                {
                    gw.Agent = new GXAgent() { Id = agent.Id };
                }
                IGatewayRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayRepository>();
                gw.Id = (await repository.UpdateAsync([gw], null, cancellationToken)).First();
            }

            else
            {
                throw new ArgumentException("Add script failed. Unknown target.");
            }
        }

        /// <inheritdoc />
        public async Task UpdateAsync(object value, CancellationToken cancellationToken)
        {
            await AddAsync(value, cancellationToken);
        }

        /// <inheritdoc />
        public async Task RemoveAsync(object value, bool delete, CancellationToken cancellationToken)
        {
            if (_serviceProvider == null)
            {
                throw new ArgumentException(nameof(_serviceProvider));
            }
            using IServiceScope scope = _serviceProvider.CreateScope();
            if (value is GXDeviceGroup dg)
            {
                IDeviceGroupRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceGroupRepository>();
                await repository.DeleteAsync([dg.Id], delete, true, cancellationToken);
            }
            else if (value is GXDevice d)
            {
                IDeviceRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                await repository.DeleteAsync([d.Id], delete, true, cancellationToken);
            }
            else if (value is GXObject o)
            {
                IObjectRepository repository = scope.ServiceProvider.GetRequiredService<IObjectRepository>();
                await repository.DeleteAsync([o.Id], delete, true, cancellationToken);
            }
            else if (value is GXTask t)
            {
                ITaskRepository repository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
                await repository.DeleteAsync([t.Id], cancellationToken);
            }
            else if (value is GXAgentGroup ag)
            {
                IAgentGroupRepository repository = scope.ServiceProvider.GetRequiredService<IAgentGroupRepository>();
                await repository.DeleteAsync([ag.Id], delete, true, cancellationToken);
            }
            else if (value is GXAgent a)
            {
                IAgentRepository repository = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
                await repository.DeleteAsync([a.Id], delete, true, cancellationToken);
            }
            else if (value is GXUserGroup ug)
            {
                IUserGroupRepository repository = scope.ServiceProvider.GetRequiredService<IUserGroupRepository>();
                await repository.DeleteAsync([ug.Id], delete, true, cancellationToken);
            }
            else if (value is GXUser u)
            {
                IUserRepository repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                await repository.DeleteAsync([u.Id!], delete, true, cancellationToken);
            }
            else if (value is GXScheduleGroup sg)
            {
                IScheduleGroupRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleGroupRepository>();
                await repository.DeleteAsync([sg.Id], delete, true, cancellationToken);
            }
            else if (value is GXSchedule s)
            {
                IScheduleRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
                await repository.DeleteAsync([s.Id], delete, true, cancellationToken);
            }
            else if (value is GXLog ue)
            {
                ILogRepository repository = scope.ServiceProvider.GetRequiredService<ILogRepository>();
                await repository.CloseAsync([ue.PublicId], cancellationToken);
            }
            else if (value is IEnumerable<GXDeviceGroup> dgList)
            {
                IDeviceGroupRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceGroupRepository>();
                await repository.DeleteAsync(dgList.Select(s => s.Id), delete, true, cancellationToken);
            }
            else if (value is IEnumerable<GXDevice> dList)
            {
                IDeviceRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                await repository.DeleteAsync(dList.Select(s => s.Id), delete, true, cancellationToken);
            }
            else if (value is IEnumerable<GXObject> oList)
            {
                IObjectRepository repository = scope.ServiceProvider.GetRequiredService<IObjectRepository>();
                await repository.DeleteAsync(oList.Select(s => s.Id), delete, true, cancellationToken);
            }
            else if (value is IEnumerable<GXTask> tList)
            {
                ITaskRepository repository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
                await repository.DeleteAsync(tList.Select(s => s.Id), cancellationToken);
            }
            else if (value is IEnumerable<GXAgentGroup> agList)
            {
                IAgentGroupRepository repository = scope.ServiceProvider.GetRequiredService<IAgentGroupRepository>();
                await repository.DeleteAsync(agList.Select(s => s.Id), delete, true, cancellationToken);
            }
            else if (value is IEnumerable<GXAgent> aList)
            {
                IAgentRepository repository = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
                await repository.DeleteAsync(aList.Select(s => s.Id), delete, cancellationToken: cancellationToken);
            }
            else if (value is IEnumerable<GXUserGroup> ugList)
            {
                IUserGroupRepository repository = scope.ServiceProvider.GetRequiredService<IUserGroupRepository>();
                await repository.DeleteAsync(ugList.Select(s => s.Id!), delete, true, cancellationToken);
            }
            else if (value is IEnumerable<GXUser> uList)
            {
                IUserRepository repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                await repository.DeleteAsync(uList.Select(s => s.Id!), delete, true, cancellationToken);
            }
            else if (value is IEnumerable<GXScheduleGroup> sgList)
            {
                IScheduleGroupRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleGroupRepository>();
                await repository.DeleteAsync(sgList.Select(s => s.Id), delete, true, cancellationToken);
            }
            else if (value is IEnumerable<GXSchedule> sList)
            {
                IScheduleRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
                await repository.DeleteAsync(sList.Select(s => s.Id), delete, true, cancellationToken);
            }
            else if (value is IEnumerable<GXLog> ueList)
            {
                ILogRepository repository = scope.ServiceProvider.GetRequiredService<ILogRepository>();
                await repository.CloseAsync(ueList.Select(s => s.PublicId), cancellationToken);
            }
            else if (value is IEnumerable<GXGatewayGroup> gwgList)
            {
                IGatewayGroupRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayGroupRepository>();
                await repository.DeleteAsync(gwgList.Select(s => s.Id), delete, true, cancellationToken);
            }
            else if (value is IEnumerable<GXGateway> gwList)
            {
                IGatewayRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayRepository>();
                await repository.DeleteAsync(gwList.Select(s => s.Id), delete, true, cancellationToken);
            }

            else
            {
                throw new ArgumentException("Remove script failed. Unknown target.");
            }
        }

        /// <summary>
        /// This method generates byte assembly from the given script.
        /// </summary>
        /// <param name="scriptLanguage">Script language.</param>
        /// <param name="fileName">File name.</param>
        /// <param name="script">The source code of the script.</param>
        /// <param name="errors">Errors of the source code.</param>
        /// <param name="methods">Methods of the source code.</param>
        /// <returns>(COFF)-based image containing an emitted assembly.</returns>
        public static byte[]? Generate(
            ScriptLanguage scriptLanguage,
            string fileName,
            string script,
            List<GXScriptException> errors,
            List<MethodDeclarationSyntax>? methods)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(script);
            List<MetadataReference> references = [];
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
            }
            Compilation compilation;
            if (scriptLanguage == ScriptLanguage.CSharp)
            {
                compilation = CSharpCompilation.Create(fileName,
                 syntaxTrees: [syntaxTree],
                 references: references,
                 options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            }
            else
            {
                compilation = VisualBasicCompilation.Create(fileName,
                 syntaxTrees: [syntaxTree],
                 references: references,
                 options: new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            }
            using MemoryStream ms = new();
            EmitResult result = compilation.Emit(ms);
            if (!result.Success)
            {
                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                     diagnostic.IsWarningAsError ||
                     diagnostic.Severity == DiagnosticSeverity.Error);
                foreach (Diagnostic diagnostic in failures.OrderBy(o => o.Location.GetLineSpan().StartLinePosition.Line))
                {
                    GXScriptException error = new()
                    {
                        Line = diagnostic.Location.GetLineSpan().StartLinePosition.Line,
                        Id = diagnostic.Id,
                        Message = diagnostic.GetMessage()
                    };
                    errors.Add(error);
                }
                return null;
            }

            if (methods != null)
            {
                // Getting the root node of the file.
                methods.Clear();
                var rootSyntaxNode = syntaxTree.GetRootAsync().Result;
                //Add all public methods that are not static.
                methods.AddRange(rootSyntaxNode.DescendantNodesAndSelf().OfType<MethodDeclarationSyntax>());
            }
            ms.Seek(0, SeekOrigin.Begin);
            return ms.ToArray();
        }

        /// <summary>
        /// This method creates a instance from the byte assembly and runs the given method and returns the result of the script.
        /// </summary>
        /// <param name="args">Script run arguments.</param>
        /// <returns>The return value of the invoked method.</returns>
        public async Task<object?> RunAsync(GXScriptRunArgs args)
        {
            Assembly? asm = null;
            if (args.AssemblyLoadContext != null)
            {
                asm = args.AssemblyLoadContext.Assemblies.FirstOrDefault();
            }
            if (asm == null)
            {
                try
                {
                    args.AssemblyLoadContext = new AssemblyLoadContext("Gurux.DLMS.AMI.GeneratedScript", true);
                    using var ms = new MemoryStream();
                    ms.Write(args.ByteAssembly);
                    ms.Position = 0;
                    asm = args.AssemblyLoadContext.LoadFromStream(ms);
                }
                catch (Exception)
                {
                    throw;
                }
            }
            Type? amiMacroType = asm.GetType("Gurux.DLMS.AMI.GeneratedScript.GXAmiScript");
            if (amiMacroType == null || amiMacroType.FullName == null)
            {
                throw new ArgumentException("Failed to load the AMI script type.");
            }
            var instance = asm.CreateInstance(amiMacroType.FullName, false, BindingFlags.Instance | BindingFlags.Public, null, [this], null, null);
            MethodInfo? entryPoint = amiMacroType.GetMethod(args.MethodName) ?? throw new ArgumentException(string.Format("Invalid method name {0}.", args.MethodName));
            try
            {
                if (args.Asyncronous)
                {
                    return await Task.Run(() =>
                    {
                        return entryPoint.Invoke(instance, args.Parameters);
                    });
                }
                return entryPoint.Invoke(instance, args.Parameters);
            }
            catch (Exception ex)
            {
                if (ex.InnerException == null)
                {
                    throw;
                }
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                return null;
            }
        }

        /// <inheritdoc />
        public object? GetService(Type type)
        {
            if (_serviceProvider == null)
            {
                throw new ArgumentException(nameof(_serviceProvider));
            }
            return _serviceProvider.GetService(type);
        }

        /// <inheritdoc />
        public T? GetService<T>()
        {
            if (_serviceProvider == null)
            {
                throw new ArgumentException(nameof(_serviceProvider));
            }
            return (T?)_serviceProvider.GetService(typeof(T));
        }

        /// <inheritdoc />
        public IEnumerable<T> Select<T>(T filter)
        {
            return SelectAsync(filter).Result;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<T>> SelectAsync<T>(T filter, CancellationToken cancellationToken = default)
        {
            if (_serviceProvider == null)
            {
                throw new ArgumentException(nameof(_serviceProvider));
            }
            using IServiceScope scope = _serviceProvider.CreateScope();
            if (typeof(T) == typeof(GXLog))
            {
                ILogRepository repository = scope.ServiceProvider.GetRequiredService<ILogRepository>();
                ListLogs request = new()
                {
                    Filter = filter as GXLog
                };
                return (IEnumerable<T>)(object)(await repository.ListAsync(request, cancellationToken: cancellationToken));
            }

            else if (typeof(T) == typeof(GXDeviceGroup))
            {
                IDeviceGroupRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceGroupRepository>();
                ListDeviceGroups request = new()
                {
                    Filter = filter as GXDeviceGroup
                };
                return (IEnumerable<T>)(object)(await repository.ListAsync(request, cancellationToken: cancellationToken));
            }
            else if (typeof(T) == typeof(GXDevice))
            {
                IDeviceRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                ListDevices request = new()
                {
                    Filter = filter as GXDevice
                };
                return (IEnumerable<T>)(object)(await repository.ListAsync(request, cancellationToken: cancellationToken));
            }
            else if (typeof(T) == typeof(GXObject))
            {
                IObjectRepository repository = scope.ServiceProvider.GetRequiredService<IObjectRepository>();
                ListObjects request = new()
                {
                    Filter = filter as GXObject
                };
                return (IEnumerable<T>)(object)(await repository.ListAsync(request, cancellationToken: cancellationToken));
            }
            else if (typeof(T) == typeof(GXTask))
            {
                ITaskRepository repository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
                ListTasks request = new()
                {
                    Filter = filter as GXTask
                };
                return (IEnumerable<T>)(object)(await repository.ListAsync(request, cancellationToken: cancellationToken));
            }

            else if (typeof(T) == typeof(GXAgentGroup))
            {
                IAgentGroupRepository repository = scope.ServiceProvider.GetRequiredService<IAgentGroupRepository>();
                ListAgentGroups request = new()
                {
                    Filter = filter as GXAgentGroup
                };
                return (IEnumerable<T>)(object)(await repository.ListAsync(request, cancellationToken: cancellationToken));
            }
            else if (typeof(T) == typeof(GXAgent))
            {
                IAgentRepository repository = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
                ListAgents request = new()
                {
                    Filter = filter as GXAgent
                };
                return (IEnumerable<T>)(object)(await repository.ListAsync(request, cancellationToken: cancellationToken));
            }
            else if (typeof(T) == typeof(GXUserGroup))
            {
                IUserGroupRepository repository = scope.ServiceProvider.GetRequiredService<IUserGroupRepository>();
                ListUserGroups request = new()
                {
                    Filter = filter as GXUserGroup
                };
                return (IEnumerable<T>)(object)(await repository.ListAsync(request, cancellationToken: cancellationToken));
            }
            else if (typeof(T) == typeof(GXUser))
            {
                IUserRepository repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                ListUsers request = new()
                {
                    Filter = filter as GXUser
                };
                return (IEnumerable<T>)(object)(await repository.ListAsync(request, cancellationToken: cancellationToken));
            }
            else if (typeof(T) == typeof(GXScheduleGroup))
            {
                IScheduleGroupRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleGroupRepository>();
                ListScheduleGroups request = new()
                {
                    Filter = filter as GXScheduleGroup
                };
                return (IEnumerable<T>)(object)(await repository.ListAsync(request, cancellationToken: cancellationToken));
            }
            else if (typeof(T) == typeof(GXSchedule))
            {
                IScheduleRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
                ListSchedules request = new()
                {
                    Filter = filter as GXSchedule
                };
                return (IEnumerable<T>)(object)(await repository.ListAsync(request, cancellationToken: cancellationToken));
            }

            else if (typeof(T) == typeof(GXGatewayGroup))
            {
                IGatewayGroupRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayGroupRepository>();
                ListGatewayGroups request = new()
                {
                    Filter = filter as GXGatewayGroup
                };
                return (IEnumerable<T>)(object)(await repository.ListAsync(request, cancellationToken: cancellationToken));
            }
            else if (typeof(T) == typeof(GXGateway))
            {
                IGatewayRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayRepository>();
                ListGateways request = new()
                {
                    Filter = filter as GXGateway
                };
                return (IEnumerable<T>)(object)(await repository.ListAsync(request, cancellationToken: cancellationToken));
            }

            else
            {
                throw new ArgumentException("Add script failed. Unknown target.");
            }
        }

        /// <inheritdoc />
        public T? SingleOrDefault<T>(T filter)
        {
            return SingleOrDefaultAsync(filter).Result;
        }

        /// <inheritdoc />
        public async Task<T?> SingleOrDefaultAsync<T>(T value, CancellationToken cancellationToken = default)
        {
            if (_serviceProvider == null)
            {
                throw new ArgumentException(nameof(_serviceProvider));
            }
            object? ret = null;
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                if (value is GXLog se)
                {
                    ILogRepository repository = scope.ServiceProvider.GetRequiredService<ILogRepository>();
                    ret = await repository.ReadAsync(se.PublicId, cancellationToken: cancellationToken);
                }

                else if (value is GXDeviceGroup dg)
                {
                    IDeviceGroupRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceGroupRepository>();
                    ret = await repository.ReadAsync(dg.Id, cancellationToken: cancellationToken);
                }
                else if (value is GXDevice d)
                {
                    IDeviceRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                    ListDevices request = new()
                    {
                        Filter = d
                    };
                    var devices = await repository.ListAsync(request, cancellationToken: cancellationToken);
                    if (devices != null && devices.Count() == 1)
                    {
                        ret = devices.FirstOrDefault();
                    }
                }
                else if (value is GXObject o)
                {
                    IObjectRepository repository = scope.ServiceProvider.GetRequiredService<IObjectRepository>();
                    ret = await repository.ReadAsync(o.Id, cancellationToken: cancellationToken);
                }
                else if (value is GXTask t)
                {
                    ITaskRepository repository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
                    ret = await repository.ReadAsync(t.Id, cancellationToken: cancellationToken);
                }

                else if (value is GXAgentGroup ag)
                {
                    IAgentGroupRepository repository = scope.ServiceProvider.GetRequiredService<IAgentGroupRepository>();
                    ret = await repository.ReadAsync(ag.Id, cancellationToken: cancellationToken);
                }
                else if (value is GXAgent a)
                {
                    IAgentRepository repository = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
                    ret = await repository.ReadAsync(a.Id, cancellationToken: cancellationToken);
                }
                else if (value is GXUserGroup ug)
                {
                    IUserGroupRepository repository = scope.ServiceProvider.GetRequiredService<IUserGroupRepository>();
                    ret = await repository.ReadAsync(ug.Id, cancellationToken: cancellationToken);
                }
                else if (value is GXUser u)
                {
                    IUserRepository repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                    ret = await repository.ReadAsync(u.Id, cancellationToken: cancellationToken);
                }
                else if (value is GXScheduleGroup sg)
                {
                    IScheduleGroupRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleGroupRepository>();
                    ret = await repository.ReadAsync(sg.Id, cancellationToken: cancellationToken);
                }
                else if (value is GXSchedule s)
                {
                    IScheduleRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
                    ret = await repository.ReadAsync(s.Id, cancellationToken: cancellationToken);
                }

                else if (value is GXGatewayGroup gwg)
                {
                    IGatewayGroupRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayGroupRepository>();
                    ret = await repository.ReadAsync(gwg.Id, cancellationToken: cancellationToken);
                }
                else if (value is GXGateway gw)
                {
                    IGatewayRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayRepository>();
                    ListGateways request = new()
                    {
                        Filter = gw
                    };
                    var gateways = await repository.ListAsync(request, cancellationToken: cancellationToken);
                    if (gateways != null && gateways.Count() == 1)
                    {
                        ret = gateways.FirstOrDefault();
                    }
                }

                else
                {
                    throw new ArgumentException("Add script failed. Unknown target.");
                }
            }
            return (T?)ret;
        }

        /// <inheritdoc />
        public void Add(object value)
        {
            AddAsync(value, default).Wait();
        }

        /// <inheritdoc />
        public void Remove(object value, bool delete)
        {
            RemoveAsync(value, delete, default).Wait();
        }

        /// <inheritdoc />
        public void Update(object value)
        {
            UpdateAsync(value, default).Wait();
        }

        /// <inheritdoc />
        public async Task ClearAsync<T>(IEnumerable<T>? items)
        {
            if (_serviceProvider == null)
            {
                throw new ArgumentException(nameof(_serviceProvider));
            }
            using IServiceScope scope = _serviceProvider.CreateScope();
            if (typeof(T) == typeof(GXLog))
            {
                ILogRepository repository = scope.ServiceProvider.GetRequiredService<ILogRepository>();
                await repository.ClearAsync(null, default);
            }


            else
            {
                throw new ArgumentException("Add script failed. Unknown target.");
            }
        }

        /// <inheritdoc />
        public void Clear<T>(IEnumerable<T>? items)
        {
            ClearAsync(items).Wait();
        }

        /// <inheritdoc />
        public void AddDevice(GXDevice value, bool lateBinding)
        {
            AddDeviceAsync(value, lateBinding).Wait();
        }

        /// <inheritdoc />
        public async Task AddDeviceAsync(GXDevice value, bool lateBinding, CancellationToken cancellationToken = default)
        {
            if (_serviceProvider == null)
            {
                throw new ArgumentException(nameof(_serviceProvider));
            }
            using IServiceScope scope = _serviceProvider.CreateScope();
            IDeviceRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
            value.Id = (await repository.UpdateAsync([value], cancellationToken, null, lateBinding)).First();
        }
    }
}
