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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Microsoft.Extensions.DependencyInjection;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Runtime.Loader;
using System.Runtime.ExceptionServices;

namespace Gurux.DLMS.AMI.Script
{
    /// <summary>
    /// This class implements Roslyn scripting engine.
    /// </summary>
    public class GXAmiScript : IGXAmi
    {
        object? _sender;
        private readonly IServiceProvider? _serviceProvider;
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXAmiScript(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc cref="IGXAmi.Sender"/>
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

        /// <inheritdoc cref="IGXAmi.User"/>
        public GXUser? User
        {
            get;
            set;
        }

        /// <summary>
        /// User claims to be.
        /// </summary>
        public ClaimsPrincipal Claims
        {
            get;
            set;
        }

        /// <inheritdoc />
        public async Task AddAsync(object value)
        {
            if (_serviceProvider == null)
            {
                throw new ArgumentException(nameof(_serviceProvider));
            }
            using IServiceScope scope = _serviceProvider.CreateScope();
            if (value is GXSystemLog se)
            {
                ISystemLogRepository repository = scope.ServiceProvider.GetRequiredService<ISystemLogRepository>();
                await repository.AddAsync(Claims, new GXSystemLog[] { se });
            }
            else if (value is GXDeviceError de)
            {
                IDeviceErrorRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceErrorRepository>();
                await repository.AddAsync(Claims, new GXDeviceError[] { de });
            }
            else if (value is GXDeviceGroup dg)
            {
                IDeviceGroupRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceGroupRepository>();
                await repository.UpdateAsync(Claims, new GXDeviceGroup[] { dg });
            }
            else if (value is GXDevice d)
            {
                IDeviceRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                d.Id = (await repository.UpdateAsync(Claims, new GXDevice[] { d }, CancellationToken.None))[0];
            }
            else if (value is GXObject o)
            {
                IObjectRepository repository = scope.ServiceProvider.GetRequiredService<IObjectRepository>();
                await repository.UpdateAsync(Claims, new GXObject[] { o });
            }
            else if (value is GXValue v)
            {
                IValueRepository repository = scope.ServiceProvider.GetRequiredService<IValueRepository>();
                await repository.AddAsync(Claims, new GXValue[] { v });
            }
            else if (value is GXTask t)
            {
                ITaskRepository repository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
                t.Id = (await repository.AddAsync(Claims, new GXTask[] { t }))[0];
            }
            else if (value is GXDeviceAction da)
            {
                IDeviceActionRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceActionRepository>();
                await repository.AddAsync(Claims, new GXDeviceAction[] { da });
            }
            else if (value is GXDeviceTrace dt)
            {
                IDeviceTraceRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceTraceRepository>();
                await repository.AddAsync(Claims, new GXDeviceTrace[] { dt });
            }
            else if (value is GXAgentGroup ag)
            {
                IAgentGroupRepository repository = scope.ServiceProvider.GetRequiredService<IAgentGroupRepository>();
                await repository.UpdateAsync(Claims, new GXAgentGroup[] { ag });
            }
            else if (value is GXAgent a)
            {
                IAgentRepository repository = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
                await repository.UpdateAsync(Claims, new GXAgent[] { a });
            }
            else if (value is GXUserGroup ug)
            {
                IUserGroupRepository repository = scope.ServiceProvider.GetRequiredService<IUserGroupRepository>();
                await repository.UpdateAsync(Claims, new GXUserGroup[] { ug });
            }
            else if (value is GXUser u)
            {
                IUserRepository repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                await repository.UpdateAsync(Claims, new GXUser[] { u });
            }
            else if (value is GXScheduleGroup sg)
            {
                IScheduleGroupRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleGroupRepository>();
                await repository.UpdateAsync(Claims, new GXScheduleGroup[] { sg });
            }
            else if (value is GXSchedule s)
            {
                IScheduleRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
                await repository.UpdateAsync(Claims, new GXSchedule[] { s });
            }
            else if (value is GXUserError ue)
            {
                IUserErrorRepository repository = scope.ServiceProvider.GetRequiredService<IUserErrorRepository>();
                await repository.AddAsync(Claims, new GXUserError[] { ue });
            }
            else if (value is GXAgentLog ae)
            {
                IAgentLogRepository repository = scope.ServiceProvider.GetRequiredService<IAgentLogRepository>();
                await repository.AddAsync(Claims, new GXAgentLog[] { ae });
            }
            else if (value is IEnumerable<GXSystemLog> seList)
            {
                ISystemLogRepository repository = scope.ServiceProvider.GetRequiredService<ISystemLogRepository>();
                await repository.AddAsync(Claims, seList);
            }
            else if (value is IEnumerable<GXDeviceError> deList)
            {
                IDeviceErrorRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceErrorRepository>();
                await repository.AddAsync(Claims, deList);
            }
            else if (value is IEnumerable<GXDeviceGroup> dgList)
            {
                IDeviceGroupRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceGroupRepository>();
                var ret = await repository.UpdateAsync(Claims, dgList);
                for (int pos = 0; pos < ret.Length; ++pos)
                {
                    dgList.ElementAt(pos).Id = ret[pos];
                }
            }
            else if (value is IEnumerable<GXDevice> dList)
            {
                IDeviceRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                var ret  = await repository.UpdateAsync(Claims, dList, default);
                for (int pos = 0; pos < ret.Length; ++pos)
                {
                    dList.ElementAt(pos).Id = ret[pos];
                }
            }
            else if (value is IEnumerable<GXObject> oList)
            {
                IObjectRepository repository = scope.ServiceProvider.GetRequiredService<IObjectRepository>();
                await repository.UpdateAsync(Claims, oList);
            }
            else if (value is IEnumerable<GXValue> vList)
            {
                IValueRepository repository = scope.ServiceProvider.GetRequiredService<IValueRepository>();
                await repository.AddAsync(Claims, vList);
            }
            else if (value is IEnumerable<GXTask> tList)
            {
                ITaskRepository repository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
                await repository.AddAsync(Claims, tList);
            }
            else if (value is IEnumerable<GXDeviceAction> daList)
            {
                IDeviceActionRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceActionRepository>();
                await repository.AddAsync(Claims, daList);
            }
            else if (value is IEnumerable<GXDeviceTrace> dtList)
            {
                IDeviceTraceRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceTraceRepository>();
                await repository.AddAsync(Claims, dtList);
            }
            else if (value is IEnumerable<GXAgentGroup> agList)
            {
                IAgentGroupRepository repository = scope.ServiceProvider.GetRequiredService<IAgentGroupRepository>();
                await repository.UpdateAsync(Claims, agList);
            }
            else if (value is IEnumerable<GXAgent> aList)
            {
                IAgentRepository repository = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
                await repository.UpdateAsync(Claims, aList);
            }
            else if (value is IEnumerable<GXUserGroup> ugList)
            {
                IUserGroupRepository repository = scope.ServiceProvider.GetRequiredService<IUserGroupRepository>();
                await repository.UpdateAsync(Claims, ugList);
            }
            else if (value is IEnumerable<GXUser> uList)
            {
                IUserRepository repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                await repository.UpdateAsync(Claims, uList);
            }
            else if (value is IEnumerable<GXScheduleGroup> sgList)
            {
                IScheduleGroupRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleGroupRepository>();
                await repository.UpdateAsync(Claims, sgList);
            }
            else if (value is IEnumerable<GXSchedule> sList)
            {
                IScheduleRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
                await repository.UpdateAsync(Claims, sList);
            }
            else if (value is IEnumerable<GXUserError> ueList)
            {
                IUserErrorRepository repository = scope.ServiceProvider.GetRequiredService<IUserErrorRepository>();
                await repository.AddAsync(Claims, ueList);
            }
            else if (value is IEnumerable<GXAgentLog> aeList)
            {
                IAgentLogRepository repository = scope.ServiceProvider.GetRequiredService<IAgentLogRepository>();
                await repository.AddAsync(Claims, aeList);
            }
            else if (value is IEnumerable<GXGatewayGroup> gwgList)
            {
                IGatewayGroupRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayGroupRepository>();
                var ret = await repository.UpdateAsync(Claims, gwgList);
                for (int pos = 0; pos < ret.Length; ++pos)
                {
                    gwgList.ElementAt(pos).Id = ret[pos];
                }
            }
            else if (value is IEnumerable<GXGateway> gwList)
            {
                IGatewayRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayRepository>();
                var ret = await repository.UpdateAsync(Claims, gwList);
                for (int pos = 0; pos < ret.Length; ++pos)
                {
                    gwList.ElementAt(pos).Id = ret[pos];
                }
            }
            else if (value is GXGateway gw)
            {
                IGatewayRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayRepository>();
                gw.Id = (await repository.UpdateAsync(Claims, new GXGateway[] { gw }))[0];
            }
            else if (value is IEnumerable<GXGatewayLog> gwlList)
            {
                IGatewayLogRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayLogRepository>();
                await repository.AddAsync(Claims, gwlList);
            }
            else
            {
                throw new ArgumentException("Add script failed. Unknown target.");
            }
        }

        /// <inheritdoc />
        public async Task UpdateAsync(object value)
        {
            await AddAsync(value);
        }

        /// <inheritdoc />
        public async Task RemoveAsync(object value, bool delete)
        {
            if (_serviceProvider == null)
            {
                throw new ArgumentException(nameof(_serviceProvider));
            }
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                if (value is GXDeviceGroup dg)
                {
                    IDeviceGroupRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceGroupRepository>();
                    await repository.DeleteAsync(Claims, new Guid[] { dg.Id }, delete);
                }
                else if (value is GXDevice d)
                {
                    IDeviceRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                    await repository.DeleteAsync(Claims, new Guid[] { d.Id }, delete);
                }
                else if (value is GXObject o)
                {
                    IObjectRepository repository = scope.ServiceProvider.GetRequiredService<IObjectRepository>();
                    await repository.DeleteAsync(Claims, new Guid[] { o.Id }, delete);
                }
                else if (value is GXTask t)
                {
                    ITaskRepository repository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
                    await repository.DeleteAsync(Claims, new Guid[] { t.Id });
                }
                else if (value is GXAgentGroup ag)
                {
                    IAgentGroupRepository repository = scope.ServiceProvider.GetRequiredService<IAgentGroupRepository>();
                    await repository.DeleteAsync(Claims, new Guid[] { ag.Id }, delete);
                }
                else if (value is GXAgent a)
                {
                    IAgentRepository repository = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
                    await repository.DeleteAsync(Claims, new Guid[] { a.Id }, delete);
                }
                else if (value is GXUserGroup ug)
                {
                    IUserGroupRepository repository = scope.ServiceProvider.GetRequiredService<IUserGroupRepository>();
                    await repository.DeleteAsync(Claims, new Guid[] { ug.Id }, delete);
                }
                else if (value is GXUser u)
                {
                    IUserRepository repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                    await repository.DeleteAsync(Claims, new string[] { u.Id }, delete);
                }
                else if (value is GXScheduleGroup sg)
                {
                    IScheduleGroupRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleGroupRepository>();
                    await repository.DeleteAsync(Claims, new Guid[] { sg.Id }, delete);
                }
                else if (value is GXSchedule s)
                {
                    IScheduleRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
                    await repository.DeleteAsync(Claims, new Guid[] { s.Id }, delete);
                }
                else if (value is GXUserError ue)
                {
                    IUserErrorRepository repository = scope.ServiceProvider.GetRequiredService<IUserErrorRepository>();
                    await repository.CloseAsync(Claims, new Guid[] { ue.Id });
                }
                else if (value is IEnumerable<GXDeviceGroup> dgList)
                {
                    IDeviceGroupRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceGroupRepository>();
                    await repository.DeleteAsync(Claims, dgList.Select(s => s.Id).ToList(), delete);
                }
                else if (value is IEnumerable<GXDevice> dList)
                {
                    IDeviceRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                    await repository.DeleteAsync(Claims, dList.Select(s => s.Id).ToList(), delete);
                }
                else if (value is IEnumerable<GXObject> oList)
                {
                    IObjectRepository repository = scope.ServiceProvider.GetRequiredService<IObjectRepository>();
                    await repository.DeleteAsync(Claims, oList.Select(s => s.Id).ToList(), delete);
                }
                else if (value is IEnumerable<GXTask> tList)
                {
                    ITaskRepository repository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
                    await repository.DeleteAsync(Claims, tList.Select(s => s.Id).ToList());
                }
                else if (value is IEnumerable<GXAgentGroup> agList)
                {
                    IAgentGroupRepository repository = scope.ServiceProvider.GetRequiredService<IAgentGroupRepository>();
                    await repository.DeleteAsync(Claims, agList.Select(s => s.Id).ToList(), delete);
                }
                else if (value is IEnumerable<GXAgent> aList)
                {
                    IAgentRepository repository = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
                    await repository.DeleteAsync(Claims, aList.Select(s => s.Id).ToList(), delete);
                }
                else if (value is IEnumerable<GXUserGroup> ugList)
                {
                    IUserGroupRepository repository = scope.ServiceProvider.GetRequiredService<IUserGroupRepository>();
                    await repository.DeleteAsync(Claims, ugList.Select(s => s.Id).ToList(), delete);
                }
                else if (value is IEnumerable<GXUser> uList)
                {
                    IUserRepository repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                    await repository.DeleteAsync(Claims, uList.Select(s => s.Id).ToList(), delete);
                }
                else if (value is IEnumerable<GXScheduleGroup> sgList)
                {
                    IScheduleGroupRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleGroupRepository>();
                    await repository.DeleteAsync(Claims, sgList.Select(s => s.Id).ToList(), delete);
                }
                else if (value is IEnumerable<GXSchedule> sList)
                {
                    IScheduleRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
                    await repository.DeleteAsync(Claims, sList.Select(s => s.Id).ToList(), delete);
                }
                else if (value is IEnumerable<GXUserError> ueList)
                {
                    IUserErrorRepository repository = scope.ServiceProvider.GetRequiredService<IUserErrorRepository>();
                    await repository.CloseAsync(Claims, ueList.Select(s => s.Id).ToList());
                }
                else if (value is IEnumerable<GXGatewayGroup> gwgList)
                {
                    IGatewayGroupRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayGroupRepository>();
                    await repository.DeleteAsync(Claims, gwgList.Select(s => s.Id).ToList(), delete);
                }
                else if (value is IEnumerable<GXGateway> gwList)
                {
                    IGatewayRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayRepository>();
                    await repository.DeleteAsync(Claims, gwList.Select(s => s.Id).ToList(), delete);
                }
                else if (value is IEnumerable<GXGatewayLog> gwlList)
                {
                    IGatewayLogRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayLogRepository>();
                    await repository.CloseAsync(Claims, gwlList.Select(s => s.Id).ToList());
                }
                else
                {
                    throw new ArgumentException("Remove script failed. Unknown target.");
                }
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
            List<MetadataReference> references = new();
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
                 syntaxTrees: new[] { syntaxTree },
                 references: references,
                 options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            }
            else
            {
                compilation = VisualBasicCompilation.Create(fileName,
                 syntaxTrees: new[] { syntaxTree },
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
                    GXScriptException error = new GXScriptException()
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
                methods.AddRange(rootSyntaxNode.DescendantNodesAndSelf().OfType<MethodDeclarationSyntax>().ToList());
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
                    using (var ms = new MemoryStream())
                    {
                        ms.Write(args.ByteAssembly);
                        ms.Position = 0;
                        asm = args.AssemblyLoadContext.LoadFromStream(ms);
                    }
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
            var instance = asm.CreateInstance(amiMacroType.FullName, false, BindingFlags.Instance | BindingFlags.Public, null, new object[] { this }, null, null);
            MethodInfo? entryPoint = amiMacroType.GetMethod(args.MethodName);
            if (entryPoint == null)
            {
                throw new ArgumentException(string.Format("Invalid method name {0}.", args.MethodName));
            }
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

        /// <inheritdoc cref="IGXAmi.GetService"/>
        public object? GetService(Type type)
        {
            if (_serviceProvider == null)
            {
                throw new ArgumentException(nameof(_serviceProvider));
            }
            return _serviceProvider.GetService(type);
        }

        /// <inheritdoc cref="IGXAmi.GetService"/>
        public T? GetService<T>()
        {
            if (_serviceProvider == null)
            {
                throw new ArgumentException(nameof(_serviceProvider));
            }
            return (T?)_serviceProvider.GetService(typeof(T));
        }

        /// <inheritdoc />
        public T[] Select<T>(T filter)
        {
            return SelectAsync(filter).Result;
        }

        /// <inheritdoc />
        public async Task<T[]> SelectAsync<T>(T filter)
        {
            if (_serviceProvider == null)
            {
                throw new ArgumentException(nameof(_serviceProvider));
            }
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                if (typeof(T) == typeof(GXSystemLog))
                {
                    ISystemLogRepository repository = scope.ServiceProvider.GetRequiredService<ISystemLogRepository>();
                    ListSystemLogs request = new ListSystemLogs();
                    request.Filter = filter as GXSystemLog;
                    return (await repository.ListAsync(Claims, request, null, CancellationToken.None)) as T[];
                }
                else if (typeof(T) == typeof(GXDeviceError))
                {
                    IDeviceErrorRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceErrorRepository>();
                    ListDeviceErrors request = new ListDeviceErrors();
                    request.Filter = filter as GXDeviceError;
                    return (await repository.ListAsync(Claims, request, null, CancellationToken.None)) as T[];
                }
                else if (typeof(T) == typeof(GXDeviceGroup))
                {
                    IDeviceGroupRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceGroupRepository>();
                    ListDeviceGroups request = new ListDeviceGroups();
                    request.Filter = filter as GXDeviceGroup;
                    return (await repository.ListAsync(Claims, request, null, CancellationToken.None)) as T[];
                }
                else if (typeof(T) == typeof(GXDevice))
                {
                    IDeviceRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                    ListDevices request = new ListDevices();
                    request.Filter = filter as GXDevice;
                    return (await repository.ListAsync(Claims, request, null, CancellationToken.None)) as T[];
                }
                else if (typeof(T) == typeof(GXObject))
                {
                    IObjectRepository repository = scope.ServiceProvider.GetRequiredService<IObjectRepository>();
                    ListObjects request = new ListObjects();
                    request.Filter = filter as GXObject;
                    return (await repository.ListAsync(Claims, request, null, CancellationToken.None)) as T[];
                }
                else if (typeof(T) == typeof(GXTask))
                {
                    ITaskRepository repository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
                    ListTasks request = new ListTasks();
                    request.Filter = filter as GXTask;
                    return (await repository.ListAsync(Claims, request, null, CancellationToken.None)) as T[];
                }
                else if (typeof(T) == typeof(GXDeviceAction))
                {
                    IDeviceActionRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceActionRepository>();
                    ListDeviceAction request = new ListDeviceAction();
                    request.Filter = filter as GXDeviceAction;
                    return (await repository.ListAsync(Claims, request, null, CancellationToken.None)) as T[];
                }
                else if (typeof(T) == typeof(GXAgentGroup))
                {
                    IAgentGroupRepository repository = scope.ServiceProvider.GetRequiredService<IAgentGroupRepository>();
                    ListAgentGroups request = new ListAgentGroups();
                    request.Filter = filter as GXAgentGroup;
                    return (await repository.ListAsync(Claims, request, null, CancellationToken.None)) as T[];
                }
                else if (typeof(T) == typeof(GXAgent))
                {
                    IAgentRepository repository = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
                    ListAgents request = new ListAgents();
                    request.Filter = filter as GXAgent;
                    return (await repository.ListAsync(Claims, request, null, CancellationToken.None)) as T[];
                }
                else if (typeof(T) == typeof(GXUserGroup))
                {
                    IUserGroupRepository repository = scope.ServiceProvider.GetRequiredService<IUserGroupRepository>();
                    ListUserGroups request = new ListUserGroups();
                    request.Filter = filter as GXUserGroup;
                    return (await repository.ListAsync(Claims, request, null, CancellationToken.None)) as T[];
                }
                else if (typeof(T) == typeof(GXUser))
                {
                    IUserRepository repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                    ListUsers request = new ListUsers();
                    request.Filter = filter as GXUser;
                    return (await repository.ListAsync(Claims, request, null, CancellationToken.None)) as T[];
                }
                else if (typeof(T) == typeof(GXScheduleGroup))
                {
                    IScheduleGroupRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleGroupRepository>();
                    ListScheduleGroups request = new ListScheduleGroups();
                    request.Filter = filter as GXScheduleGroup;
                    return (await repository.ListAsync(Claims, request, null, CancellationToken.None)) as T[];
                }
                else if (typeof(T) == typeof(GXSchedule))
                {
                    IScheduleRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
                    ListSchedules request = new ListSchedules();
                    request.Filter = filter as GXSchedule;
                    return (await repository.ListAsync(Claims, request, null, CancellationToken.None)) as T[];
                }
                else if (typeof(T) == typeof(GXUserError))
                {
                    IUserErrorRepository repository = scope.ServiceProvider.GetRequiredService<IUserErrorRepository>();
                    ListUserErrors request = new ListUserErrors();
                    request.Filter = filter as GXUserError;
                    return (await repository.ListAsync(Claims, request, null, CancellationToken.None)) as T[];
                }
                else if (typeof(T) == typeof(GXGatewayGroup))
                {
                    IGatewayGroupRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayGroupRepository>();
                    ListGatewayGroups request = new ListGatewayGroups();
                    request.Filter = filter as GXGatewayGroup;
                    return (await repository.ListAsync(Claims, request, null, CancellationToken.None)) as T[];
                }
                else if (typeof(T) == typeof(GXGateway))
                {
                    IGatewayRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayRepository>();
                    ListGateways request = new ListGateways();
                    request.Filter = filter as GXGateway;
                    return (await repository.ListAsync(Claims, request, null, CancellationToken.None)) as T[];
                }
                else if (typeof(T) == typeof(GXGatewayLog))
                {
                    IGatewayLogRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayLogRepository>();
                    ListGatewayLogs request = new ListGatewayLogs();
                    request.Filter = filter as GXGatewayLog;
                    return (await repository.ListAsync(Claims, request, null, CancellationToken.None)) as T[];
                }
                else
                {
                    throw new ArgumentException("Add script failed. Unknown target.");
                }
            }
        }

        /// <inheritdoc cref="IGXAmi.SingleOrDefault"/>
        public T? SingleOrDefault<T>(T filter)
        {
            return SingleOrDefaultAsync(filter).Result;
        }

        /// <inheritdoc cref="IGXAmi.SingleOrDefaultAsync"/>
        public async Task<T?> SingleOrDefaultAsync<T>(T value)
        {
            if (_serviceProvider == null)
            {
                throw new ArgumentException(nameof(_serviceProvider));
            }
            object ret = null;
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                if (value is GXSystemLog se)
                {
                    ISystemLogRepository repository = scope.ServiceProvider.GetRequiredService<ISystemLogRepository>();
                    ret = await repository.ReadAsync(Claims, se.Id);
                }
                else if (value is GXDeviceError de)
                {
                    IDeviceErrorRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceErrorRepository>();
                    ret = await repository.ReadAsync(Claims, de.Id);
                }
                else if (value is GXDeviceGroup dg)
                {
                    IDeviceGroupRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceGroupRepository>();
                    ret = await repository.ReadAsync(Claims, dg.Id);
                }
                else if (value is GXDevice d)
                {
                    IDeviceRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
                    ListDevices request = new ListDevices()
                    {
                        Filter = d
                    };
                    var devices = await repository.ListAsync(Claims, request, null, CancellationToken.None);
                    if (devices != null && devices.Length == 1)
                    {
                        ret = devices[0];
                    }
                }
                else if (value is GXObject o)
                {
                    IObjectRepository repository = scope.ServiceProvider.GetRequiredService<IObjectRepository>();
                    ret = await repository.ReadAsync(Claims, o.Id);
                }
                else if (value is GXTask t)
                {
                    ITaskRepository repository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
                    ret = await repository.ReadAsync(Claims, t.Id);
                }
                else if (value is GXDeviceAction da)
                {
                    IDeviceActionRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceActionRepository>();
                    ret = await repository.ReadAsync(Claims, da.Id);
                }
                else if (value is GXAgentGroup ag)
                {
                    IAgentGroupRepository repository = scope.ServiceProvider.GetRequiredService<IAgentGroupRepository>();
                    ret = await repository.ReadAsync(Claims, ag.Id);
                }
                else if (value is GXAgent a)
                {
                    IAgentRepository repository = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
                    ret = await repository.ReadAsync(Claims, a.Id);
                }
                else if (value is GXUserGroup ug)
                {
                    IUserGroupRepository repository = scope.ServiceProvider.GetRequiredService<IUserGroupRepository>();
                    ret = await repository.ReadAsync(Claims, ug.Id);
                }
                else if (value is GXUser u)
                {
                    IUserRepository repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                    ret = await repository.ReadAsync(Claims, u.Id);
                }
                else if (value is GXScheduleGroup sg)
                {
                    IScheduleGroupRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleGroupRepository>();
                    ret = await repository.ReadAsync(Claims, sg.Id);
                }
                else if (value is GXSchedule s)
                {
                    IScheduleRepository repository = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
                    ret = await repository.ReadAsync(Claims, s.Id);
                }
                else if (value is GXUserError ue)
                {
                    IUserErrorRepository repository = scope.ServiceProvider.GetRequiredService<IUserErrorRepository>();
                    ret = await repository.ReadAsync(Claims, ue.Id);
                }
                else if (value is GXGatewayGroup gwg)
                {
                    IGatewayGroupRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayGroupRepository>();
                    ret = await repository.ReadAsync(Claims, gwg.Id);
                }
                else if (value is GXGateway gw)
                {
                    IGatewayRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayRepository>();
                    ListGateways request = new ListGateways()
                    {
                        Filter = gw
                    };
                    var gateways = await repository.ListAsync(Claims, request, null, CancellationToken.None);
                    if (gateways != null && gateways.Length == 1)
                    {
                        ret = gateways[0];
                    }
                }
                else if (value is GXGatewayLog gwl)
                {
                    IGatewayLogRepository repository = scope.ServiceProvider.GetRequiredService<IGatewayLogRepository>();
                    ret = await repository.ReadAsync(Claims, gwl.Id);
                }
                else
                {
                    throw new ArgumentException("Add script failed. Unknown target.");
                }
            }
            return (T?)ret;
        }

        /// <inheritdoc cref="IGXAmi.Add"/>
        public void Add(object value)
        {
            AddAsync(value).Wait();
        }

        /// <inheritdoc cref="IGXAmi.Remove"/>
        public void Remove(object value, bool delete)
        {
            RemoveAsync(value, delete).Wait();
        }

        /// <inheritdoc cref="IGXAmi.Update"/>
        public void Update(object value)
        {
            UpdateAsync(value).Wait();
        }

        /// <inheritdoc />
        public async Task ClearAsync<T>(IEnumerable<T>? items)
        {
            if (_serviceProvider == null)
            {
                throw new ArgumentException(nameof(_serviceProvider));
            }
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                if (typeof(T) == typeof(GXSystemLog))
                {
                    ISystemLogRepository repository = scope.ServiceProvider.GetRequiredService<ISystemLogRepository>();
                    await repository.ClearAsync(Claims);
                }
                else if (typeof(T) == typeof(GXDeviceError))
                {
                    List<Guid> removed = new List<Guid>();
                    if (items != null)
                    {
                        foreach (T it in items)
                        {
                            if (it is GXDeviceError de)
                            {
                                removed.Add(de.Id);
                            }
                            else
                            {
                                throw new ArgumentException("Invalid type.");
                            }
                        }
                    }
                    IDeviceErrorRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceErrorRepository>();
                    await repository.ClearAsync(Claims, removed);
                }
                else if (typeof(T) == typeof(GXDeviceAction))
                {
                    List<Guid> removed = new List<Guid>();
                    if (items != null)
                    {
                        foreach (T it in items)
                        {
                            if (it is GXDeviceAction de)
                            {
                                removed.Add(de.Id);
                            }
                            else
                            {
                                throw new ArgumentException("Invalid type.");
                            }
                        }
                    }
                    IDeviceActionRepository repository = scope.ServiceProvider.GetRequiredService<IDeviceActionRepository>();
                    await repository.ClearAsync(Claims, removed);
                }
                else if (typeof(T) == typeof(GXUserError))
                {
                    List<string> removed = new List<string>();
                    if (items != null)
                    {
                        foreach (T it in items)
                        {
                            if (it is GXUser de)
                            {
                                removed.Add(de.Id);
                            }
                            else
                            {
                                throw new ArgumentException("Invalid type.");
                            }
                        }
                    }
                    IUserErrorRepository repository = scope.ServiceProvider.GetRequiredService<IUserErrorRepository>();
                    await repository.ClearAsync(Claims, removed);
                }
                else
                {
                    throw new ArgumentException("Add script failed. Unknown target.");
                }
            }
        }

        /// <inheritdoc />
        public void Clear<T>(IEnumerable<T>? items)
        {
            ClearAsync(items).Wait();
        }
    }
}
