using System.Reflection;

namespace Gurux.DLMS.AMI.Module.Testing;

public class RepositoryProxy : DispatchProxy
{
    public Func<MethodInfo, object?[]?, object?> InvokeMethod = null!;
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => InvokeMethod(targetMethod!, args);
    public static T Create<T>(Func<MethodInfo, object?[]?, object?> action) where T : class
    {
        var instance = Create<T, RepositoryProxy>();
        ((RepositoryProxy)(object)instance).InvokeMethod = action;
        return instance;
    }
}
