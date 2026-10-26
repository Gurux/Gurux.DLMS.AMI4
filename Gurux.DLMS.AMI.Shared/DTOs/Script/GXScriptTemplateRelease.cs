using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Gurux.Service.Orm.Common;

namespace Gurux.DLMS.AMI.Shared.DTOs.Script;

/// <summary>A retained immutable catalog release. Synchronization never changes installed source code.</summary>
[IndexCollection(true, nameof(CatalogId), nameof(Version))]
public sealed class GXScriptTemplateRelease : IUnique<Guid>
{
    /// <summary>Local release identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Publisher's stable script identifier.</summary>
    [StringLength(200), Index(false)] public string CatalogId { get; set; } = "";
    /// <summary>Normalized NuGet version.</summary>
    [StringLength(64)] public string Version { get; set; } = "";
    /// <summary>Validated index source, configured by the host.</summary>
    [StringLength(2048)] public string CatalogSource { get; set; } = "";
    /// <summary>Validated native SDK manifest, including immutable source identity.</summary>
    [JsonIgnore] public string ManifestJson { get; set; } = "";
    /// <summary>False when a later complete index no longer advertises this release.</summary>
    public bool Enabled { get; set; } = true;
}
