using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Block;

/// <summary>Assigns an alternative named policy to a resource.</summary>
[DataContract, Serializable]
[IndexCollection(true, nameof(BlockGroupId), nameof(PolicyId))]
public class GXBlockGroupPolicy
{
    /// <summary>Gets or sets the block group associated with the policy.</summary>
    [DataMember, IsRequired]
    [ForeignKey(typeof(GXBlockGroup), OnDelete = ForeignKeyDelete.Cascade)]
    public Guid BlockGroupId { get; set; }

    /// <summary>Gets or sets the identifier of the assigned authorization policy.</summary>
    [DataMember, IsRequired]
    [ForeignKey(typeof(GXPolicy), OnDelete = ForeignKeyDelete.Restrict)]
    public Guid PolicyId { get; set; }
}
