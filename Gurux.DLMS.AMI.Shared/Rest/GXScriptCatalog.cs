namespace Gurux.DLMS.AMI.Shared.Rest;

/// <summary>Requests an authorized comparison with one exact catalog release.</summary>
public sealed class PreviewScriptTemplate
{
    /// <summary>Existing script; null imports a new independent instance of the template.</summary>
    public Guid? ScriptId { get; set; }
    /// <summary>Publisher's stable script identifier.</summary>
    public string CatalogId { get; set; } = "";
    /// <summary>Exact retained catalog version.</summary>
    public string Version { get; set; } = "";
}

/// <summary>Comparison data; requesting this never compiles or invokes a script.</summary>
public sealed class ScriptTemplatePreview
{
    /// <summary>Existing local instance, or null for a new import.</summary>
    public Guid? ScriptId { get; set; }
    /// <summary>Catalog identity.</summary>
    public string CatalogId { get; set; } = "";
    /// <summary>Selected exact version.</summary>
    public string Version { get; set; } = "";
    /// <summary>Content identity to pin in the apply request.</summary>
    public string Sha256 { get; set; } = "";
    /// <summary>Current local concurrency token to pin in the apply request.</summary>
    public string? ConcurrencyStamp { get; set; }
    /// <summary>Previously installed baseline.</summary>
    public string? OriginalSource { get; set; }
    /// <summary>Currently saved user source.</summary>
    public string? CurrentSource { get; set; }
    /// <summary>Verified new template source.</summary>
    public string NewSource { get; set; } = "";
    /// <summary>Whether current source differs from its installed baseline.</summary>
    public bool Customized { get; set; }
    /// <summary>Publisher's description.</summary>
    public string Description { get; set; } = "";
    /// <summary>Publisher attribution.</summary>
    public string Author { get; set; } = "";
    /// <summary>Source license.</summary>
    public string License { get; set; } = "";
}

/// <summary>Explicitly accepts the exact previewed source after conflict checks.</summary>
public sealed class ApplyScriptTemplate
{
    /// <summary>Existing local script; null creates a new instance.</summary>
    public Guid? ScriptId { get; set; }
    /// <summary>Selected catalog identity.</summary>
    public string CatalogId { get; set; } = "";
    /// <summary>Selected exact release.</summary>
    public string Version { get; set; } = "";
    /// <summary>SHA-256 supplied by the preview.</summary>
    public string ExpectedSha256 { get; set; } = "";
    /// <summary>Concurrency token supplied by the preview.</summary>
    public string? ExpectedConcurrencyStamp { get; set; }
    /// <summary>Explicit user acknowledgment that saved local edits will be replaced.</summary>
    public bool OverwriteLocalChanges { get; set; }
    /// <summary>Optional new-instance name.</summary>
    public string? Name { get; set; }
    /// <summary>New-instance script groups; null uses accessible defaults.</summary>
    public List<Guid>? GroupIds { get; set; }
}
