namespace Gurux.DLMS.AMI.Shared.Enums;

/// <summary>Source of a mandatory module dependency.</summary>
public enum AmiModuleDependencyKind
{
    /// <summary>Dependency supplied by a host assembly.</summary>
    HostAssembly,
    /// <summary>Dependency supplied by another module.</summary>
    Module,
    /// <summary>Dependency supplied by an exported service.</summary>
    Service
}
