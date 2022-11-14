using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gurux.DLMS.AMI.Server.Models
{
    /// <summary>
    /// Application user.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// When user is created.
        /// </summary>
        [PersonalData]
        [DataType(DataType.DateTime)]
        public DateTimeOffset? CreationTime
        {
            get;
            set;
        }
        /// <summary>
        /// When was the last time a user logged in.
        /// </summary>
        [PersonalData]
        [DataType(DataType.DateTime)]
        public virtual DateTimeOffset? LastLogin
        {
            get;
            set;
        }
        /// <summary>
        /// IP address where last connection is established.
        /// </summary>
        [PersonalData]
        [Column("IPAddress")]
        public UInt64? LastIPAddress
        {
            get;
            set;
        }
        /// <summary>
        /// When user is updated last time.
        /// </summary>
        [PersonalData]
        [Column("Updated")]
        [DataType(DataType.DateTime)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        } 
        /// <summary>
        /// Given name.
        /// </summary>
        [PersonalData]
        [Column("GivenName")]
        public string? GivenName { get; set; }
        /// <summary>
        /// Surname
        /// </summary>
        [PersonalData]
        [Column("Surname")]
        public string? Surname { get; set; }

        /// <summary>
        /// Default language.
        /// </summary>
        [PersonalData]
        [Column("Language")]
        public string? Language { get; set; }

        /// <summary>
        /// Date of birth.
        /// </summary>
        [PersonalData]
        [Column("DateOfBirth")]
        [DataType(DataType.Date)]
        public DateTimeOffset? DateOfBirth { get; set; }

        /// <summary>
        /// Profile picture.
        /// </summary>
        [PersonalData]
        [Column("ProfilePicture")]
        public string? ProfilePicture { get; set; }
    }
}
