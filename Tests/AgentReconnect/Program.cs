using System.Reflection;
using Gurux.DLMS.AMI.Agent.Shared;
using Gurux.DLMS.AMI.Agent.Worker;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

await ReconnectChecks.RunAsync();

internal static class ReconnectChecks
{
    internal static async Task RunAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSignalR();
        var state = new ReconnectTestState();
        builder.Services.AddSingleton(state);
        await using var app = builder.Build();
        app.MapHub<ReconnectTestHub>("/guruxami");
        await app.StartAsync();
        await using var hub = new HubConnectionBuilder().WithUrl(app.Urls.Single() + "/guruxami",
            options => options.Transports = HttpTransportType.LongPolling).Build();
        await using var worker = new GXAgentWorker();
        using var cancellation = new CancellationTokenSource();
        const BindingFlags members = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(GXAgentWorker).GetMethod("PublishConfiguration", members)!.Invoke(worker,
            [new GXAgentOptions { Address = app.Urls.Single(), Token = "test-token", Id = Guid.NewGuid() }, true]);
        typeof(GXAgentWorker).GetField("_hubConnection", members)!.SetValue(worker, hub);
        typeof(GXAgentWorker).GetField("cancellationToken", members)!.SetValue(worker, cancellation.Token);
        // Stop after registration, before unrelated update checks and poller services.
        typeof(GXAgentWorker).GetField("restartPending", members)!.SetValue(worker, true);
        var closed = typeof(GXAgentWorker).GetMethod("HubConnectionClosed", members)!;
        Task? reconnect = null;
        try
        {
            for (var restart = 0; restart != 2; ++restart)
            {
                state.Attempts = 0;
                reconnect = (Task)closed.Invoke(worker, [new IOException("Server restarted")])!;
                await reconnect.WaitAsync(TimeSpan.FromSeconds(8));
                if (hub.State != HubConnectionState.Connected ||
                    worker.ConnectionState != GXAgentConnectionState.Connected || state.Attempts != 3)
                {
                    throw new Exception("Reconnection must retry registration on the connected hub after transient server failures.");
                }
                await hub.StopAsync();
            }
            // Stopping the worker must interrupt the retry delay promptly.
            reconnect = (Task)closed.Invoke(worker, [new IOException("Server unavailable")])!;
            cancellation.Cancel();
            await reconnect.WaitAsync(TimeSpan.FromSeconds(2));
            Console.WriteLine("PASS repeated SignalR reconnect, transient registration failures and retry cancellation.");
        }
        finally
        {
            cancellation.Cancel();
            if (reconnect != null)
            {
                await reconnect.WaitAsync(TimeSpan.FromSeconds(2));
            }
        }
    }
}

public sealed class ReconnectTestState
{
    public int Attempts;
}

public sealed class ReconnectTestHub(ReconnectTestState state) : Hub
{
    public void RegisterAgent(Guid id)
    {
        if (Interlocked.Increment(ref state.Attempts) <= 2)
        {
            throw new HubException("Registration is temporarily unavailable after restart.");
        }
    }
}