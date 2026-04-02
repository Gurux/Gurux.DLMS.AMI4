using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// //Workaround for AntiforgeryToken bug:
///https://github.com/dotnet/aspnetcore/issues/58822
/// </summary>
public sealed class PersistAntiforgeryState : ComponentBase, IDisposable
{
    private const string PersistenceKey = $"__internal__{nameof(AntiforgeryRequestToken)}";
    private PersistingComponentStateSubscription _subscription;

    [Inject] PersistentComponentState PersistentState { get; set; }
    [Inject] AntiforgeryStateProvider AntiforgeryProvider { get; set; }

    protected override void OnInitialized()
    {
        _subscription = PersistentState.RegisterOnPersisting(() =>
        {
            PersistentState.PersistAsJson(PersistenceKey, AntiforgeryProvider.GetAntiforgeryToken());
            return Task.CompletedTask;
        }, RenderMode.InteractiveAuto);
    }

    public void Dispose() => _subscription.Dispose();
}