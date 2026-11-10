using System.Reflection;
using Gurux.DLMS.AMI.Server.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ModuleRuntime.Tests;

[TestClass]
public sealed class KeyManagementCommandsTests
{
    [TestMethod]
    public void ManualLifecycleTransitionsAreNotExposedByTheServer()
    {
        Assert.IsFalse(typeof(KeyManagementController).GetMethods().SelectMany(method => method.GetCustomAttributes<HttpPostAttribute>())
            .Any(route => route.Template is "Transition" or "Activate" or "MarkCompromised" or "DestroyMaterial"));
        Assert.IsNull(typeof(KeyManagementController).Assembly.GetType("Gurux.DLMS.AMI.Server.Services.KeyManagement.GXKeyLifecycleService"));
    }
}
