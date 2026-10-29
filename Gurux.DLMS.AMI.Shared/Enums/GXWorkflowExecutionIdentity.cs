namespace Gurux.DLMS.AMI.Shared.Enums;

/// <summary>Principal used for automatic workflow execution.</summary>
public enum GXWorkflowExecutionIdentity
{
    /// <summary>Execute with the permissions of the originating event actor.</summary>
    EventActor = 0,
    /// <summary>Execute with the explicitly configured service account.</summary>
    ServiceAccount = 1
}
