//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using Gurux.Common.Db;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>
    /// Users table.
    /// </summary>
    [DataContract(Name = "GXUser"), Serializable]
    public class GXUser : IUnique<string>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXUser()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constuctor is called when a new user group is created. It will create all needed lists.
        /// </remarks>
        /// <param name="name">User group name.</param>
        public GXUser(string? name)
        {
            UserName = name;
            Actions = new List<GXUserAction>();
            UserGroups = new List<GXUserGroup>();
            Roles = new List<string>();
            IpAddresses = new List<GXIpAddress>();
            BlockSettings = new List<GXBlock>();
            Errors = new List<GXUserError>();
            RestStatistics = new List<GXRestStatistic>();
            Settings = new List<GXUserSetting>();
            Favorites = new List<GXFavorite>();
            Tasks = new List<GXTask>();
        }

        /// <summary>
        /// User ID.
        /// </summary>
        [Key]
        [DataMember(Name = "ID"), Index(Unique = true)]
        [StringLength(36)]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public string Id
        {
            get;
            set;
        } = "";

        /// <summary>
        /// User name.
        /// </summary>
        [DataMember]
        [StringLength(256)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? UserName
        {
            get;
            set;
        }

        /// <summary>
        /// Normalized user name.
        /// </summary>
        /// <remarks>
        /// User name is converted to uppercase.
        /// </remarks>
        [DataMember]
        [StringLength(256)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? NormalizedUserName
        {
            get;
            set;
        }

        /// <summary>
        /// Eamil address.
        /// </summary>
        [DataMember, Index(Unique = true)]
        [StringLength(256)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Email
        {
            get;
            set;
        }

        /// <summary>
        /// Normalized user eamil address.
        /// </summary>
        /// <remarks>
        /// Eamil address is converted to uppercase.
        /// </remarks>

        [DataMember, Index(Unique = true)]
        [StringLength(256)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? NormalizedEmail
        {
            get;
            set;
        }

        /// <summary>
        /// Has user confirmed the email address.
        /// </summary>
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? EmailConfirmed { get; set; }

        /// <summary>
        /// Password is not saved for the database.
        /// </summary>
        /// <seealso cref="PasswordHash"/>
        [DataMember]
        [Ignore(IgnoreType.Db)]
        [IsRequired]
        public string? Password
        {
            get;
            set;
        }

        /// <summary>
        /// Passwords are not saved to the database. Only the hash is saved by the server.
        /// </summary>
        /// <seealso cref="Password"/>
        [StringLength(256)]
        [DataMember]
        [JsonIgnore]
        [IsRequired]
        public string? PasswordHash
        {
            get;
            set;
        }

        [DataMember]
        [JsonIgnore]
        public string? SecurityStamp
        {
            get;
            set;
        }

        /// <summary>
        /// Concurrency stamp.
        /// </summary>
        /// <remarks>
        /// Concurrency stamp is used to verify that several user's can't 
        /// modify the target at the same time.
        /// </remarks>
        [DataMember]
        [StringLength(36)]
        public string? ConcurrencyStamp
        {
            get;
            set;
        }

        /// <summary>
        /// Phone number.
        /// </summary>
        [DataMember]
        [Filter(FilterType.Contains)]
        public string? PhoneNumber
        {
            get;
            set;
        }

        /// <summary>
        /// Is phone number confirmed.
        /// </summary>
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? PhoneNumberConfirmed { get; set; }

        /// <summary>
        /// Is two factor enabled.
        /// </summary>
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? TwoFactorEnabled { get; set; }

        /// <summary>
        /// When lockout ends.
        /// </summary>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? LockoutEnd
        {
            get;
            set;
        }

        /// <summary>
        /// Is lockout enabled.
        /// </summary>

        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired()]
        public bool? LockoutEnabled { get; set; }

        /// <summary>
        /// Amount of failed access.
        /// </summary>
        [DataMember]
        [DefaultValue(0)]
        public int? AccessFailedCount { get; set; }

        /// <summary>
        /// When user is created.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// When was the last time a user logged in.
        /// </summary>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public virtual DateTimeOffset? LastLogin
        {
            get;
            set;
        }

        /// <summary>
        /// IP address where last connection is established.
        /// </summary>
        [DataMember]
        [Index(false)]
        public UInt64 IPAddress
        {
            get;
            set;
        }

        /// <summary>
        /// User Roles.
        /// </summary>
        [Ignore(IgnoreType.Db)]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public List<string>? Roles
        {
            get;
            set;
        }

        /// <summary>
        /// User notifications.
        /// </summary>
        [Ignore(IgnoreType.Db)]
        [DataMember]
        [DefaultValue(null)]
        public List<string>? Notifications
        {
            get;
            set;
        }

        /// <summary>
        /// When was the user last updated.
        /// </summary>
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <summary>
        /// User has modified the user settings.
        /// </summary>
        [IgnoreDataMember]
        [Ignore]
        [JsonIgnore]
        public bool Modified
        {
            get;
            set;
        }

        /// <summary>
        /// Remove time.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed
        {
            get;
            set;
        }


        /// <summary>
        /// List of user groups where this user belongs.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXUserGroup), typeof(GXUserGroupUser))]
        [Filter(FilterType.Contains)]
        public List<GXUserGroup>? UserGroups
        {
            get;
            set;
        }

        /// <summary>
        /// List of user actions.
        /// </summary>
        //Actions are not saved for the DB column.
        [DataMember, ForeignKey(typeof(GXUserAction))]
        [Filter(FilterType.Contains)]
        public List<GXUserAction>? Actions
        {
            get;
            set;
        }

        /// <summary>
        /// User errors.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXUserError))]
        [Filter(FilterType.Contains)]
        public List<GXUserError>? Errors
        {
            get;
            set;
        }

        /// <summary>
        /// List of allowed and disallowed IP addresses.
        /// </summary>
        /// <remarks>
        /// Allowed list is also known as safelist or white list.
        /// Disallowed list is also known as black list.
        /// </remarks>
        [DataMember]
        [DefaultValue(null)]
        [ForeignKey(typeof(GXIpAddress))]
        [Filter(FilterType.Contains)]
        public List<GXIpAddress>? IpAddresses
        {
            get;
            set;
        }

        /// <summary>
        /// User depending block settings.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXBlock))]
        [Filter(FilterType.Contains)]
        public List<GXBlock>? BlockSettings
        {
            get;
            set;
        }

        /// <summary>
        /// User depending settings.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXUserSetting))]
        [Filter(FilterType.Contains)]
        public List<GXUserSetting>? Settings
        {
            get;
            set;
        }


        /// <summary>
        /// Rest statistics.
        /// </summary>
        /// <remarks>
        /// This information can be used to improve SQL query syntax.
        /// </remarks>
        [DataMember]
        [ForeignKey(typeof(GXRestStatistic))]
        [Filter(FilterType.Contains)]
        public List<GXRestStatistic>? RestStatistics
        {
            get;
            set;
        }

        /// <summary>
        /// User favorites.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXFavorite))]
        [Filter(FilterType.Contains)]
        public List<GXFavorite>? Favorites
        {
            get;
            set;
        }

        /// <summary>
        /// Executed tasks.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXTask))]
        [Filter(FilterType.Contains)]
        public List<GXTask>? Tasks
        {
            get;
            set;
        }

        /// <summary>
        /// User language.
        /// </summary>
        [DataMember]
        [Filter(FilterType.Contains)]
        public string? Language { get; set; }

        /// <summary>
        /// Given name.
        /// </summary>
        [DataMember]
        [Filter(FilterType.Contains)]
        public string? GivenName { get; set; }

        /// <summary>
        /// Surname
        /// </summary>
        [DataMember]
        [Filter(FilterType.Contains)]
        public string? Surname { get; set; }

        /// <summary>
        /// Latitude and Longitude.
        /// </summary>
        [DataMember]
        [StringLength(26)]
        [Filter(FilterType.Contains)]
        public string? Coordinates { get; set; }

        /// <summary>
        /// Street address.
        /// </summary>
        [DataMember]
        [StringLength(64)]
        [Filter(FilterType.Contains)]
        public string? StreetAddress { get; set; }

        /// <summary>
        /// Postal code.
        /// </summary>
        [DataMember]
        [StringLength(64)]
        [Filter(FilterType.Contains)]
        public string? PostalCode { get; set; }

        /// <summary>
        /// State or province.
        /// </summary>
        [DataMember]
        [StringLength(64)]
        [Filter(FilterType.Contains)]
        public string? StateOrProvince { get; set; }

        /// <summary>
        /// Country.
        /// </summary>
        [DataMember]
        [StringLength(64)]
        [Filter(FilterType.Contains)]
        public string? Country { get; set; }

        /// <summary>
        /// Date of birth.
        /// </summary>
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? DateOfBirth { get; set; }

        /// <summary>
        /// Profile picture.
        /// </summary>
        [DataMember]
        public string? ProfilePicture { get; set; }

        /// <summary>
        /// User stamps tell what user has stamped and when.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXTask))]
        [Filter(FilterType.Contains)]
        public List<GXUserStamp>? Stamps
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(UserName))
            {
                return UserName;
            }
            return nameof(GXUser);
        }

    }
}
