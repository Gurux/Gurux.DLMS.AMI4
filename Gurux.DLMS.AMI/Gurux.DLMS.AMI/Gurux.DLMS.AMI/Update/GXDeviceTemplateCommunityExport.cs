using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Gurux.DLMS.AMI.Client.Rest;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.Enums;

namespace Gurux.DLMS.AMI.Update;

/// <summary>Exports only reusable protocol and COSEM metadata, never local credentials or values.</summary>
public static class GXDeviceTemplateCommunityExport
{
    /// <summary>Public protocol metadata and sanitized GXC sent to the review service.</summary>
    /// <param name="Manufacturer">Public manufacturer name.</param>
    /// <param name="Model">Public model name.</param>
    /// <param name="Firmware">Optional firmware variant.</param>
    /// <param name="Interface">Gurux interface name.</param>
    /// <param name="Authentication">Gurux authentication name.</param>
    /// <param name="Description">Public profile description.</param>
    /// <param name="Gxc">Sanitized reusable COSEM definitions.</param>
    /// <param name="AcceptPublication">Explicit CC0 consent.</param>
    public sealed record Payload(string Manufacturer, string Model, string Firmware, string Interface,
        string Authentication, string Description, string Gxc, bool AcceptPublication);

    /// <summary>Creates an anonymous profile using allowlisted fields from persisted template data.</summary>
    public static Payload Create(GXDeviceTemplate template, GXPublishCommunityDeviceTemplate request)
    {
        if (!request.AcceptPublication || template.Removed != null)
        {
            throw new ArgumentException("Select an available device template and accept publication under CC0.");
        }
        foreach (var value in new[] { request.Manufacturer, request.Model })
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 80)
            {
                throw new ArgumentException("Manufacturer and model are required (at most 80 characters each).");
            }
        }
        if (request.Firmware == null || request.Description == null || request.Firmware.Length > 80 || request.Description.Length > 1000)
        {
            throw new ArgumentException("Firmware or description is too long.");
        }
        var settings = JsonSerializer.Deserialize<Gurux.DLMS.AMI.Shared.DTOs.Device.GXDLMSSettings>(template.Settings ?? "{}")
            ?? throw new ArgumentException("The device template has no communication settings.");
        var profiles = settings.Profiles;
        var profileCount = Math.Max(1, profiles.Count);
        if (request.ProfileIndex < 0 || request.ProfileIndex >= profileCount)
        {
            throw new ArgumentException("Select an available communication profile.");
        }
        var profile = profiles.Count == 0 ? null : profiles[request.ProfileIndex];
        var interfaceType = (InterfaceType)(profile?.InterfaceType ?? settings.InterfaceType);
        var authentication = (Authentication)settings.Authentication;
        if (!Enum.IsDefined(interfaceType) || !Enum.IsDefined(authentication))
        {
            throw new ArgumentException("Invalid interface or authentication type.");
        }
        XNamespace ns = "Gurux1";
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        var device = new XElement(ns + "GXDLMSDevice",
            new XElement(ns + "Name", authentication.ToString()),
            new XElement(ns + "Authentication", authentication.ToString()),
            new XElement(ns + "InterfaceType", (int)interfaceType),
            new XElement(ns + "ClientAddress", profile?.ClientAddress ?? settings.ClientAddress),
            new XElement(ns + "PhysicalAddress", profile?.PhysicalAddress ?? settings.PhysicalAddress),
            new XElement(ns + "LogicalAddress", profile?.LogicalAddress ?? settings.LogicalAddress),
            new XElement(ns + "WaitTime", template.WaitTime), new XElement(ns + "ResendCount", template.ResendCount));
        using var json = JsonDocument.Parse(template.Settings ?? "{}");
        foreach (var name in new[] { "ServerAddressSize", "MaxInfoTX", "MaxInfoRX", "WindowSizeTX", "WindowSizeRX", "PduSize",
            "Conformance", "HDLCAddressing", "InactivityTimeout", "Priority", "ServiceClass", "UseLogicalNameReferencing", "UseProtectedRelease" })
        {
            if (json.RootElement.TryGetProperty(name, out var value) &&
                value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            {
                device.Add(new XElement(ns + name, value.GetRawText()));
            }
        }
        var objects = new XElement(ns + "Objects2");
        foreach (var item in (template.Objects ?? []).Where(item => item.Removed == null))
        {
            if (item.ObjectType == null || !Enum.IsDefined((ObjectType)item.ObjectType.Value))
            {
                throw new ArgumentException("A COSEM object has an invalid class.");
            }
            var native = GXDLMSClient.CreateObject((ObjectType)item.ObjectType.Value);
            if (native is not Gurux.DLMS.Objects.IGXDLMSBase)
            {
                throw new ArgumentException("The template contains an unsupported COSEM class.");
            }
            var obj = new XElement(ns + "GXDLMSObject", new XAttribute(xsi + "type", native.GetType().Name),
                new XElement(ns + "LogicalName", item.LogicalName), new XElement(ns + "Version", item.Version ?? 0),
                new XElement(ns + "ShortName", item.ShortName ?? 0));
            var attributes = new XElement(ns + "Attributes");
            foreach (var attribute in (item.Attributes ?? []).Where(attribute => attribute.Removed == null))
            {
                var dataType = (DataType)attribute.DataType;
                var uiType = (DataType)attribute.UIDataType;
                if (!Enum.IsDefined(dataType) || !Enum.IsDefined(uiType))
                {
                    throw new ArgumentException("An attribute has an invalid data type.");
                }
                var accessName = Enum.IsDefined((AccessMode)attribute.AccessLevel) ? "Access" : "Access3";
                attributes.Add(new XElement(ns + "Item", new XElement(ns + "Index", attribute.Index),
                    new XElement(ns + "Type", dataType.ToString()), new XElement(ns + "UIType", uiType.ToString()),
                    new XElement(ns + accessName, attribute.AccessLevel)));
            }
            obj.Add(attributes);
            var scaling = item.Attributes?.FirstOrDefault(attribute => attribute.Removed == null && attribute.Index == 2);
            if (scaling?.Scaler is double scaler && double.IsFinite(scaler) && scaler > 0)
            {
                obj.Add(new XElement(ns + "Scaler", scaler.ToString("R", CultureInfo.InvariantCulture)));
            }
            if (Enum.TryParse<Unit>(scaling?.Unit, out var unit) && Enum.IsDefined(unit))
            {
                obj.Add(new XElement(ns + "Unit", (int)unit));
            }
            objects.Add(obj);
        }
        if (!objects.HasElements)
        {
            throw new ArgumentException("The device template must contain at least one COSEM object.");
        }
        device.Add(objects);
        var gxc = new XElement(ns + "ArrayOfGXDLMSDevice", new XAttribute(XNamespace.Xmlns + "xsi", xsi), device).ToString();
        if (Encoding.UTF8.GetByteCount(gxc) > 2 * 1024 * 1024)
        {
            throw new ArgumentException("The exported device template exceeds 2 MiB.");
        }
        return new(request.Manufacturer.Trim(), request.Model.Trim(), request.Firmware.Trim(), interfaceType.ToString(),
            authentication.ToString(), request.Description.Trim(), gxc, true);
    }
}
