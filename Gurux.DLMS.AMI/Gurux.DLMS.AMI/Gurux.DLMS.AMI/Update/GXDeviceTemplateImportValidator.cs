using System.Text.Json;
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Shared.DTOs.Device;

namespace Gurux.DLMS.AMI.Update;

/// <summary>Checks community GXC files with the actual AMI template importer and serializer.</summary>
public static class GXDeviceTemplateImportValidator
{
    /// <summary>Host capability used by independently deployed DeviceProfiles modules.</summary>
    public const string ServiceKey = "Gurux.DLMS.AMI.DeviceTemplate.ValidateImport";

    /// <summary>Throws when a sanitized GXC cannot become one serializable device template.</summary>
    public static void Validate(string gxc)
    {
        var templates = ClientHelpers.ConvertToTemplates(null, gxc);
        if (templates.Count != 1)
        {
            throw new InvalidOperationException("The GXC file must import as exactly one device template.");
        }
        var json = JsonSerializer.Serialize(templates[0]);
        var restored = JsonSerializer.Deserialize<GXDeviceTemplate>(json);
        if (restored?.Objects?.Any() != true)
        {
            throw new InvalidOperationException("The imported device template could not be serialized with its COSEM objects.");
        }
    }
}
