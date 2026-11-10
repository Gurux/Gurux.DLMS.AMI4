using System.Reflection;
using System.Text.Json;
using Gurux.DLMS.AMI.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProfilePage = Gurux.DLMS.AMI.Components.Account.Pages.Manage.Index;

namespace ModuleRuntime.Tests;

[TestClass]
public sealed class ProfileRenderingTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ProfilePictureSelectorRendersOnServerWithoutBrowserStorage(bool selected)
    {
        // Seed the loaded profile so this test exercises the page's actual rendering
        // independently of Identity and database initialization.
        const BindingFlags members = BindingFlags.Instance | BindingFlags.NonPublic;
        var page = new ProfilePage();
        var inputProperty = typeof(ProfilePage).GetProperty("Input", members)!;
        var input = Activator.CreateInstance(inputProperty.PropertyType, nonPublic: true)!;
        var imageId = Guid.NewGuid();
        inputProperty.PropertyType.GetProperty("ProfilePictureIndex")!.SetValue(input, 1);
        inputProperty.PropertyType.GetProperty("ProfilePictureOptions")!.SetValue(input, JsonSerializer.Serialize(new Guid?[] { null, imageId }));
        if (selected)
        {
            inputProperty.PropertyType.GetProperty("ProfilePictureId")!.SetValue(input, imageId);
        }
        inputProperty.SetValue(page, input);
        typeof(ProfilePage).GetField("profilePictureOptions", members)!.SetValue(page, new Guid?[] { null, imageId });
        typeof(ProfilePage).GetField("images", members)!.SetValue(page, new[] { new GXAppearance { Id = imageId, Name = "My picture", Value = "bi-person-circle" } });
        var render = typeof(ProfilePage).GetMethod("BuildRenderTree", members)!;
        RenderFragment content = builder => render.Invoke(page, [builder]);

        await using var services = new ServiceCollection().AddLogging()
            .AddSingleton<NavigationManager, ProfileNavigation>().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var result = await renderer.RenderComponentAsync<ProfileHost>(ParameterView.FromDictionary(
                new Dictionary<string, object?> { [nameof(ProfileHost.Content)] = content }));
            return result.ToHtmlString();
        });
        StringAssert.Contains(html, "profile-picture");
        StringAssert.Contains(html, "My picture");
        StringAssert.Contains(html, "value=" + (char)34 + "0" + (char)34);
        StringAssert.Contains(html, "value=" + (char)34 + "1" + (char)34);
        StringAssert.Contains(html, "ProfilePictureOptions");
        if (selected)
        {
            StringAssert.Contains(html, "bi-person-circle");
        }
    }

    [TestMethod]
    [DataRow("GXImage")]
    [DataRow("GXSpinner")]
    [DataRow("GXBreadcrumb")]
    [DataRow("GXMenuBar")]
    [DataRow("GXLocalizedText")]
    [DataRow("GXHelp")]
    [DataRow("GXNotificationsTile")]
    [DataRow("GXToaster")]
    public async Task SharedLayoutUiComponentsRenderWithoutBrowserStorage(string componentName)
    {
        var type = typeof(Gurux.UI.Components.GXImage).Assembly.GetTypes()
            .Single(t => t.Name == componentName && typeof(IComponent).IsAssignableFrom(t));
        await using var services = new ServiceCollection().AddLogging()
            .AddSingleton<NavigationManager, ProfileNavigation>()
            .AddScoped<Gurux.UI.Components.IGXProgress, Gurux.UI.Components.GXProgressService>()
            .AddScoped<Gurux.UI.Components.IGXNotificationService, Gurux.UI.Components.GXNotificationService>()
            .AddScoped<Gurux.UI.Components.IGXMenuService, Gurux.UI.Components.GXMenuService>()
            .BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await renderer.RenderComponentAsync(type, ParameterView.Empty);
        });
    }

    public sealed class ProfileHost : ComponentBase
    {
        [Parameter] public RenderFragment? Content { get; set; }
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<HttpContext>>(0);
            builder.AddComponentParameter(1, "Value", new DefaultHttpContext());
            builder.AddComponentParameter(2, "ChildContent", Content);
            builder.CloseComponent();
        }
    }

    private sealed class ProfileNavigation : NavigationManager
    {
        public ProfileNavigation() => Initialize("https://ami.test/", "https://ami.test/Account/Manage");
        protected override void NavigateToCore(string uri, bool forceLoad) => throw new NotSupportedException();
    }
}
