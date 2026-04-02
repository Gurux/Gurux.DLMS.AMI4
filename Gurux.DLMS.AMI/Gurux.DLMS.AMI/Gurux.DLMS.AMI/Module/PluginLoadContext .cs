using System.Reflection;
using System.Runtime.Loader;

namespace Gurux.DLMS.AMI.Module;

public sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    public readonly List<Assembly> ReferenceAssembliest = new List<Assembly>();

    public PluginLoadContext(string pluginMainAssemblyPath)
        : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginMainAssemblyPath);
        //Gurux.DLMS.AMI asseblies are used instead of added as references to module projects.
        ReferenceAssembliest.Add(typeof(IAmiModule).Assembly);
        ReferenceAssembliest.Add(typeof(Components.Dialog).Assembly);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var asm = ReferenceAssembliest.Where(w => w.GetName().Name == assemblyName.Name).SingleOrDefault();
        if (asm != null)
        {
            return null;
        }
        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path != null ? LoadFromAssemblyPath(path) : null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path != null ? LoadUnmanagedDllFromPath(path) : IntPtr.Zero;
    }
}
