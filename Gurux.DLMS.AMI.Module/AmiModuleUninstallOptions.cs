using Gurux.DLMS.AMI.Shared.Enums;
namespace Gurux.DLMS.AMI.Module;

/// <summary>Controls data removal when uninstalling a module.</summary>
/// <param name="Mode">Data removal mode used during uninstallation.</param>
public sealed record AmiModuleUninstallOptions(ModuleDataRemoval Mode = ModuleDataRemoval.Preserve)
{
    /// <summary>Rejects an undefined data removal mode.</summary>
    public void Validate()
    {
        if (!Enum.IsDefined(Mode))
        {
            throw new ArgumentException("Unknown module data removal option.");
        }
    }
}
/// <summary>Classifies a database table owned by a module.</summary>
public enum AmiModuleTableKind
{
    /// <summary>Module data records.</summary>
    Data,
    /// <summary>Module metadata records.</summary>
    Metadata
}
/// <summary>Database table ownership declaration.</summary>
/// <param name="Kind">Table data classification.</param>
/// <param name="Type">Database table type.</param>
public sealed record AmiModuleTableRegistration(Type Type, AmiModuleTableKind Kind);
