namespace Gurux.DLMS.AMI.Client.Shared
{
    /// <summary>
    /// Log type.
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// Generic log type.
        /// </summary>
        None = 0,
        /// <summary>
        /// Connection is established.
        /// </summary>
        Connected,
        /// <summary>
        /// Connection is terminated.
        /// </summary>
        Disconnect
    }
}