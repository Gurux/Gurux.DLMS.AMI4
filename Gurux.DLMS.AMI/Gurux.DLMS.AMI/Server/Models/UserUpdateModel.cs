using System.ComponentModel.DataAnnotations;

namespace Gurux.DLMS.AMI.Server.Models
{
    public class UserUpdateModel
    {
        /// <summary>
        /// Given name.
        /// </summary>
        public string? GivenName { get; set; }
        /// <summary>
        /// Surname.
        /// </summary>
        public string? Surname { get; set; }

        /// <summary>
        /// Phone number.
        /// </summary>
        [Phone]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Language.
        /// </summary>
        public string? Language { get; set; }

        /// <summary>
        /// Date of birth.
        /// </summary>
        [DataType(DataType.Date)]
        public DateTimeOffset? DateOfBirth { get; set; }

        /// <summary>
        /// Profile picture.
        /// </summary>
        public string ProfilePicture
        {
            get;
            set;
        }
    }

}
