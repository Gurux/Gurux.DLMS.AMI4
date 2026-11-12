namespace Gurux.DLMS.AMI.Client.Rest;

/// <summary>Selects a saved device template and its public profile metadata.</summary>
public sealed class GXPublishCommunityDeviceTemplate
{
    public Guid Id { get; set; }
    public string Manufacturer { get; set; } = "";
    public string Model { get; set; } = "";
    public string Firmware { get; set; } = "";
    public string Description { get; set; } = "";
    public int ProfileIndex { get; set; }
    public bool AcceptPublication { get; set; }
}
