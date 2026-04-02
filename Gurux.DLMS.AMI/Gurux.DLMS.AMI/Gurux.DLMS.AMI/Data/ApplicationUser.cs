using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Data
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
        public DateTime? CreationTime
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
        /// Holds metadata about the client connection, including the remote IP address and other optional 
        /// network-related details associated with the current request.
        /// </summary>
        [PersonalData]
        [DataMember]
        [Description("Holds metadata about the client connection, including the remote IP address and other optional network-related details associated with the current request.")]
        [Filter(FilterType.Contains)]
        [DefaultValue(null)]
        [StringLength(64)]
        public string? ConnectionInfo
        {
            get;
            set;
        }


        /// <summary>
        /// When was the user last updated.
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

        /// <summary>
        /// Has this user been approved by an administrator.
        /// </summary>
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired()]
        public bool IsApproved { get; set; } = false;

        /// <summary>
        /// Time zone.
        /// </summary>
        [DataMember]
        public string? TimeZone
        {
            get;
            set;
        }
    }
}
