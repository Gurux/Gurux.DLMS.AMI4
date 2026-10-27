namespace Gurux.DLMS.AMI.Module.Enums;

/// <summary>Specifies how module-owned data is handled during uninstall.</summary>
public enum ModuleDataRemoval
{
    /// <summary>Preserve module-owned tables and their contents.</summary>
    Preserve = 0,
    /// <summary>Delete module-owned data while preserving table schemas.</summary>
    DeleteData = 1,
    /// <summary>Delete module-owned data and tables.</summary>
    DeleteDataAndTables = 2
}
