using System.ComponentModel.DataAnnotations;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;

namespace Gurux.DLMS.AMI.Shared.DTOs.Script;

/// <summary>Source retained transactionally before an installed catalog template is replaced.</summary>
public sealed class GXScriptTemplateHistory : IUnique<Guid>
{
    /// <summary>Revision identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Script whose previous source is retained.</summary>
    [ForeignKey(typeof(GXScript), OnDelete = ForeignKeyDelete.Cascade), Index(false)]
    public Guid ScriptId { get; set; }
    /// <summary>Previous catalog version.</summary>
    [StringLength(64)] public string? CatalogVersion { get; set; }
    /// <summary>Previous user source, including any local edits.</summary>
    public string SourceCode { get; set; } = "";
    /// <summary>Previous additional imports.</summary>
    public string? Namespaces { get; set; }
    /// <summary>User who accepted the replacement.</summary>
    [StringLength(450)] public string UserId { get; set; } = "";
    /// <summary>Time at which the replacement was accepted.</summary>
    public DateTimeOffset CreationTime { get; set; }
}
