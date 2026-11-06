namespace Gurux.DLMS.AMI.Agent.Shared;

/// <summary>Limits durable raw notification delivery without changing transport payloads.</summary>
public sealed class GXRawDeliveryOptions
{
    public int MaxPendingRecords { get; set; } = 10000;
    public long MaxPendingBytes { get; set; } = 268435456;
    public long MinFreeBytes { get; set; } = 67108864;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan ShutdownFlushBudget { get; set; } = TimeSpan.FromSeconds(5);

    internal void Validate()
    {
        if (MaxPendingRecords < 1 || MaxPendingBytes < 1 || MinFreeBytes < 0 || RequestTimeout <= TimeSpan.Zero ||
            RetryDelay <= TimeSpan.Zero || ShutdownFlushBudget <= TimeSpan.Zero ||
            RequestTimeout.TotalMilliseconds > int.MaxValue || RetryDelay.TotalMilliseconds > int.MaxValue || ShutdownFlushBudget.TotalMilliseconds > int.MaxValue)
        {
            throw new ArgumentException("Raw delivery storage limits must be positive, free space nonnegative, and timeouts finite positive timer intervals.");
        }
    }
}
