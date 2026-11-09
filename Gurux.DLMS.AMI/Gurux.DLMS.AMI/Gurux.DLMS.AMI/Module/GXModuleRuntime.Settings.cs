namespace Gurux.DLMS.AMI.Server.Module;

public sealed partial class GXModuleRuntime
{
    /// <summary>Validates and migrates configuration for a currently registered module schema.</summary>
    /// <remarks>Does not mutate the live snapshot. Unloaded modules are validated when activated.</remarks>
    internal string? NormalizeSettings(string id, string? settings) => PrepareSettings(id, settings).Value;

    private (string? Value, string? Generation) PrepareSettings(string id, string? settings)
    {
        Entry? entry;
        lock (sync)
        {
            entry = entries.GetValueOrDefault(id);
        }
        // Keep the generation alongside the schema to prevent applying an edit to a replacement module.
        return (entry?.Settings.Normalize(settings) ?? settings, entry?.Generation);
    }
}
