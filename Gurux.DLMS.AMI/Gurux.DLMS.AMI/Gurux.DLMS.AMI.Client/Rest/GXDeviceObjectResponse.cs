using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Client.Rest;

/// <summary>A device-scoped object and optional validated attribute selection.</summary>
public sealed class GXDeviceObjectResponse
{
    public GXObject? Item { get; set; }
    public Guid? AttributeId { get; set; }
}