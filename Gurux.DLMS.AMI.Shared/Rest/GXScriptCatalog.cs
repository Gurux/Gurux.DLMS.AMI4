namespace Gurux.DLMS.AMI.Shared.Rest;

/// <summary>Requests an authorized comparison with one exact catalog release.</summary>
[System.ComponentModel.Description("Requests an authorized comparison with one exact catalog release.")]
public sealed class PreviewScriptTemplate
{
    /// <summary>Existing script; null imports a new independent instance of the template.</summary>
    [System.ComponentModel.Description("Existing script; null imports a new independent instance of the template.")]
    public Guid? ScriptId { get; set; }
    /// <summary>Publisher's stable script identifier.</summary>
    [System.ComponentModel.Description("Publisher's stable script identifier.")]
    public string CatalogId { get; set; } = "";
    /// <summary>Exact retained catalog version.</summary>
    [System.ComponentModel.Description("Exact retained catalog version.")]
    public string Version { get; set; } = "";
}

/// <summary>Comparison data; requesting this never compiles or invokes a script.</summary>
[System.ComponentModel.Description("Comparison data; requesting this never compiles or invokes a script.")]
public sealed class ScriptTemplatePreview
{
    /// <summary>Existing local instance, or null for a new import.</summary>
    [System.ComponentModel.Description("Existing local instance, or null for a new import.")]
    public Guid? ScriptId { get; set; }
    /// <summary>Catalog identity.</summary>
    [System.ComponentModel.Description("Catalog identity.")]
    public string CatalogId { get; set; } = "";
    /// <summary>Selected exact version.</summary>
    [System.ComponentModel.Description("Selected exact version.")]
    public string Version { get; set; } = "";
    /// <summary>Content identity to pin in the apply request.</summary>
    [System.ComponentModel.Description("Content identity to pin in the apply request.")]
    public string Sha256 { get; set; } = "";
    /// <summary>Current local concurrency token to pin in the apply request.</summary>
    [System.ComponentModel.Description("Current local concurrency token to pin in the apply request.")]
    public string? ConcurrencyStamp { get; set; }
    /// <summary>Previously installed baseline.</summary>
    [System.ComponentModel.Description("Previously installed baseline.")]
    public string? OriginalSource { get; set; }
    /// <summary>Currently saved user source.</summary>
    [System.ComponentModel.Description("Currently saved user source.")]
    public string? CurrentSource { get; set; }
    /// <summary>Verified new template source.</summary>
    [System.ComponentModel.Description("Verified new template source.")]
    public string NewSource { get; set; } = "";
    /// <summary>Whether current source differs from its installed baseline.</summary>
    [System.ComponentModel.Description("Whether current source differs from its installed baseline.")]
    public bool Customized { get; set; }
    /// <summary>Publisher's description.</summary>
    [System.ComponentModel.Description("Publisher's description.")]
    public string Description { get; set; } = "";
    /// <summary>Publisher attribution.</summary>
    [System.ComponentModel.Description("Publisher attribution.")]
    public string Author { get; set; } = "";
    /// <summary>Source license.</summary>
    [System.ComponentModel.Description("Source license.")]
    public string License { get; set; } = "";
}

/// <summary>Explicitly accepts the exact previewed source after conflict checks.</summary>
[System.ComponentModel.Description("Explicitly accepts the exact previewed source after conflict checks.")]
public sealed class ApplyScriptTemplate
{
    /// <summary>Existing local script; null creates a new instance.</summary>
    [System.ComponentModel.Description("Existing local script; null creates a new instance.")]
    public Guid? ScriptId { get; set; }
    /// <summary>Selected catalog identity.</summary>
    [System.ComponentModel.Description("Selected catalog identity.")]
    public string CatalogId { get; set; } = "";
    /// <summary>Selected exact release.</summary>
    [System.ComponentModel.Description("Selected exact release.")]
    public string Version { get; set; } = "";
    /// <summary>SHA-256 supplied by the preview.</summary>
    [System.ComponentModel.Description("SHA-256 supplied by the preview.")]
    public string ExpectedSha256 { get; set; } = "";
    /// <summary>Concurrency token supplied by the preview.</summary>
    [System.ComponentModel.Description("Concurrency token supplied by the preview.")]
    public string? ExpectedConcurrencyStamp { get; set; }
    /// <summary>Explicit user acknowledgment that saved local edits will be replaced.</summary>
    [System.ComponentModel.Description("Explicit user acknowledgment that saved local edits will be replaced.")]
    public bool OverwriteLocalChanges { get; set; }
    /// <summary>Optional new-instance name.</summary>
    [System.ComponentModel.Description("Optional new-instance name.")]
    public string? Name { get; set; }
    /// <summary>New-instance script groups; null uses accessible defaults.</summary>
    [System.ComponentModel.Description("New-instance script groups; null uses accessible defaults.")]
    public List<Guid>? GroupIds { get; set; }
}
