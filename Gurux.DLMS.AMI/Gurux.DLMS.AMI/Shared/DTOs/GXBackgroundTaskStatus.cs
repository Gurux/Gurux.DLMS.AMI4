using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs;

/// <summary>An immutable snapshot of a background execution.</summary>
public sealed record GXBackgroundTaskStatus
{
    /// <summary>Gets the unique identifier of this execution.</summary>
    public Guid RunId { get; init; }
    /// <summary>Gets the identifier of the background task being executed.</summary>
    public string TaskId { get; init; } = string.Empty;
    /// <summary>Gets the current execution state.</summary>
    public BackgroundTaskState State { get; init; }
    /// <summary>Gets the time the execution was queued.</summary>
    public DateTimeOffset QueuedAt { get; init; }
    /// <summary>Gets the time execution started, or null if it has not started.</summary>
    public DateTimeOffset? StartedAt { get; init; }
    /// <summary>Gets the time execution finished, or null if it has not finished.</summary>
    public DateTimeOffset? FinishedAt { get; init; }
    /// <summary>Gets the execution error description, if available.</summary>
    public string? Error { get; init; }

    /// <summary>Used for server-side authorization; excluded from the HTTP response.</summary>
    [JsonIgnore]
    public string OwnerId { get; init; } = string.Empty;
}
