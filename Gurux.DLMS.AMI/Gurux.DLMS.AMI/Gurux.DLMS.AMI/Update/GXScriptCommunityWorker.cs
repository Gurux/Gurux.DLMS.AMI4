namespace Gurux.DLMS.AMI.Update;

/// <summary>Refreshes durable receipts without holding a task open during human review.</summary>
public sealed class GXScriptCommunityWorker(IServiceScopeFactory scopes, ILogger<GXScriptCommunityWorker> logger) : BackgroundService
{
    /// <summary>Polls durable receipts once per minute.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<GXScriptCommunityService>().RefreshAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception error)
            {
                logger.LogWarning("Community receipt refresh failed ({ErrorType}).", error.GetType().Name);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
