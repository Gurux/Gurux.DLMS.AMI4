namespace Gurux.DLMS.AMI.Client.Rest;

/// <summary>The published Gurux.DLMS.AMI tutorial playlist.</summary>
public sealed class GXVideoTutorialFeed
{
    public string? PlaylistId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool Stale { get; set; }
    public GXVideoTutorial[]? Videos { get; set; } = [];
}

/// <summary>A tutorial identified by its YouTube video ID.</summary>
public sealed class GXVideoTutorial
{
    public string VideoId { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public int Position { get; set; }

    /// <summary>Only YouTube identifiers can be used to construct player and image URLs.</summary>
    public bool IsPlayable => VideoId is { Length: 11 } && VideoId.All(c =>
        c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or '-');
}