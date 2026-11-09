namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Canonical routes for operating on a device's COSEM objects.</summary>
public static class GXDeviceObjectRoutes
{
    public static string Objects(Guid deviceId) => $"/Device/Objects/{deviceId}";
    public static string Object(Guid deviceId, Guid objectId) => $"{Objects(deviceId)}/{objectId}";
    public static string Attribute(Guid deviceId, Guid objectId, Guid attributeId) => $"{Object(deviceId, objectId)}/Attributes/{attributeId}";
}