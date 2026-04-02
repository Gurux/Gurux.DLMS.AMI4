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

using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Module
{
    /// <summary>
    /// Module service methods.
    /// </summary>
    sealed class GXModuleService : IGXModuleService
    {
        private readonly IGXHost _host;
        private readonly ApplicationPartManager _parts;
        private readonly PluginActionDescriptorChangeProvider _change;
        private readonly IServiceProvider _rootServices;
        private readonly object _gate = new();
        private readonly Dictionary<string, LoadedPlugin> _loaded = new();
        private readonly string _contentRootPath;

        public GXModuleService(IGXHost host,
            IServiceProvider rootServices,
            ApplicationPartManager parts,
            PluginActionDescriptorChangeProvider change,
            IHostEnvironment env)
        {
            _host = host;
            _parts = parts;
            _change = change;
            _contentRootPath = env.ContentRootPath;
            _rootServices = rootServices;
            GXSelectArgs args = GXSelectArgs.Select<GXModule>(s => new { s.Id, s.FileName, s.Version }, w => w.Active == true);
            var modules = host.Connection.Select<GXModule>(args);
            foreach (var module in modules)
            {
                try
                {
                    string path = Path.Combine(_contentRootPath, "Modules", module.Id, module.Version, module.FileName);
                    LoadModulesAsync(null, module.Id, _rootServices, path, null).Wait();
                }
                catch (Exception)
                {
                    // Ignore errors during startup.
                }
            }
        }

        /// <inheritdoc/>
        public async Task<List<GXModule>> AddModuleAsync(ClaimsPrincipal? user, string compressedFile)
        {
            List<GXModule> modules = new List<GXModule>();
            //Unzip the file.
            string tempFolder = Path.Combine(Path.GetDirectoryName(compressedFile), Guid.NewGuid().ToString());
            ZipFile.ExtractToDirectory(compressedFile, tempFolder);
            foreach (string fileName in Directory.GetFiles(tempFolder, "*.dll", SearchOption.AllDirectories))
            {
                modules.AddRange(await LoadModulesAsync(user, null, _rootServices, fileName, compressedFile));
            }
            return modules;
        }

        /// <inheritdoc/>
        public async Task<bool> EnableModuleAsync(ClaimsPrincipal? user, GXModule module)
        {
            GXSelectArgs args = GXSelectArgs.Select<GXModule>(s => new
            {
                s.Id,
                s.FileName
            }, w => w.Id == module.Id);
            var mod = _host.Connection.Select<GXModule>(args).SingleOrDefault();
            string path = Path.Combine(_contentRootPath, "Modules", module.Id, module.Version, module.FileName);
            await LoadModulesAsync(user, module.Id, _rootServices, path, null);
            return false;
        }


        /// <inheritdoc/>
        public Task<bool> DisableModuleAsync(ClaimsPrincipal? user, GXModule module)
        {
            UnloadModule(_rootServices, module.Id);
            return Task.FromResult(false);
        }

        /// <inheritdoc/>
        public Task UpdateModuleSettingsAsync(GXModule module)
        {
            //MIKKO
            // throw new NotImplementedException();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public bool DeleteModule(GXModule module)
        {
            UnloadModule(_rootServices, module.Id);
            if (module.Version == null)
            {
                module = _host.Connection.SelectById<GXModule>(module.Id);
            }
            string path = Path.Combine(_contentRootPath, "Modules", module.Id, module.Version);
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                return false;
            }
            catch (Exception)
            {
                //Unable to delete module files. Require restart.
                return true;
            }
        }

        /// <inheritdoc/>
        public Task ExecuteAsync(GXModule module, string? settings)
        {
            throw new NotImplementedException();
        }

        private async Task<List<GXModule>> LoadModulesAsync(ClaimsPrincipal? user,
            string? id,
            IServiceProvider rootServices, string path,
            string? zipFile)
        {
            List<GXModule> modules = new List<GXModule>();
            using var scope = rootServices.CreateScope();
            var sp = scope.ServiceProvider;
            var moduleLogRepository = sp.GetRequiredService<IModuleLogRepository>();
            try
            {
                var fullPath = Path.GetFullPath(path);
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException("Module file not found.", fullPath);
                }
                lock (_gate)
                {
                    //Id is null when new module is added.
                    if (id != null && _loaded.ContainsKey(id))
                    {
                        return null;
                    }
                    var alc = new PluginLoadContext(fullPath);
                    var asm = alc.LoadFromAssemblyPath(fullPath);
                    var part = new AssemblyPart(asm);
                    _parts.ApplicationParts.Add(part);
                    if (id != null)
                    {
                        _loaded[id] = new LoadedPlugin(fullPath, alc, part);
                        _change.NotifyChanges();
                    }
                    var types = asm.GetTypes();
                    var allDefs = new List<AmiEndpointDefinition>();
                    foreach (var type in asm.GetTypes()
                                 .Where(t => typeof(IAmiModule).IsAssignableFrom(t)))
                    {
                        var module = (IAmiModule?)ActivatorUtilities.CreateInstance(sp, type);
                        if (id == null)
                        {
                            id = module?.Id;
                        }
                        if (id != null && !_loaded.ContainsKey(id))
                        {
                            _loaded[id] = new LoadedPlugin(fullPath, alc, part);
                            _change.NotifyChanges();
                        }
                        if (!string.IsNullOrEmpty(id) && module != null)
                        {
                            GXModule mod;
                            //If new module is added.
                            if (user != null)
                            {
                                mod = new GXModule(id);
                                if (module.CanSchedule)
                                {
                                    mod.Type = mod.Type.GetValueOrDefault() | ModuleType.Schedule;
                                }
                                mod.CreationTime = DateTime.Now;
                                mod.Name = module.Name;
                                mod.FileName = Path.GetFileName(path);
                                mod.Description = module.Description;
                                if (module.Extension != null)
                                {
                                    mod.Type = mod.Type.GetValueOrDefault() | ModuleType.Extension;
                                }
                                if (module.Configuration != null)
                                {
                                    mod.Type = mod.Type.GetValueOrDefault() | ModuleType.Settings;
                                }
                                FileVersionInfo info = FileVersionInfo.GetVersionInfo(asm.Location);
                                mod.Version = info.FileVersion;
                                if (string.IsNullOrEmpty(mod.Version))
                                {
                                    throw new ArgumentException("Invalid module version.");
                                }
                                if (!string.IsNullOrEmpty(module.Icon))
                                {
                                    // Add module icon.
                                    var args2 = GXSelectArgs.SelectAll<GXAppearance>(w => w.Id == mod.Id && w.ResourceType == (byte)ResourceType.Image);
                                    var item = _host.Connection.SingleOrDefault<GXAppearance>(args2);
                                    if (item?.Id == null)
                                    {
                                        var it = new GXAppearance()
                                        {
                                            Id = mod.Id,
                                            ResourceType = (byte)ResourceType.Image,
                                            Value = module.Icon,
                                            Creator = mod.Creator,
                                            CreationTime = mod.CreationTime
                                        };
                                        _host.Connection.Insert(GXInsertArgs.Insert(it));
                                    }
                                }

                                //Check if the module is already installed.
                                var existing = _host.Connection.SelectById<GXModule>(module.Id);
                                if (existing != null &&
                                    (existing.Status & ModuleStatus.Installed) != 0 &&
                                    existing.Version == mod.Version)
                                {
                                    throw new Exception(string.Format("{0} module version {1} already installed.", mod.Id, mod.Version));
                                }
                                mod.CreationTime = DateTime.Now;
                                mod.Creator = new GXUser()
                                {
                                    Id = ServerHelpers.GetUserId(user)
                                };
                                //Add module to default module groups.
                                ListModuleGroups request = new ListModuleGroups()
                                {
                                    Filter = new GXModuleGroup() { Default = true }
                                };
                                var moduleGroupRepository = sp.GetRequiredService<IModuleGroupRepository>();
                                //MIKKO mod.ModuleGroups?.AddRange(await moduleGroupRepository.ListAsync(request, null));
                                //Copy module file to modules folder.
                                string installPath = Path.Combine(_contentRootPath, "Modules", mod.Id, mod.Version);
                                if (!Directory.Exists(installPath))
                                {
                                    Directory.CreateDirectory(installPath);
                                }
                                var source = Path.GetDirectoryName(path);
                                ZipFile.ExtractToDirectory(zipFile, installPath, true);
                                // Load existing module.
                                var tmp = _host.Connection.SelectById<GXModule>(module.Id);
                                if (tmp != null)
                                {
                                    tmp.Version = mod.Version;
                                    tmp.Name = mod.Name;
                                    tmp.FileName = mod.FileName;
                                    tmp.Description = mod.Description;
                                    mod.Type = mod.Type;
                                    mod = tmp;
                                    mod.Status = ModuleStatus.CustomBuild;
                                    GXUpdateArgs args = GXUpdateArgs.Update(mod);
                                    args.Exclude<GXModule>(e => new { e.Creator, e.CreationTime });
                                    _host.Connection.Update(args);
                                }
                                else
                                {
                                    mod.Status = ModuleStatus.CustomBuild;
                                    GXInsertArgs args = GXInsertArgs.Insert(mod);
                                    args.Exclude<GXModule>(e => e.Updated);
                                    _host.Connection.Insert(args);
                                }
                            }
                            else
                            {
                                // Load existing module.
                                mod = _host.Connection.SelectById<GXModule>(module.Id);
                            }
                            var sc = new ServiceCollection();
                            var configuration = sp.GetRequiredService<IConfiguration>();
                            //Add crypto service. That contains encryption key and it's not share across the application.
                            var crypto = sp.GetRequiredService<IAmiCryptoService>();
                            sc.AddSingleton(crypto);
                            var logger = sp.GetRequiredService<ILoggerFactory>();
                            sc.AddSingleton(logger);
                            var sd = sp.GetRequiredService<EndpointDataSource>();
                            sc.AddSingleton(sd);
                            var http2 = sp.GetRequiredService<HttpClient>();
                            sc.AddSingleton(http2);
                            var cf = sp.GetRequiredService<IHttpClientFactory>();
                            sc.AddSingleton(cf);

                            //Add AMI services.
                            //Mikko  ServerSettings.AddRepositories(sc);
                            module.ConfigureModuleServices(sc, configuration);
                            //MIKKO descriptor.ServiceProvider = sc.BuildServiceProvider();
                            //MIKKO var defs = module.GetEndpoints(descriptor.ServiceProvider);
                            //MIKKO if (defs != null)
                            //MIKKO {
                            //MIKKO allDefs.AddRange(defs);
                            //MIKKO }
                            if (user != null)
                            {
                                //MIKKO module.Install(descriptor.ServiceProvider, module);
                            }
                            //Add custom EndpointDataSource.
                            //MIKKO  endpointRegistry.AddOrUpdate(module.Id, allDefs);
                            //MIKKO module.Start(descriptor.ServiceProvider);
                            //MIKKO await module.StartAsync(descriptor.ServiceProvider);
                            modules.Add(mod);
                            //MIKKO _modules[module.Id] = descriptor;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<GXModuleService>();
                if (id != null)
                {
                    GXModule module = new GXModule(id);
                    if (ex is FileNotFoundException)
                    {
                        // Disable module if module file not found.
                        var moduleRepository = sp.GetRequiredService<IModuleRepository>();
                        module.Active = false;
                        module.Updated = DateTime.Now;
                        await moduleRepository.UpdateAsync(module, w => new { w.Active, w.Updated });
                    }
                    if (user == null)
                    {
                        await moduleLogRepository.AddAsync("Load", module, ex);
                    }
                    else
                    {
                        await moduleLogRepository.AddAsync("Install", module, ex);
                    }
                }
                logger.LogError(ex, "Error loading module from {Path}", path);
            }
            return modules;
        }

        public void UnloadModule(IServiceProvider services, string id)
        {
        }

        private sealed record LoadedPlugin(string Path, PluginLoadContext LoadContext, AssemblyPart Part);
    }
}
