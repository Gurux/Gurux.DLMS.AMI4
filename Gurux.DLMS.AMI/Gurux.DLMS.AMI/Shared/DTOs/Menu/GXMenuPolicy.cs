using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Menu;

/// <summary>Assigns an alternative named policy to a resource.</summary>
[DataContract, Serializable]
[IndexCollection(true, nameof(MenuId), nameof(PolicyId))]
public class GXMenuPolicy
{
    /// <summary>Gets or sets the menu associated with the policy.</summary>
    [DataMember, IsRequired]
    [ForeignKey(typeof(GXMenu), OnDelete = ForeignKeyDelete.Cascade)]
    public Guid MenuId { get; set; }

    /// <summary>Gets or sets the identifier of the assigned authorization policy.</summary>
    [DataMember, IsRequired]
    [ForeignKey(typeof(GXPolicy))]
    public Guid PolicyId { get; set; }
}
