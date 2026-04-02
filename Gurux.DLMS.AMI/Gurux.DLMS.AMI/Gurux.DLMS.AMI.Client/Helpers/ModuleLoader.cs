using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Reflection;

namespace Gurux.DLMS.AMI.Client.Helpers
{

    public sealed class ModuleLoader
    {
        private readonly HttpClient _http;

        private readonly Dictionary<string, GXModule> _modules = new();
        private readonly Dictionary<string, Assembly> _assembliesByUrl = new();
        private readonly Dictionary<string, Type> _componentTypesById = new();

        public ModuleLoader(HttpClient http)
        {
            _http = http;
        }

        public IReadOnlyCollection<GXModule> AddIns => _modules.Values;

        public async Task RefreshAsync()
        {
            //Get extension modules.
            ListModules req = new ListModules()
            {
                Filter = new GXModule()
                {
                    Active = true
                }
            };
            _modules.Clear();
            var ret = await _http.PostAsJson<ListModulesResponse>("api/Module/List", req);
            if (ret.Modules != null)
            {
                foreach (var it in ret.Modules)
                {
                    _modules[it.Id] = it;
                }
            }
        }

        public async Task<Type?> GetComponentTypeAsync(string id)
        {
            if (_componentTypesById.TryGetValue(id, out var cached))
            {
                return cached;
            }

            if (!_modules.TryGetValue(id, out var module))
            {
                return null;
            }
            // Load only once.
            if (!_assembliesByUrl.TryGetValue(module.Id, out var asm))
            {
                var bytes = await _http.GetByteArrayAsync("/api/module/load?id=" + id);
                asm = Assembly.Load(bytes);
                _assembliesByUrl[id] = asm;
            }
            var type = asm.GetTypes().Where(t => typeof(IAmiModule).IsAssignableFrom(t)).SingleOrDefault();
            if (type is null)
            {
                return null;
            }
            _componentTypesById[id] = type;
            return type;
        }
    }

}
