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

using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
using Gurux.DLMS.AMI.Module;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Gurux.DLMS.AMI.Server.Internal;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using System.Security.Claims;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Mvc;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;

namespace Gurux.DLMS.AMI.Server.Services
{
    /// <summary>
    /// Modules are load for own assembly load context.
    /// </summary>
    public class GXAssemblyLoadContext : AssemblyLoadContext
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Assembly load context name.</param>
        /// <param name="searchPath">Resource search path.</param>
        public GXAssemblyLoadContext(string name, string searchPath) : base(name, true)
        {
            SearchPath = searchPath;
        }

        /// <summary>
        /// Resource search path.
        /// </summary>
        public string SearchPath
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Module service methods.
    /// </summary>
    public interface IGXModuleService
    {
        /// <summary>
        /// Add new module.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="compressedFile">Name of the compressed file.</param>
        /// <param name="path">Installation path.</param>
        /// <returns></returns>
        Task<GXModule> AddModuleAsync(ClaimsPrincipal user, string compressedFile, string path);

        /// <summary>
        /// Enable installed module.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="module">Module to enable.</param>
        /// <returns>True, if server restart is required before module can be used.</returns>
        bool EnableModule(ClaimsPrincipal user,
            GXModule module);

        /// <summary>
        /// Disable installed module.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="module">Module to disable.</param>
        /// <returns>True, if server restart is required before module can be unloaded.</returns>
        bool DisableModule(ClaimsPrincipal user,
            GXModule module);

        /// <summary>
        /// Update module settings for the module.
        /// </summary>
        /// <param name="module">Module to update.</param>
        void UpdateModuleSettings(GXModule module);

        /// <summary>
        /// Delete installed module.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="module">Deleted module.</param>
        /// <returns>True, if server restart is required before module can be unloaded.</returns>
        bool DeleteModule(ClaimsPrincipal user,
            GXModule module);

        /// <summary>
        /// Execute module async.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="module">Module to execute.</param>
        /// <param name="settings">Schedule settings.</param>
        Task ExecuteAsync(ClaimsPrincipal user,
            GXModule module,
            string? settings);
    }

    /// <summary>
    /// Module information.
    /// </summary>
    internal class GXModuleInfo
    {
        /// <summary>
        /// Assembly load context;
        /// </summary>
        public AssemblyLoadContext? AssemblyLoadContext;
        /// <summary>
        /// Server module.
        /// </summary>
        public IAmiModule? Module;
        /// <summary>
        /// Services.
        /// </summary>
        public IServiceCollection Services = new ServiceCollection();
        /// <summary>
        /// Service provider.
        /// </summary>
        public ServiceProvider? ServiceProvider;
        /// <summary>
        /// Is restart required.
        /// </summary>
        public bool Restart
        {
            get;
            set;
        }

        public Dictionary<string, string?> Settings = new Dictionary<string, string?>();


        public void Clear()
        {
            AssemblyLoadContext = null;
            Module = null;
            Services.Clear();
            ServiceProvider = null;
        }
    }

    /// <summary>
    /// Module service is used to handle all external modules.
    /// </summary>
    public class GXModulesService
    {
        Dictionary<string, GXModuleInfo> _modules = new Dictionary<string, GXModuleInfo>();

        /// <summary>
        /// Get assembly parts.
        /// </summary>
        /// <returns></returns>
        public AssemblyPart[] GetAssemblyParts()
        {
            List<AssemblyPart> list = new List<AssemblyPart>();
            foreach (var module in _modules.Values)
            {
                if (module.AssemblyLoadContext != null)
                {
                    foreach (var it in module.AssemblyLoadContext.Assemblies)
                    {
                        list.Add(new AssemblyPart(it));
                    }
                }
            }
            return list.ToArray();
        }

        /// <summary>
        /// Get server modules.
        /// </summary>
        /// <returns></returns>
        public IAmiModule[] GetServerModules()
        {
            List<IAmiModule> list = new List<IAmiModule>();
            foreach (var it in _modules.Values)
            {
                if (it.Module != null)
                {
                    list.Add(it.Module);
                }
            }
            return list.ToArray();
        }

