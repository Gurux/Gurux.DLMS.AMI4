namespace Gurux.DLMS.AMI.Client.Pages.Module;

/// <summary>Owns the cancellation and completion of visible health polling.</summary>
public sealed class ModuleHealthPolling : IAsyncDisposable
{
    private CancellationTokenSource? cancellation;
    private readonly List<Task> tasks = [];
    private readonly List<CancellationTokenSource> sources = [];
    private bool disposed;
    public void Start(Func<CancellationToken, Task> refresh)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        Stop();
        cancellation = new CancellationTokenSource();
        for (int pos = tasks.Count - 1; pos >= 0; --pos)
        {
            if (tasks[pos].IsCompleted)
            {
                sources[pos].Dispose();
                sources.RemoveAt(pos);
                tasks.RemoveAt(pos);
            }
        }
        sources.Add(cancellation);
        tasks.Add(RunAsync(refresh, cancellation));
    }
    public void Stop()
    {
        cancellation?.Cancel();
        cancellation = null;
    }
    private static async Task RunAsync(Func<CancellationToken, Task> refresh, CancellationTokenSource source)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
            do
            {
                await refresh(source.Token);
            }
            while (await timer.WaitForNextTickAsync(source.Token));
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested)
        {
        }
    }
    public async ValueTask DisposeAsync()
    {
        disposed = true;
        Stop();
        await Task.WhenAll(tasks);
        tasks.Clear();
        foreach (var source in sources)
        {
            source.Dispose();
        }
        sources.Clear();
    }
}
