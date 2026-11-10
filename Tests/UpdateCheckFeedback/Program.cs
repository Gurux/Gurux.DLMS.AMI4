using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Shared.DTOs;
using System.Net;
using System.Net.Http.Json;
using Bunit;
using Gurux.DLMS.AMI.Client.Pages.Admin;
using Gurux.DLMS.AMI.Client.Rest;
using Gurux.UI.Components;
using Microsoft.Extensions.DependencyInjection;

using var context = new BunitContext();
context.JSInterop.Mode = JSRuntimeMode.Loose;
context.Services.AddSingleton<IGXMenuService>(new GXMenuService());
var progress = new GXProgressService();
var notifications = new GXNotificationService();
context.Services.AddSingleton<IGXProgress>(progress).AddSingleton<IGXNotificationService>(notifications);
var api = new UpdatesApi();
context.Services.AddSingleton(new HttpClient(api) { BaseAddress = new Uri("https://example.test/") });
context.ComponentFactories.AddStub<StaticDeployment>();
context.ComponentFactories.AddStub<UpdateTable>();
context.ComponentFactories.AddStub<ScriptUpdates>();
context.ComponentFactories.AddStub<LocalizationUpdates>();
var page = context.Render<Updates>();
page.WaitForAssertion(() => Check(!page.Find(".check-manually").HasAttribute("disabled"), "Initial loading must finish."));
var click = page.Find(".check-manually").ClickAsync(new());
page.WaitForAssertion(() => Check(progress.IsBusy, "Manual checks must use IGXProgress."));
Check(api.Checks == 1, "The button must issue one POST check request.");
api.Response.SetResult(new(HttpStatusCode.ServiceUnavailable));
await click;
page.WaitForAssertion(() => Check(notifications.Notifications.Any(n => n.Key == "Pages/Admin/Updates.Check"), "A failed check must use IGXNotificationService."));
Check(!page.Find(".check-manually").HasAttribute("disabled"), "A failed check must allow retry.");
api.Response = new(TaskCreationOptions.RunContinuationsAsynchronously);
var retry = page.Find(".check-manually").ClickAsync(new());
page.WaitForAssertion(() => Check(!notifications.Notifications.Any(n => n.Key == "Pages/Admin/Updates.Check"), "Retry must clear the previous failure."));
api.Response.SetResult(new(HttpStatusCode.OK) { Content = JsonContent.Create(new GXAvailableUpdatesResponse
{
    Checked = DateTimeOffset.UtcNow,
    Core = new() { AvailableVersion = "4.1.2611.1002" }
}) });
await retry;
Check(api.Checks == 2, "Retry must issue a fresh request, regardless of the previous check time.");
Check(!notifications.Notifications.Any(n => n.Key == "Pages/Admin/Updates.Check") && !progress.IsBusy, "A successful check must clear pending feedback.");
api.Response = new(TaskCreationOptions.RunContinuationsAsynchronously);
var stale = page.Find(".check-manually").ClickAsync(new());
api.Response.SetResult(new(HttpStatusCode.OK) { Content = JsonContent.Create(new GXAvailableUpdatesResponse
{
    Checked = DateTimeOffset.UtcNow.AddHours(-11),
    Core = new() { AvailableVersion = "4.1.2611.913" }
}) });
await stale;
page.WaitForAssertion(() => Check(notifications.Notifications.Any(n => n.Key == "Pages/Admin/Updates.Check"),
    "An unchanged or older check timestamp must not be silently accepted as a completed check."));
Check(page.FindAll(".update-check-progress, .update-check-error").Count == 0, "Feedback must use shared services without inline status or error text.");
Check(!progress.IsBusy, "Failed checks must release progress.");
Console.WriteLine("PASS manual check progress/notifications, retry, stale-result rejection and progress disposal.");

static void Check(bool condition, string message)
{
    if (!condition)
    {
        throw new Exception(message);
    }
}

sealed class UpdatesApi : HttpMessageHandler
{
    internal int Checks;
    internal TaskCompletionSource<HttpResponseMessage> Response = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        if (request.RequestUri!.AbsolutePath == "/api/Update/Check")
        {
            if (request.Method != HttpMethod.Post)
            {
                throw new Exception("Checks must use POST.");
            }
            ++Checks;
            return Response.Task.WaitAsync(token);
        }
        object data = request.RequestUri.AbsolutePath switch
        {
            "/api/Configuration/GetTableChanges" => Array.Empty<GXConfigurationTableChange>(),
            "/api/Update/List" => new GXAvailableUpdatesResponse { Checked = DateTimeOffset.UtcNow.AddHours(-11), Core = new() { AvailableVersion = "4.1.2611.913" } },
            _ => throw new Exception("Unexpected request " + request.RequestUri)
        };
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(data, data.GetType()) });
    }
}
