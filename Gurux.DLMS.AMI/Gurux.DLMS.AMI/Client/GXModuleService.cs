using Microsoft.AspNetCore.Components;
using System.Reflection;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Net.Http.Json;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Client.Helpers;

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
        /// Add new assembly.
        /// </summary>
        /// <param name="name">Assembly name.</param>
        /// <param name="assemblies">Assembly list.</param>
        public void AddAssemblies(string name, List<Assembly> assemblies)
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
                        if (typeof(IGXDeviceSettings).IsAssignableFrom(it))
                        {
                            meterSettings.Add(it);
                        }
                        else if (typeof(IGXObjectSettings).IsAssignableFrom(it))
                        {
                            objectSettings.Add(it);
                        }
                        else if (typeof(IGXAttributeSettings).IsAssignableFrom(it))
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
                _logger.LogError(ex.Message);
            }
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
        public async Task GetModule(HttpClient Http, GXModule module)
        {
            List<Assembly> assemblies = new List<Assembly>();
            ModuleConfigurationUIResponse ret2 = await Http.PostAsJson<ModuleConfigurationUIResponse>("api/Module/ModuleUI", new ModuleConfigurationUI() { Name = module.Id });
            foreach (string it in ret2.Modules)
            {
                lock (this)
                {
                    Assembly asm = Assembly.Load(Convert.FromBase64String(it));
                    if (asm == null)
                    {
                        throw new Exception("Failed to load assembly.");
                    }
                    assemblies.Add(asm);
                }
            }
            AddAssemblies(module.Id, assemblies);
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
                            _logger.LogError(ex.Message);
                        }
                    }
                }
            }
        }
    }
}
