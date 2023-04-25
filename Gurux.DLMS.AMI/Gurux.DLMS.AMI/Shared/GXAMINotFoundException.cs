
namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Throws an exception when the Gurux DLMS AMI does not find item from the database.
    /// </summary>
    public class GXAmiNotFoundException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXAmiNotFoundException()
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
        public GXAmiNotFoundException(string message) : base(message)
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
