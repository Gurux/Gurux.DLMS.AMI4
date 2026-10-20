using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
namespace Gurux.DLMS.AMI.Shared.DTOs.View;

/// <summary>A validated left or inner join in a view.</summary>
[DataContract, Serializable, IndexCollection(true, nameof(ViewId), nameof(Alias))]
public class GXViewJoin : IUnique<Guid>
{
    [DataMember] public Guid Id { get; set; }
    [DataMember, ForeignKey(typeof(GXView), OnDelete = ForeignKeyDelete.Cascade)] public Guid ViewId { get; set; }
    [DataMember, StringLength(64)] public string SourceAlias { get; set; } = "source";
    [DataMember, StringLength(128)] public string SourceColumn { get; set; } = "";
    [DataMember, StringLength(128)] public string TargetTable { get; set; } = "";
    [DataMember, StringLength(64)] public string Alias { get; set; } = "";
    [DataMember, StringLength(128)] public string TargetColumn { get; set; } = "";
    [DataMember] public bool Inner { get; set; }
    [DataMember] public int Position { get; set; }
}
