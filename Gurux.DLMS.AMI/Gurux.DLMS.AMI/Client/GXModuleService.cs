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

using Microsoft.AspNetCore.Components;
using System.Reflection;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Module;
using System.Runtime.Loader;
using Gurux.DLMS.AMI.Shared;

namespace Gurux.DLMS.AMI.Client
{
    public class GXModuleService : ComponentBase
    {
        ILogger<GXModuleService> _logger;
        private Dictionary<string, List<Assembly>> _assemblies = new Dictionary<string, List<Assembly>>();
        private Dictionary<string, List<Type>> _meterSettings = new Dictionary<string, List<Type>>();
        private Dictionary<string, List<Type>> _objectSettings = new Dictionary<string, List<Type>>();
        private Dictionary<string, List<Type>> _attributeSettings = new Dictionary<string, List<Type>>();

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXModuleService(ILogger<GXModuleService> logger)
        {
            Assemblies = new List<Assembly>();
            _logger = logger;
        }

        public List<Assembly> Assemblies
        {
            get;
            private set;
        }

        /// <summary>
        /// Get meter UI settings.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, List<Type>> GetMeterSettings()
        {
            return _meterSettings;
        }

        /// <summary>
        /// Get object UI settings.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, List<Type>> GetObjectSettings()
        {
            return _objectSettings;
        }

        /// <summary>
        /// Get object UI settings.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, List<Type>> GetAttributeSettings()
        {
            return _attributeSettings;
        }

        /// <summary>
        /// Modules are load for own assembly load context.
        /// </summary>
        public class GXAssemblyLoadContext : AssemblyLoadContext
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="name">Assembly load context name.</param>
            public GXAssemblyLoadContext(string name) : base(name, true)
            {
            }
        }

        private Assembly? ModuleResolving(AssemblyLoadContext arg1, AssemblyName arg2)
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
            return null;
        }

        /// <summary>
        /// Add new assembly.
        /// </summary>
        /// <param name="name">Assembly name.</param>
        /// <param name="assemblies">Assembly list.</param>
        public bool AddAssemblies(string name, List<Assembly> assemblies)
        {
            lock (Assemblies)
            {
                _assemblies[name] = assemblies;
                Assemblies.AddRange(assemblies);
            }
            try
            {
                //Update module settings.
                List<Type> meterSettings = new List<Type>();
                List<Type> objectSettings = new List<Type>();
                List<Type> attributeSettings = new List<Type>();
                foreach (var asm in assemblies)
                {
                    foreach (Type it in asm.GetExportedTypes())
                    {
                        if (it.IsAbstract || !it.IsClass || it.FullName == null)
                        {
                            continue;
                        }
                        if (typeof(IAmiExtendedDeviceSettingsUI).IsAssignableFrom(it))
                        {
                            meterSettings.Add(it);
                        }
                        else if (typeof(IAmiExtendedObjectSettingsUI).IsAssignableFrom(it))
                        {
                            objectSettings.Add(it);
                        }
                        else if (typeof(IAmiExtendedAttributeSettingsUI).IsAssignableFrom(it))
                        {
                            attributeSettings.Add(it);
                        }
                    }
                    if (meterSettings.Any())
                    {
                        _meterSettings[name] = meterSettings;
                    }
                    if (objectSettings.Any())
                    {
                        _objectSettings[name] = objectSettings;
                    }
                    if (attributeSettings.Any())
                    {
                        _attributeSettings[name] = attributeSettings;
                    }
                }
            }
            catch (Exception ex)
            {
                _assemblies.Remove(name);
                Assemblies.RemoveAll(w => assemblies.Contains(w));
                _logger.LogError("AddAssemblies {0}", ex.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Remove assembly.
        /// </summary>
        /// <param name="name">Assembly name.</param>
        public void RemoveAssembly(string name)
        {
            lock (Assemblies)
            {
                if (_assemblies.TryGetValue(name, out var list))
                {
                    Assemblies.RemoveAll(w => list.Contains(w));
                }
                _assemblies.Remove(name);
            }
        }

        /// <summary>
        /// Check is module already loaded.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        bool IsLoaded(string name)
        {
            lock (Assemblies)
            {
                return _assemblies.ContainsKey(name);
            }
        }

        /// <summary>
        /// Load module assembly into app domain.
        /// </summary>
        /// <param name="Http">Http client.</param>
        /// <param name="module">Module to load.</param>
        public async Task<Assembly[]> GetModule(HttpClient Http, GXModule module)
        {
            lock (Assemblies)
            {
                List<Assembly>? list = null;
                if (_assemblies.TryGetValue(module.Id, out list))
                {
                    //If assembly is already loaded.
                    return list.ToArray();
                }
            }
            try
            {
                List<Assembly> assemblies = new List<Assembly>();
                ModuleConfigurationUIResponse ret2 = await Http.PostAsJson<ModuleConfigurationUIResponse>("api/Module/ModuleUI", new ModuleConfigurationUI() { Name = module.Id });
                if (ret2.Modules != null)
                {
                    AssemblyLoadContext alc = new AssemblyLoadContext(module.Id);
                    alc.Resolving += ModuleResolving;
                    foreach (string it in ret2.Modules)
                    {
                        using MemoryStream ms = new MemoryStream();
                        ms.Write(Convert.FromBase64String(it));
                        ms.Position = 0;
                        Assembly asm = alc.LoadFromStream(ms);
                        if (asm == null)
                        {
                            throw new Exception("Failed to load assembly.");
                        }
                        assemblies.Add(asm);
                    }
                    if (!AddAssemblies(module.Id, assemblies))
                    {
                        return new Assembly[0];
                    }
                }
                return assemblies.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError("GetModule failed {0}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Load modules from the server.
        /// </summary>
        /// <param name="Http"></param>
        public async Task LoadModules(HttpClient Http)
        {
            ListModules req = new ListModules();
            ListModulesResponse ret = await Http.PostAsJson<ListModulesResponse>("api/Module/List", req);
            foreach (GXModule module in ret.Modules)
            {
                if (!IsLoaded(module.Id))
                {
                    try
                    {
                        var mod = (await Http.GetAsJsonAsync<GetModuleResponse>(string.Format("api/Module?id={0}", module.Id)))?.Item;
                        List<Assembly> assemblies = new List<Assembly>();
                        foreach (var it in mod.Assemblies)
                        {
                            var asm = Assembly.Load(it.FileName);
                            assemblies.Add(asm);
                        }
                        AddAssemblies(module.Id, assemblies);
                    }
                    catch (FileNotFoundException)
                    {
                        try
                        {
                            await GetModule(Http, module);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("LoadModules failed", ex.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("LoadModules failed", ex.Message);
                    }
                }
            }
        }
    }
}
