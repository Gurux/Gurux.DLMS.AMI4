using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs;

/// <summary>Associates an appearance with an authorization policy.</summary>
[DataContract, Serializable]
[IndexCollection(true, nameof(AppearanceId), nameof(PolicyId))]
public class GXAppearancePolicy
{
    /// <summary>Gets or sets the appearance associated with the policy.</summary>
    [DataMember, IsRequired, StringLength(64), ForeignKey(typeof(GXAppearance), OnDelete = ForeignKeyDelete.Cascade)]
    public string AppearanceId { get; set; } = "";

    /// <summary>Gets or sets the identifier of the assigned authorization policy.</summary>
    [DataMember, IsRequired, ForeignKey(typeof(GXPolicy), OnDelete = ForeignKeyDelete.Restrict)]
    public Guid PolicyId { get; set; }
}
