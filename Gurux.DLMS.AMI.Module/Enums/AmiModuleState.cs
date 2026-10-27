namespace Gurux.DLMS.AMI.Module.Enums;

/// <summary>Specifies the lifecycle state owned by the host module runtime.</summary>
public enum AmiModuleState : byte
{
    /// <summary>The module is loading and is not ready for use.</summary>
    Loading,
    /// <summary>The module is active and ready for use.</summary>
    Active,
    /// <summary>The module is stopping and may still be processing requests.</summary>
    Stopping,
    /// <summary>The module is stopped.</summary>
    Stopped,
    /// <summary>The module failed to load or encountered an execution error.</summary>
    Failed
}
