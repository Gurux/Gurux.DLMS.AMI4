namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Forbidden exception is used to tell that resource access fails.
    /// </summary>
    public class GXAMIForbiddenException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXAMIForbiddenException()
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
        public GXAMIForbiddenException(string message) : base(message)
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
    }
}
