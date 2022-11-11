using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// AMI error is used to send server errors.
    /// </summary>
    public class GXAmiException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXAmiException()
        {

        }

        public new string Message
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Error message.</param>
        public GXAmiException(string message) : base(message)
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
