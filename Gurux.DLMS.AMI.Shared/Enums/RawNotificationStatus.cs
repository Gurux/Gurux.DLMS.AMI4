namespace Gurux.DLMS.AMI.Shared.Enums
{
    /// <summary>Processing state of a received raw notification.</summary>
    public enum RawNotificationStatus : byte
    {
        /// <summary>Waiting to be processed.</summary>
        Pending = 0,

        /// <summary>Reserved by a processing agent.</summary>
        Processing = 1,

        /// <summary>Processed successfully.</summary>
        Completed = 2,

        /// <summary>Processing failed.</summary>
        Failed = 3
    }
}
