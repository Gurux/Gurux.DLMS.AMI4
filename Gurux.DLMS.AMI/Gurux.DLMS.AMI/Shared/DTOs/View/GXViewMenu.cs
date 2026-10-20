using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
namespace Gurux.DLMS.AMI.Shared.DTOs.View;
/// <summary>Ordered membership of a menu in a view's toolbar.</summary>
[DataContract, Serializable, IndexCollection(true, nameof(ViewId), nameof(MenuId))]
public class GXViewMenu : IUnique<Guid>
{
    [DataMember, PrimaryKey] public Guid Id {get;set;} = Guid.NewGuid();
    [DataMember, IsRequired, ForeignKey(typeof(GXView), OnDelete = ForeignKeyDelete.Cascade)] public Guid ViewId { get; set; }
    [DataMember, IsRequired, ForeignKey(typeof(GXMenu), OnDelete = ForeignKeyDelete.None)] public Guid MenuId { get; set; }
    [DataMember] public int Position { get; set; }
}
