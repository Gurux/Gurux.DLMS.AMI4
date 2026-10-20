using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.View;

/// <summary>Assigns an alternative named policy to a resource.</summary>
[DataContract, Serializable]
[IndexCollection(true, nameof(ViewId), nameof(PolicyId))]
public class GXViewPolicy
{
    /// <summary>Gets or sets the view associated with the policy.</summary>
    [DataMember, IsRequired]
    [ForeignKey(typeof(GXView), OnDelete = ForeignKeyDelete.Cascade)]
    public Guid ViewId { get; set; }

    /// <summary>Gets or sets the identifier of the assigned authorization policy.</summary>
    [DataMember, IsRequired]
    [ForeignKey(typeof(GXPolicy))]
    public Guid PolicyId { get; set; }
}
