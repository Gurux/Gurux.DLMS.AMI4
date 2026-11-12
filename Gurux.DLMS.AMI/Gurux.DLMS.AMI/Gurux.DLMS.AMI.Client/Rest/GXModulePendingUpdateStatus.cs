namespace Gurux.DLMS.AMI.Client.Rest;

/// <summary>Public status of a prepared module update batch.</summary>
public sealed class GXModulePendingUpdateStatus
{
    public Guid Id { get; set; }
    public string State { get; set; } = "None";
    public DateTimeOffset? PreparedAt { get; set; }
    public string? Error { get; set; }
    public List<GXModulePendingVersion> Modules { get; set; } = [];
}

/// <summary>Separates the running module version from its prepared replacement.</summary>
public sealed record GXModulePendingVersion(string Id, string Name, string? CurrentVersion, string Version);
