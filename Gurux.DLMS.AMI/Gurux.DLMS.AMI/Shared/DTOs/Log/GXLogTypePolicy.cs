using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Log;

/// <summary>Assigns an alternative named policy to a resource.</summary>
[DataContract, Serializable]
[IndexCollection(true, nameof(LogTypeId), nameof(PolicyId))]
public class GXLogTypePolicy
{
    /// <summary>Gets or sets the log type associated with the policy.</summary>
    [DataMember, IsRequired]
    [ForeignKey(typeof(GXLogType), OnDelete = ForeignKeyDelete.Cascade)]
    public long LogTypeId { get; set; }

    /// <summary>Gets or sets the identifier of the assigned authorization policy.</summary>
    [DataMember, IsRequired]
    [ForeignKey(typeof(GXPolicy), OnDelete = ForeignKeyDelete.Restrict)]
    public Guid PolicyId { get; set; }
}
