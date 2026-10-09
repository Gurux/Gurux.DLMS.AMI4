using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Menu;

/// <summary>Assigns an alternative named policy to a resource.</summary>
[DataContract, Serializable]
[IndexCollection(true, nameof(MenuLinkId), nameof(PolicyId))]
public class GXMenuLinkPolicy
{
    /// <summary>Gets or sets the menu link associated with the policy.</summary>
    [DataMember, IsRequired]
    [ForeignKey(typeof(GXMenuLink), OnDelete = ForeignKeyDelete.Cascade)]
    public Guid MenuLinkId { get; set; }

    /// <summary>Gets or sets the identifier of the assigned authorization policy.</summary>
    [DataMember, IsRequired]
    [ForeignKey(typeof(GXPolicy))]
    public Guid PolicyId { get; set; }
}
