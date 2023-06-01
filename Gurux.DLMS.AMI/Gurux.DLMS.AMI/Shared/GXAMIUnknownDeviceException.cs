namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Unknown device exception is used when agent asks device from the meter and it's not found.
    /// </summary>
    public class GXAMIUnknownDeviceException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXAMIUnknownDeviceException()
        {
            Message = "";
        }

        /// <inheritdoc />
        public new string Message
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Error message.</param>
        public GXAMIUnknownDeviceException(string message) : base(message)
        {
            Message = message;
        }

        /// <summary>
        /// Creation time.
        /// </summary>
        public DateTime CreationTime { get; set; }
        /// <summary>
        /// Message ID on the database.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Device system title.
        /// </summary>
        public string? SystemTitle
        {
            get;
            set;
        }

        /// <summary>
        /// Additional data.
        /// </summary>
        public new string? Data
        {
            get;
            set;
        }
    }
}
