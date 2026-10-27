namespace Gurux.DLMS.AMI.Shared.Enums
{
    /// <summary>Processing state of a received raw notification.</summary>
    [System.ComponentModel.Description("Processing state of a received raw notification.")]
    public enum RawNotificationStatus : byte
    {
        /// <summary>Waiting to be processed.</summary>
        [System.ComponentModel.Description("Waiting to be processed.")]
        Pending = 0,

        /// <summary>Reserved by a processing agent.</summary>
        [System.ComponentModel.Description("Reserved by a processing agent.")]
        Processing = 1,

        /// <summary>Processed successfully.</summary>
        [System.ComponentModel.Description("Processed successfully.")]
        Completed = 2,

        /// <summary>Processing failed.</summary>
        [System.ComponentModel.Description("Processing failed.")]
        Failed = 3
    }
}
