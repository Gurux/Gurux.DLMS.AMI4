#pragma warning disable BL0006 // Inspect generated component output without mounting the application.
using System.Reflection;
using Gurux.DLMS.AMI.Client.Pages.Admin;
using Gurux.DLMS.AMI.Client.Rest;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ModuleRuntime.Tests;

[TestClass]
public sealed class DeploymentUiTests
{
    [TestMethod]
    [DataRow("Systemd")]
    [DataRow("Linux")]
    [DataRow("Container")]
    public async Task UnconfiguredLinuxInstallationExplainsWhyThereIsNoUpdateButton(string environment)
    {
        await using var component = new StaticDeployment();
        Set(component, "_status", new GXDeploymentResponse { HostingEnvironment = environment });
        var text = Render(component);
        StringAssert.Contains(text, "not configured");
        Assert.IsFalse(text.Contains("<button>", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task AConfiguredSystemdAgentCanUpdateTheCompleteApplication()
    {
        await using var component = new StaticDeployment();
        Set(component, "_status", new GXDeploymentResponse
        {
            HostingEnvironment = "Systemd", Configured = true, AgentAvailable = true,
            IncludesBackend = true, CurrentVersion = "1.0.0", ApiVersion = "1.0.0"
        });
        Set(component, "_available", "2.0.0");
        var text = Render(component);
        StringAssert.Contains(text, "Update AMI and user interface");
        StringAssert.Contains(text, "brief outage");
        StringAssert.Contains(text, "Installed AMI version");
    }

    [TestMethod]
    public async Task EnvironmentDetectionAloneNeverEnablesAnUpdate()
    {
        await using var component = new StaticDeployment();
        Set(component, "_status", new GXDeploymentResponse
        {
            HostingEnvironment = "Systemd", Configured = true, AgentAvailable = false,
            IncludesBackend = true, CurrentVersion = "1.0.0", ApiVersion = "1.0.0"
        });
        Set(component, "_available", "2.0.0");
        var text = Render(component);
        StringAssert.Contains(text, "deployment agent is unavailable");
        Assert.IsFalse(text.Contains("Update AMI and user interface", StringComparison.Ordinal));
    }

    private static void Set(StaticDeployment component, string field, object value)
    {
        typeof(StaticDeployment).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(component, value);
    }

    private static string Render(StaticDeployment component)
    {
        var text = new System.Text.StringBuilder();
        void Visit(RenderFragment fragment)
        {
            using var builder = new RenderTreeBuilder();
            fragment(builder);
            var range = builder.GetFrames();
            foreach (var frame in range.Array.Take(range.Count).ToArray())
            {
                if (frame.FrameType == RenderTreeFrameType.Text)
                {
                    text.Append(frame.TextContent);
                }
                if (frame.FrameType == RenderTreeFrameType.Markup)
                {
                    text.Append(frame.MarkupContent);
                }
                if (frame.FrameType == RenderTreeFrameType.Element)
                {
                    text.Append('<').Append(frame.ElementName).Append('>');
                }
                if (frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeValue is RenderFragment child)
                {
                    Visit(child);
                }
            }
        }
        Visit(builder => typeof(StaticDeployment).GetMethod("BuildRenderTree", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(component, new object[] { builder }));
        return text.ToString();
    }
}