        /// <summary>
        /// Update module settings.
        /// </summary>
        /// <param name="module">Updated module.</param>
        public void UpdateModuleSettings(GXModule module)
        {
            if (_modules.TryGetValue(module.Id, out var info))
            {
                if (!string.IsNullOrEmpty(module.Settings))
                {
                    var values = JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(module.Settings);
                    if (values != null)
                    {
                        foreach (var it in values)
                        {
                            info.Settings[module.Id + ":" + it.Key] = Convert.ToString(it.Value);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Load module.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="name"></param>
        /// <param name="services"></param>
        /// <returns></returns>
        internal GXModuleInfo LoadModule(IGXHost host, string name, IServiceCollection? services)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Modules");
            GXSelectArgs args = GXSelectArgs.SelectAll<GXModule>(where => where.Active == true && where.Id == name);
            args.Columns.Add<GXModuleAssembly>();
            args.Joins.AddLeftJoin<GXModule, GXModuleAssembly>(j => j.Id, j => j.Module);
            GXModule module = host.Connection.SingleOrDefault<GXModule>(args);
            if (module == null)
            {
                throw new ArgumentException("Invalid module {0}.", name);
            }
            string? searchPath = null;
            string path2;
            IAmiModule? server;
            GXModuleInfo info;
            GXAssemblyLoadContext? alc = null;
            if (module.Version != null)
            {
                searchPath = Path.Combine(path, module.Id, module.Version);
                alc = new GXAssemblyLoadContext(module.Id, searchPath);
                alc.Resolving += ModuleResolving;
                path2 = Path.Combine(searchPath, module.FileName);
                Assembly asm = alc.LoadFromAssemblyPath(path2);
                //Create services.
                server = (IAmiModule?)asm.CreateInstance(module.Class);
                if (server == null)
                {
                    throw new ArgumentNullException();
                }
                AssemblyPart ap = new AssemblyPart(asm);
                info = new GXModuleInfo()
                {
                    AssemblyLoadContext = alc,
                    Module = server,
                };
            }
            else
            {
                //Internal module.                
                info = _modules[module.Id];
                server = info.Module;
            }
            var cfgBuilder = new ConfigurationBuilder();
            IConfigurationBuilder b = cfgBuilder.AddInMemoryCollection(info.Settings);
            IConfiguration cfg = cfgBuilder.Build();
            info.Services.AddSingleton(conf => cfg);
            server.ConfigureModuleServices(info.Services, cfg);
            if (services != null)
            {
                server.ConfigureModuleServices(services, cfg);
            }
            info.ServiceProvider = info.Services.BuildServiceProvider(false);
            //Update module settings.
            var configuredOptionTypes =
                from descriptor in info.Services
                let serviceType = descriptor.ServiceType
                where serviceType.IsGenericType
                where serviceType.GetGenericTypeDefinition() == typeof(IConfigureOptions<>)
                let optionType = serviceType.GetGenericArguments()[0]
                select optionType;
            Type? type = configuredOptionTypes.FirstOrDefault();
            if (type != null)
            {
                var option = info.ServiceProvider.GetService(typeof(IOptions<>).MakeGenericType(type));
                if (!string.IsNullOrEmpty(module.Settings))
                {
                    type = option.GetType().GenericTypeArguments.FirstOrDefault();
                    object? options = JsonSerializer.Deserialize(module.Settings, type);
                    foreach (var it in type.GetProperties())
                    {
                        if (it.CanRead && it.CanWrite)
                        {
                            cfg[module.Id + ":" + it.Name] = Convert.ToString(it.GetValue(options));
                        }
                    }
                }
            }
            if (_modules.ContainsKey(module.Id))
            {
                _modules.Remove(module.Id);
            }
            _modules.Add(module.Id, info);
            //Load assemblies.
            if (module.Assemblies != null && searchPath != null &&
                alc != null)
            {
                foreach (var it in module.Assemblies)
                {
                    path2 = Path.Combine(searchPath, it.FileName);
                    try
                    {
                        alc.LoadFromAssemblyPath(path2);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
            //Get framework services.
            if (services != null)
            {
                int count = services.Count;
                server.ConfigureFrameworkServices(services);
                //Check is restart needed.
                info.Restart = services.Count != count;
            }
            return info;
        }

        /// <summary>
        /// Delete the module.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="services">Available services.</param>
        /// <param name="module">Module to unload.</param>
        internal GXModuleInfo? DeleteModule(ClaimsPrincipal user, IServiceProvider services, GXModule module)
        {
            if (_modules.TryGetValue(module.Id, out GXModuleInfo? info) && info != null)
            {
                if (module.Scripts != null)
                {
                    foreach (var s in module.Scripts)
                    {
                        //Invoke uninstall script if it exist.
                        GXScriptMethod? method = s.Methods.Where(w => w.Name == "Uninstall").SingleOrDefault();
                        if (method != null)
                        {
                            IScriptRepository scriptRepository = services.GetRequiredService<IScriptRepository>();
                            scriptRepository.RunAsync(user, method.Id).Wait();
                        }
                    }
                }
                _modules.Remove(module.Id);
                if (info.Module != null)
                {
                    info.Module.Uninstall(user, services, module);
                }
                WeakReference alcWeakRef = new WeakReference(info.AssemblyLoadContext);
                if (info.AssemblyLoadContext != null)
                {
                    info.AssemblyLoadContext.Resolving -= ModuleResolving;
                    info.AssemblyLoadContext.Unload();
                }
                for (int i = 0; alcWeakRef.IsAlive && (i < 10); ++i)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                //Server needs restart before module is downloaded.
                if (alcWeakRef.IsAlive)
                {
                    info.Restart = true;
                }
            }
            return info;
        }


        /// <summary>
        /// Unload the module.
        /// </summary>
        /// <param name="module">Module to unload.</param>
        internal GXModuleInfo? UnloadModule(string module)
        {
            if (_modules.TryGetValue(module, out GXModuleInfo? info))
            {
                if (info.AssemblyLoadContext != null)
                {
                    WeakReference alcWeakRef = new WeakReference(info.AssemblyLoadContext);
                    info.AssemblyLoadContext.Resolving -= ModuleResolving;
                    info.AssemblyLoadContext.Unload();
                    for (int i = 0; alcWeakRef.IsAlive && (i < 10); i++)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                    _modules.Remove(module);
                }
            }
            return info;
        }

        /// <summary>
        /// Load modules and configure scoped and singleton services.
        /// </summary>
        /// <param name="host">Host</param>
        /// <param name="services">Services</param>
        public void LoadModules(IGXHost host, IServiceCollection services)
        {
            GXSelectArgs args = GXSelectArgs.SelectAll<GXModule>(where => where.Active == true && where.FileName != null);
            List<GXModule> modules = host.Connection.Select<GXModule>(args);
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Modules");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            List<IAmiModule> list = new List<IAmiModule>();
            var user = ServerSettings.GetDefaultAdminUser(host);
            if (user != null)
            {
                //Modules are installed after the first user is created.
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (var type in assembly.GetExportedTypes())
                        {
                            if (type.IsAbstract || !type.IsClass || type.FullName == null)
                            {
                                continue;
                            }
                            if (typeof(IAmiModule).IsAssignableFrom(type))
                            {
                                IAmiModule? m = (IAmiModule?)Activator.CreateInstance(type);
                                if (!_modules.ContainsKey(m.Id))
                                {
                                    args = GXSelectArgs.SelectAll<GXModule>(w => w.Id == m.Id);
                                    GXModule module = host.Connection.SingleOrDefault<GXModule>(args);
                                    if (module == null)
                                    {
                                        list.Add(m);
                                        module = new GXModule()
                                        {
                                            //New module is inactive as a default.
                                            Active = false,
                                            Status = ModuleStatus.CustomBuild,
                                            Creator = new GXUser()
                                            {
                                                Id = ServerHelpers.GetUserId(user)
                                            },
                                            Id = m.Id,
                                            Name = m.Name,
                                            Icon = m.Icon,
                                            Class = type.FullName,
                                            Description = m.Description,
                                            CreationTime = DateTime.Now,
                                        };
                                        if (m.CanSchedule)
                                        {
                                            module.Type = module.Type.GetValueOrDefault() | ModuleType.Schedule;
                                        }
                                        if (m.Extension != null)
                                        {
                                            module.Type = module.Type.GetValueOrDefault() | ModuleType.Extension;
                                            module.ExtensionUI = m.Extension.FullName;
                                        }
                                        if (m.Configuration != null)
                                        {
                                            module.Type = module.Type.GetValueOrDefault() | ModuleType.Settings;
                                            module.ConfigurationUI = m.Configuration.FullName;
                                        }

                                        GXInsertArgs ins = GXInsertArgs.Insert(module);
                                        host.Connection.Insert(ins);
                                        modules.Add(module);
                                    }
                                    GXModuleInfo info = new GXModuleInfo()
                                    {
                                        AssemblyLoadContext = null,
                                        Module = m,
                                    };
                                    _modules.Add(module.Id, info);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        //It's OK if this fails. Assembly is skipped.
                    }
                }
            }
            foreach (GXModule module in modules)
            {
                try
                {
                    if ((module.Status & (ModuleStatus.Installed | ModuleStatus.CustomBuild)) != 0 &&
                        //Internal module is not loaded.
                        module.Version != null)
                    {
                        LoadModule(host, module.Id, services);
                    }
                }
                catch (Exception ex)
                {
                    //Disable module if there is an error.
                    module.Active = false;
                    module.Updated = DateTime.Now;
                    host.Connection.Update(GXUpdateArgs.Update(module, c => new { c.Active, c.Updated }));
                    var error = new GXModuleLog(TraceLevel.Error)
                    {
                        CreationTime = DateTime.Now,
                        Message = "Failed to load module. " + module.Class + Environment.NewLine +
                                ex.Message,
                    };
                    host.Connection.Insert(GXInsertArgs.Insert(error));
                }
            }
        }

        /// <summary>
        /// Execute module actions.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="module">Module to execute.</param>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="settings">Module global settings.</param>
        /// <param name="instanceSettings">Module instance settings.</param>
        internal async Task ExecuteAsync(
            ClaimsPrincipal user,
            string module,
            IServiceProvider serviceProvider, string? settings,
            string? instanceSettings)
        {
            if (_modules.TryGetValue(module, out GXModuleInfo? info))
            {
                if (info.Module != null)
                {
                    await info.Module.ExecuteAsync(
                        user, serviceProvider,
                        settings, instanceSettings);
                }
            }
        }

        private Assembly ModuleResolving(AssemblyLoadContext arg1, AssemblyName arg2)
        {
            //Find module from loaded assemblies.
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (assembly.GetName().Name == arg2.Name)
                    {
                        return assembly;
                    }
                }
                catch (Exception)
                {
                    //It's OK if this fails. Assembly is skipped.
                }
            }
            return arg1.LoadFromAssemblyPath(Path.Combine(((GXAssemblyLoadContext)arg1).SearchPath, arg2.Name) + ".dll");
        }
    }

    /// <summary>
    /// This service is used to save current modules.
    /// </summary>
    public class GXModuleService : IGXModuleService
    {
        private IGXHost _host;
        private readonly ApplicationPartManager _partManager;
        private readonly IServiceProvider _serviceProvider;
        private readonly GXActionDescriptorChangeProvider _actionDescriptorChangeProvider;
        private readonly GXModulesService _modulesService;
        private readonly IScriptRepository _scriptRepository;
        private readonly IModuleRepository _moduleRepository;
        private readonly IModuleGroupRepository _moduleGroupRepository;
        private readonly IUserGroupRepository _userGroupRepository;
        private readonly IComponentViewRepository _componentViewRepositorymoduleRepository;


        /// <summary>
        /// Constructor.
        /// </summary>
        public GXModuleService(IGXHost host,
            ApplicationPartManager partManager,
            GXActionDescriptorChangeProvider actionDescriptorChangeProvider,
            GXModulesService modulesService,
            IScriptRepository scriptRepository,
            IModuleRepository moduleRepository,
            IModuleGroupRepository moduleGroupRepository,
            IUserGroupRepository userGroupRepository,
        IComponentViewRepository componentViewRepositorymoduleRepository,
            IServiceProvider serviceProvider)
        {
            _partManager = partManager;
            _host = host;
            _serviceProvider = serviceProvider;
            _actionDescriptorChangeProvider = actionDescriptorChangeProvider;
            _modulesService = modulesService;
            _scriptRepository = scriptRepository;
            _moduleRepository = moduleRepository;
            _moduleGroupRepository = moduleGroupRepository;
            _userGroupRepository = userGroupRepository;
            _componentViewRepositorymoduleRepository = componentViewRepositorymoduleRepository;
            var parts = modulesService.GetAssemblyParts();
            foreach (var part in parts)
            {
                _partManager.ApplicationParts.Add(part);
            }
            if (parts.Any())
            {
                // Notify change
                _actionDescriptorChangeProvider.TokenSource.Cancel();
            }
            IAmiModule[] modules = modulesService.GetServerModules();
            foreach (IAmiModule it in modules)
            {
                try
                {
                    it.Start(serviceProvider);
                }
                catch (Exception ex)
                {
                    GXModule module = new GXModule() { Id = it.Id };
                    module.Active = false;
                    host.Connection.Update(GXUpdateArgs.Update(module, c => c.Active));
                    var error = new GXModuleLog(TraceLevel.Error)
                    {
                        CreationTime = DateTime.Now,
                        Message = string.Format("Failed to start module '{0}'. {1}", it.Name, ex.Message)
                    };
                    host.Connection.Insert(GXInsertArgs.Insert(error));
                }
            }
        }

        ///<inheritdoc />
        public async Task<GXModule> AddModuleAsync(ClaimsPrincipal user, string compressedFile, string fileName2)
        {
            //Unzip the file.
            string tempFolder = Path.Combine(Path.GetDirectoryName(compressedFile), Guid.NewGuid().ToString());
            if (string.Compare(Path.GetExtension(fileName2), ".zip", true) == 0)
            {
                ZipFile.ExtractToDirectory(compressedFile, tempFolder);
            }
            else
            {
                throw new Exception("Module update failed. Invalid module file.");
            }
            IAmiModule? server = null;
            GXModule? existing = null;
            GXModule module = new GXModule("");
            AssemblyLoadContext alc = new AssemblyLoadContext("tmp", true);
            List<string> servers = new List<string>();
            List<GXModuleAssembly> assemblies = new List<GXModuleAssembly>();
            GXModuleAssembly? serverAsm = null;
            try
            {
                foreach (string fileName in Directory.GetFiles(tempFolder, "*.dll", SearchOption.AllDirectories))
                {
                    string name = Path.GetFileNameWithoutExtension(fileName);
                    if (name == "Gurux.DLMS.AMI.Client" ||
                        name == "Gurux.DLMS.AMI.Module" ||
                        name == "Gurux.DLMS.AMI.Shared" ||
                        name == "Gurux.DLMS.AMI.Script" ||
                        name == "Gurux.DLMS.AMI.UI" ||
                        name == "Newtonsoft.Json" ||
                        name == "BrowserDebugProxy" ||
                        name == "BrowserDebugHost" ||
                        name == "Microsoft.CodeAnalysis")
                    {
                        continue;
                    }

                    GXModuleAssembly ma = new GXModuleAssembly()
                    {
                        FileName = fileName.Replace(tempFolder + Path.DirectorySeparatorChar, ""),
                        //FileName = Path.GetFileName(fileName),
                        Module = module
                    };
                    assemblies.Add(ma);
                    Assembly asm = alc.LoadFromAssemblyPath(fileName);
                    try
                    {
                        foreach (Type type in asm.GetExportedTypes())
                        {
                            if (type.IsAbstract || !type.IsClass || type.FullName == null)
                            {
                                continue;
                            }
                            //Server side code is not share with the client app.
                            if (typeof(ControllerBase).IsAssignableFrom(type))
                            {
                                serverAsm = ma;
                            }
                            if (typeof(IAmiModule).IsAssignableFrom(type))
                            {
                                server = (IAmiModule?)asm.CreateInstance(type.FullName);
                                if (server == null)
                                {
                                    throw new Exception(string.Format("Failed to create {0} module", type.FullName));
                                }
                                if (server.CanSchedule)
                                {
                                    module.Type = module.Type.GetValueOrDefault() | ModuleType.Schedule;
                                }
                                module.CreationTime = DateTime.Now;
                                module.Id = server.Id;
                                module.Name = server.Name;
                                module.FileName = Path.GetFileName(fileName);
                                servers.Add(module.FileName);
                                module.Class = type.FullName;
                                module.Description = server.Description;
                                if (server.Extension != null)
                                {
                                    module.Type = module.Type.GetValueOrDefault() | ModuleType.Extension;
                                    module.ExtensionUI = server.Extension.FullName;
                                }
                                if (server.Configuration != null)
                                {
                                    module.Type = module.Type.GetValueOrDefault() | ModuleType.Settings;
                                    module.ConfigurationUI = server.Configuration.FullName;
                                }
                                FileVersionInfo info = FileVersionInfo.GetVersionInfo(asm.Location);
                                module.Version = info.FileVersion;
                                if (module.Version == null)
                                {
                                    throw new ArgumentException("Invalid module version.");
                                }
                                module.Icon = server.Icon;
                                //Check is module already installed.
                                existing = _host.Connection.SelectById<GXModule>(server.Id);
                                if (existing != null &&
                                    (existing.Status & ModuleStatus.Installed) != 0 &&
                                    existing.Version == module.Version)
                                {
                                    throw new Exception(string.Format("{0} module version {1} already installed.", server.Id, module.Version));
                                }
                                if (existing != null)
                                {
                                    module.Status = existing.Status;
                                }
                            }
                            else if (typeof(IAMINavigation).IsAssignableFrom(type))
                            {
                                //Create object to verify it.
                                IAMINavigation? client = (IAMINavigation?)asm.CreateInstance(type.FullName);
                            }
                            else if (typeof(IAmiModuleSettings).IsAssignableFrom(type))
                            {
                                //This is a client module and it can't be created.
                            }
                            else if (typeof(IAmiComponent).IsAssignableFrom(type))
                            {
                                //Create object to verify it.
                                IAmiComponent? c = (IAmiComponent?)asm.CreateInstance(type.FullName);
                                //Add component view.
                                GXComponentView view = new GXComponentView()
                                {
                                    Name = c.Name,
                                    ClassName = type.FullName,
                                    Icon = c.Icon
                                };
                                await _componentViewRepositorymoduleRepository.UpdateAsync(user,
                                    new GXComponentView[] { view });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        assemblies.Remove(ma);
                        Console.WriteLine(ex.Message);
                    }
                }
                if (server == null)
                {
                    throw new Exception(string.Format("Failed to create {0} module", module.Class));
                }
                //Remove servers from satelite assemblies.
                //Servers are not send to the client.
                if (serverAsm != null)
                {
                    assemblies.Remove(serverAsm);
                }
                //Check is module already added or new item is installed.
                if (existing == null)
                {
                    //New module.
                    module.Status = ModuleStatus.CustomBuild | ModuleStatus.Installed;
                    module.Assemblies = assemblies;
                    module.CreationTime = DateTime.Now;
                    module.Creator = new GXUser()
                    {
                        Id = ServerHelpers.GetUserId(user)
                    };
                    //Add module to default module groups.
                    {
                        ListModuleGroups request = new ListModuleGroups()
                        {
                            Filter = new GXModuleGroup() { Default = true }
                        };
                        module.ModuleGroups.AddRange(await _moduleGroupRepository.ListAsync(user, request, null, CancellationToken.None));
                    }

                    GXInsertArgs args = GXInsertArgs.Insert(module);
                    args.Exclude<GXModule>(e => e.Updated);
                    await _host.Connection.InsertAsync(args);
                    server?.Install(user, _serviceProvider, module);
                    //Read installed module methods, etc...
                    module = await _moduleRepository.ReadAsync(user, module.Id);
                    if (module.Scripts != null)
                    {
                        foreach (var s in module.Scripts)
                        {
                            //Invoke install script if it exist.
                            GXScriptMethod? method = s.Methods.Where(w => w.Name == "Install").SingleOrDefault();
                            if (method != null)
                            {
                                await _scriptRepository.RunAsync(user, method.Id);
                            }
                        }
                    }
                }
                else
                {
                    module.Updated = DateTime.Now;
                    module.Status |= ModuleStatus.Installed;
                    module.Active = true;
                    module.NewVersion = false;
                    {
                        GXUpdateArgs args = GXUpdateArgs.Update(module);
                        args.Exclude<GXModule>(e => e.CreationTime);
                        await _host.Connection.UpdateAsync(args);
                    }
                    foreach (var it in assemblies)
                    {
                        GXInsertArgs args = GXInsertArgs.Insert(it);
                        await _host.Connection.InsertAsync(args);
                    }

                    server.Update(user, _serviceProvider, existing?.Version, module?.Version);
                    //Read updated module methods, etc...
                    module = await _moduleRepository.ReadAsync(user, module.Id);
                    if (module.Scripts != null)
                    {
                        foreach (var s in module.Scripts)
                        {
                            //Invoke Update script if it exist.
                            GXScriptMethod? method = s.Methods.Where(w => w.Name == "Update").SingleOrDefault();
                            if (method != null)
                            {
                                await _scriptRepository.RunAsync(user, method.Id);
                            }
                        }
                    }
                }
            }
            finally
            {
                server = null;
                if (alc != null)
                {
                    WeakReference alcWeakRef = new WeakReference(alc);
                    for (int i = 0; alcWeakRef.IsAlive && (i < 10); ++i)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                    alc.Unload();
                }
            }
            //Copy files to module folder.
            if (!Directory.Exists(Path.Combine("Modules", module.Id)))
            {
                Directory.CreateDirectory(Path.Combine("Modules", module.Id));
            }
            if (!Directory.Exists(Path.Combine("Modules", module.Id, module.Version)))
            {
                Directory.CreateDirectory(Path.Combine("Modules", module.Id, module.Version));
                string path = Path.Combine(Directory.GetCurrentDirectory(), "Modules", module.Id, module.Version);
                ZipFile.ExtractToDirectory(compressedFile, path, true);
            }
            return module;
        }

        ///<inheritdoc />
        public void UpdateModuleSettings(GXModule module)
        {
            _modulesService.UpdateModuleSettings(module);
        }

        ///<inheritdoc />
        public bool EnableModule(
            ClaimsPrincipal user,
            GXModule module)
        {
            try
            {
                module.Active = true;
                module.Updated = DateTime.Now;
                _host.Connection.Update(GXUpdateArgs.Update(module, c => new { c.Active, c.Updated }));
                GXModuleInfo info = _modulesService.LoadModule(_host, module.Id, null);
                if (info.Module != null)
                {
                    if ((module.Status & ModuleStatus.Installed) == 0)
                    {
                        info.Module.InstallAsync(user, _serviceProvider, module);
                        info.Module.Install(user, _serviceProvider, module);
                        module.Status |= ModuleStatus.Installed;
                        _host.Connection.Update(GXUpdateArgs.Update(module, c => new { c.Status }));
                    }
                    info.Module.Start(_serviceProvider);
                }
                if (info.AssemblyLoadContext != null)
                {
                    foreach (var it in info.AssemblyLoadContext.Assemblies)
                    {
                        _partManager.ApplicationParts.Add(new AssemblyPart(it));
                    }
                    // Notify from the change.
                    if (info.AssemblyLoadContext.Assemblies.Any())
                    {
                        _actionDescriptorChangeProvider.TokenSource.Cancel();
                    }
                }
                return info.Restart;
            }
            catch (Exception ex)
            {
                //Disable module if there is an error.
                module.Active = false;
                module.Updated = DateTime.Now;
                _host.Connection.Update(GXUpdateArgs.Update(module, c => new
                {
                    c.Active,
                    c.Updated
                }));
                var error = new GXModuleLog(TraceLevel.Error)
                {
                    CreationTime = DateTime.Now,
                    Message = "Failed to enable module. " + module.Class + Environment.NewLine +
                            ex.Message,
                };
                _host.Connection.Insert(GXInsertArgs.Insert(error));
            }
            return true;
        }

        ///<inheritdoc />
        public bool DeleteModule(ClaimsPrincipal user, GXModule module)
        {
            try
            {
                GXModuleInfo? info = _modulesService.DeleteModule(user, _serviceProvider, module);
                if (info != null)
                {
                    if (info.Module != null)
                    {
                        info.Module.Stop(_serviceProvider);
                    }
                    foreach (var it in info.AssemblyLoadContext.Assemblies)
                    {
                        _partManager.ApplicationParts.Remove(new AssemblyPart(it));
                    }
                    // Notify change
                    _actionDescriptorChangeProvider.TokenSource.Cancel();
                    info.Clear();
                    return info.Restart;
                }
                return false;
            }
            catch (Exception ex)
            {
                //Disable module if there is an error.
                module.Active = false;
                module.Updated = DateTime.Now;
                _host.Connection.Update(GXUpdateArgs.Update(module, c => new { c.Active, c.Updated }));
                var error = new GXModuleLog(TraceLevel.Error)
                {
                    CreationTime = DateTime.Now,
                    Message = "Failed to disable module. " + module.Class + Environment.NewLine +
                            ex.Message,
                };
                _host.Connection.Insert(GXInsertArgs.Insert(error));
            }
            return true;
        }

        ///<inheritdoc />
        public bool DisableModule(
            ClaimsPrincipal user, GXModule module)
        {
            try
            {
                GXModuleInfo? info = _modulesService.UnloadModule(module.Id);
                if (info != null)
                {
                    if (info.Module != null)
                    {
                        info.Module.Stop(_serviceProvider);
                    }
                    if (info.AssemblyLoadContext != null)
                    {
                        foreach (var it in info.AssemblyLoadContext.Assemblies)
                        {
                            _partManager.ApplicationParts.Remove(new AssemblyPart(it));
                        }
                        // Notify change
                        _actionDescriptorChangeProvider.TokenSource.Cancel();
                    }
                    return info.Restart;
                }
                return false;
            }
            catch (Exception ex)
            {
                //Disable module if there is an error.
                module.Active = false;
                module.Updated = DateTime.Now;
                _host.Connection.Update(GXUpdateArgs.Update(module, c => new { c.Active, c.Updated }));
                var error = new GXModuleLog(TraceLevel.Error)
                {
                    CreationTime = DateTime.Now,
                    Message = "Failed to disable module. " + module.Class + Environment.NewLine +
                            ex.Message,
                };
                _host.Connection.Insert(GXInsertArgs.Insert(error));
            }
            return true;
        }

        ///<inheritdoc />
        public async Task ExecuteAsync(
            ClaimsPrincipal user,
            GXModule module,
            string? instanceSettings)
        {
            try
            {
                await _modulesService.ExecuteAsync(
                    user,
                    module.Id,
                    _serviceProvider,
                    module.Settings,
                    instanceSettings);

                var repository = _serviceProvider.GetService<IModuleLogRepository>();
                if (repository != null)
                {
                    var log = new GXModuleLog(TraceLevel.Info)
                    {
                        Module = module,
                        CreationTime = DateTime.Now,
                        Message = "Scheduled module executed.",
                    };
                    await repository.AddAsync(user, new GXModuleLog[] { log });
                }
            }
            catch (Exception ex)
            {
                var repository = _serviceProvider.GetService<IModuleLogRepository>();
                if (repository != null)
                {
                    var error = new GXModuleLog(TraceLevel.Error)
                    {
                        Module = module,
                        CreationTime = DateTime.Now,
                        Message = "Failed to execute module. " + module.Class + Environment.NewLine +
                                ex.Message,
                    };
                    await repository.AddAsync(user, new GXModuleLog[] { error });
                }
            }
        }
    }
}
