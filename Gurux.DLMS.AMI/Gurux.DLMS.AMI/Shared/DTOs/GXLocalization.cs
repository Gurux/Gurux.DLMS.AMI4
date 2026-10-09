using Gurux.Service.Orm.Common;
using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared.DTOs;

/// <summary>Persisted localization release metadata synchronized from the update catalog.</summary>
[IndexCollection(true, nameof(Owner), nameof(OwnerType), nameof(Version))]
public sealed class GXLocalization : IUnique<Guid>
{
    /// <summary>Gets or sets the unique identifier of the localization release.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the identifier of the product that owns this localization release.</summary>
    [StringLength(256)] public string Owner { get; set; } = string.Empty;
    /// <summary>Gets or sets the type of product that owns this localization release.</summary>
    [StringLength(32)] public string OwnerType { get; set; } = string.Empty;
    /// <summary>Gets or sets the localization release version.</summary>
    [StringLength(64)] public string Version { get; set; } = string.Empty;
    /// <summary>Gets or sets the culture identifiers supported by the localization package.</summary>
    [StringLength(1024)] public string Cultures { get; set; } = string.Empty;
    /// <summary>Gets or sets the compatible owner version range, if specified.</summary>
    [StringLength(128)] public string? OwnerVersionRange { get; set; }
    /// <summary>Gets or sets the default culture of the localization package.</summary>
    [StringLength(32)] public string DefaultCulture { get; set; } = string.Empty;
    /// <summary>Gets or sets the description of the localization release.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the release notes of the localization package.</summary>
    public string? ReleaseNotes { get; set; }
    /// <summary>Gets or sets the download URL of the localization package.</summary>
    [StringLength(2048)] public string Url { get; set; } = string.Empty;
    /// <summary>Gets or sets the expected SHA-256 hash of the localization package.</summary>
    [StringLength(64)] public string Sha256 { get; set; } = string.Empty;
    /// <summary>Gets or sets the localization package size in bytes.</summary>
    public long Size { get; set; }
    /// <summary>Gets or sets whether the localization release is active.</summary>
    public bool Active { get; set; }
    /// <summary>Gets or sets the time the localization metadata was last updated.</summary>
    public DateTimeOffset Updated { get; set; }
}
