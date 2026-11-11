namespace Gurux.DLMS.AMI.Client.Rest;

/// <summary>Selects a saved script for community publication or acknowledges its review result.</summary>
public sealed class GXPublishCommunityScript
{
    /// <summary>Local script ID when publishing; submission ID when acknowledging.</summary>
    public Guid Id { get; set; }
}

/// <summary>The submitting user's review status. Remote tracking credentials remain on the server.</summary>
public sealed class GXScriptCommunitySubmission
{
    /// <summary>Local submission ID.</summary>
    public Guid Id { get; set; }
    /// <summary>Name of the submitted script.</summary>
    public string ScriptName { get; set; } = string.Empty;
    /// <summary>Pending, Publishing, Approved, Rejected or Failed.</summary>
    public string Status { get; set; } = "Pending";
    /// <summary>Published catalog version when approved.</summary>
    public string? Version { get; set; }
    /// <summary>Safe explanation when a submission could not be tracked.</summary>
    public string? Message { get; set; }
}
