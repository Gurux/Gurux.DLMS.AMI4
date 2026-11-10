using System.Net;
using System.Reflection;
using Gurux.DLMS.AMI.Client.Pages.Config;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.UI.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

const BindingFlags members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
foreach (bool success in new[] { true, false })
{
    using var menu = new GXMenuService();
    using var handler = new SaveHandler();
    using var http = new HttpClient(handler) { BaseAddress = new Uri("https://ami.test/") };
    var navigation = new TestNavigation();
    var parent = new Configurations();
    typeof(Configurations).GetField("_active", members)!.SetValue(parent, new GXConfiguration { Id = Guid.NewGuid() });
    using var account = new Account();
    void Inject(string name, object value) => typeof(Account).GetProperty(name, members)!.SetValue(account, value);
    Inject("Parent", parent);
    Inject("SharedMenuService", menu);
    Inject("NavigationManager", navigation);
    Inject("Http", http);
    var settings = new AccountSettings();
    typeof(Account).GetField("_settings", members)!.SetValue(account, settings);
    var pageMenu = (GXPageMenu)typeof(Account).GetProperty("MenuService", members)!.GetValue(account)!;
    var context = new EditContext(settings);
    pageMenu.EditContext = context;
    context.NotifyFieldChanged(new FieldIdentifier(settings, nameof(settings.UserCreationType)));
    var method = typeof(Account).GetMethod("OnSave", members)!;
    if (method.ReturnType != typeof(Task))
    {
        throw new Exception("Account Save must return Task so the menu awaits completion.");
    }
    var save = (Task)method.Invoke(account, null)!;
    if (save.IsCompleted || !context.IsModified() || navigation.Count != 0)
    {
        throw new Exception("Pending Save must retain unsaved changes and stay on the account page.");
    }
    navigation.Context = context;
    handler.Complete(success);
    await save;
    if (success && (context.IsModified() || navigation.Count != 1 || navigation.Uri != "https://ami.test/config" || navigation.WasModified))
    {
        throw new Exception("Successful Save must clear unsaved changes before returning to the configuration page.");
    }
    if (!success && (!context.IsModified() || navigation.Count != 0))
    {
        throw new Exception("Failed Save must preserve unsaved changes and remain on the account page.");
    }
}
Console.WriteLine("PASS awaitable account Save, pending state, successful save/navigation and failed-save dirty state.");

sealed class SaveHandler : HttpMessageHandler
{
    private readonly TaskCompletionSource<HttpResponseMessage> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public void Complete(bool success) => response.SetResult(new HttpResponseMessage(success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError)
    {
        Content = new StringContent("{}")
    });
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        if (request.Method != HttpMethod.Post || request.RequestUri!.AbsolutePath != "/api/Configuration/Update")
        {
            throw new Exception("Unexpected save request.");
        }
        return response.Task;
    }
}
sealed class TestNavigation : NavigationManager
{
    public int Count { get; private set; }
    public bool WasModified { get; private set; }
    public EditContext? Context { get; set; }
    public TestNavigation() => Initialize("https://ami.test/", "https://ami.test/config/account?returnUrl=%2Fconfig");
    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        WasModified = Context?.IsModified() == true;
        ++Count;
        Uri = ToAbsoluteUri(uri).AbsoluteUri;
    }
    protected override void NavigateToCore(string uri, NavigationOptions options) => NavigateToCore(uri, options.ForceLoad);
}
