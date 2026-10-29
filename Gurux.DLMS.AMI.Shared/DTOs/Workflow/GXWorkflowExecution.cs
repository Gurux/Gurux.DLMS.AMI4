using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Workflow;

/// <summary>Serializable execution summary stored in the Data of a workflow GXLog entry.
/// Related step entries use the same GXLog.ExecutionId. This payload is not a separate ORM table.</summary>
[DataContract]
public sealed class GXWorkflowExecution
{
    /// <summary>Identifier of this workflow invocation.</summary>
    [DataMember] public Guid ExecutionId { get; set; }
    /// <summary>Identifier of the originating event, shared by all workflows handling it.</summary>
    [DataMember] public Guid EventId { get; set; }
    /// <summary>Identifier of the workflow event chain.</summary>
    [DataMember] public Guid ChainId { get; set; }
    /// <summary>Number of workflow-induced event transitions in this chain.</summary>
    [DataMember] public int Depth { get; set; }
    /// <summary>Workflow identifier.</summary>
    [DataMember] public Guid WorkflowId { get; set; }
    /// <summary>Workflow name captured when the execution started.</summary>
    [DataMember] public string? WorkflowName { get; set; }
    /// <summary>Workflow revision captured when the execution started.</summary>
    [DataMember] public string? WorkflowVersion { get; set; }
    /// <summary>Account that caused the originating event, when available.</summary>
    [DataMember] public string? ActorId { get; set; }
    /// <summary>Account whose permissions were used for this execution.</summary>
    [DataMember] public string? ExecutionUserId { get; set; }
    /// <summary>Stable trigger identifier, including module-defined triggers.</summary>
    [DataMember] public string? TriggerCode { get; set; }
    /// <summary>Trigger activity name.</summary>
    [DataMember] public string? Activity { get; set; }
    /// <summary>Phase handled by this invocation.</summary>
    [DataMember] public TriggerPhase Phase { get; set; } = TriggerPhase.After;
    /// <summary>Stable code of the originating event, when available.</summary>
    [DataMember] public string? EventCode { get; set; }
    /// <summary>Correlation shared by all events of an operation.</summary>
    [DataMember] public Guid? OperationId { get; set; }
    /// <summary>Optional resource category.</summary>
    [DataMember] public string? TargetType { get; set; }
    /// <summary>Optional resource identifier; may be a module-defined string identifier.</summary>
    [DataMember] public string? TargetId { get; set; }
    /// <summary>Time the invocation was queued.</summary>
    [DataMember] public DateTimeOffset QueuedAt { get; set; }
    /// <summary>Time execution began, or null while queued.</summary>
    [DataMember] public DateTimeOffset? StartedAt { get; set; }
    /// <summary>Time execution ended, or null while unfinished.</summary>
    [DataMember] public DateTimeOffset? CompletedAt { get; set; }
    /// <summary>Current execution state.</summary>
    [DataMember] public WorkflowExecutionStatus Status { get; set; }
    /// <summary>Condition result; null if no condition was evaluated.</summary>
    [DataMember] public bool? ConditionMatched { get; set; }
    /// <summary>Optional failure or rejection message. Do not store credentials or sensitive payloads.</summary>
    [DataMember] public string? Message { get; set; }
}
