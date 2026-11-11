using Gurux.DLMS.AMI.Shared.DTOs.Script;

namespace Gurux.DLMS.AMI.Client.Helpers;

/// <summary>Creates independent editable source copies without catalog ownership or automation bindings.</summary>
public static class GXScriptCopies
{
    /// <summary>Prepares a disabled new script; normal Add authorization and compilation apply when saved.</summary>
    public static GXScript Create(GXScript source, string copyLabel)
    {
        ArgumentNullException.ThrowIfNull(source);
        string suffix = " (" + copyLabel + ")";
        string name = source.Name ?? string.Empty;
        int length = Math.Max(0, 64 - suffix.Length);
        return new GXScript(name[..Math.Min(name.Length, length)] + suffix)
        {
            Enabled = false,
            SourceCode = source.SourceCode,
            Namespaces = source.Namespaces,
            Language = source.Language,
            ServerSide = source.ServerSide,
            Description = source.Description,
            TraceLevel = source.TraceLevel,
            Properties = source.Properties?.Where(property => property.Removed == null)
                .Select(property => new GXScriptProperty { Name = property.Name, Value = property.Value }).ToList() ?? []
        };
    }
}