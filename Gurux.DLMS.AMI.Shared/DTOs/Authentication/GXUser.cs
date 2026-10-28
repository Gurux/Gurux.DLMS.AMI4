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

using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs.Notification;

namespace Gurux.DLMS.AMI.Shared.DTOs.Authentication
{
    /// <summary>An application user account with its identity, preferences, and resource memberships.</summary>
    [Description("An application user account with its identity, preferences, and resource memberships.")]
    [DataContract(Name = "GXUser"), Serializable]
    public class GXUser : IUnique<string?>
    {
        /// <summary>Initializes a new instance of GXUser.</summary>
        [Description("Initializes a new instance of GXUser.")]
        public GXUser()
        {
        }

        /// <summary>Initializes a new instance of GXUser.</summary>
        /// <remarks>
        /// This constructor is called when a new user group is created. It will create all required collections.
        /// </remarks>
        /// <param name="name">User group name.</param>
        [Description("Initializes a new instance of GXUser.")]
        public GXUser(string? name)
        {
            UserName = name;
            Actions = [];
            UserGroups = [];
            Roles = [];
            Permissions = [];
            BlockSettings = [];
            Errors = [];
            Properties = [];
            Favorites = [];
            Tasks = [];
            Notifications = [];
        }

        /// <summary>User identifier.</summary>
        [Description("User identifier.")]
        [Key]
        [DataMember(Name = "ID"), Index(Unique = true)]
        [StringLength(36)]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public string? Id { get; set; }

        /// <summary>
        /// User name.
        /// </summary>
        [Description("User name.")]
        [DataMember]
        [StringLength(256)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? UserName { get; set; }

        /// <summary>
        /// Normalized user name.
        /// </summary>
        /// <remarks>
        /// User name is converted to uppercase.
        /// </remarks>
        [Description("Normalized user name.")]
        [DataMember]
        [StringLength(256)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? NormalizedUserName { get; set; }

        /// <summary>
        /// Eamil address.
        /// </summary>
        [Description("Eamil address.")]
        [DataMember, Index(Unique = true)]
        [StringLength(256)]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Email { get; set; }

        /// <summary>
        /// Normalized user eamil address.
        /// </summary>
        /// <remarks>
        /// Eamil address is converted to uppercase.
        /// </remarks>

        [Description("Normalized user eamil address.")]
        [DataMember, Index(Unique = true)]
        [StringLength(256)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? NormalizedEmail { get; set; }

        /// <summary>
        /// Has user confirmed the email address.
        /// </summary>
        [Description("Has user confirmed the email address.")]
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? EmailConfirmed { get; set; }

        /// <summary>
        /// Password is not saved for the database.
        /// </summary>
        /// <seealso cref="PasswordHash"/>
        [Description("Password is not saved for the database.")]
        [DataMember]
        [Ignore(IgnoreType.Db)]
        [IsRequired]
        public string? Password { get; set; }

        /// <summary>
        /// Passwords are not saved to the database. Only the hash is saved by the server.
        /// </summary>
        /// <seealso cref="Password"/>
        [Description("Passwords are not saved to the database. Only the hash is saved by the server.")]
        [StringLength(256)]
        [DataMember]
        [JsonIgnore]
        [IsRequired]
        public string? PasswordHash { get; set; }

        /// <summary>
        /// If the user has modified the settings.
        /// </summary>
        [Description("If the user has modified the settings.")]
        [DataMember]
        [JsonIgnore]
        public string? SecurityStamp { get; set; }

        /// <summary>Version token used by the database to detect conflicting updates.</summary>
        /// <remarks>
        /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
        /// </remarks>
        [Description("Version token used by the database to detect conflicting updates.")]
        [DataMember]
        [StringLength(36)]
        [ConcurrencyCheck]
        public string? ConcurrencyStamp { get; set; }

        /// <summary>
        /// Phone number.
        /// </summary>
        [Description("Phone number.")]
        [DataMember]
        [Filter(FilterType.Contains)]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Is phone number confirmed.
        /// </summary>
        [Description("Is phone number confirmed.")]
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? PhoneNumberConfirmed { get; set; }

        /// <summary>
        /// Is two factor enabled.
        /// </summary>
        [Description("Is two factor enabled.")]
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired]
        public bool? TwoFactorEnabled { get; set; }

        /// <summary>
        /// When lockout ends.
        /// </summary>
        [Description("When lockout ends.")]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? LockoutEnd { get; set; }

        /// <summary>
        /// Is lockout enabled.
        /// </summary>

        [Description("Is lockout enabled.")]
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired()]
        public bool? LockoutEnabled { get; set; }

        /// <summary>
        ///  Has this user been approved by an administrator.
        /// </summary>
        [Description("Has this user been approved by an administrator.")]
        [DataMember]
        [DefaultValue(false)]
        [Filter(FilterType.Exact)]
        [IsRequired()]
        public bool? IsApproved { get; set; }

        /// <summary>
        /// Amount of failed access.
        /// </summary>
        [Description("Amount of failed access.")]
        [DataMember]
        [DefaultValue(0)]
        public int? AccessFailedCount { get; set; }

        /// <summary>
        /// When user is created.
        /// </summary>
        [Description("When user is created.")]
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        //Creation time must be DateTime.
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// When was the last time a user logged in.
        /// </summary>
        [Description("When was the last time a user logged in.")]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public virtual DateTimeOffset? LastLogin { get; set; }

        /// <summary>
        /// What types of notifications the user is interested in.
        /// </summary>
        [Description("What types of notifications the user is interested in.")]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public virtual UserNotification? Notification { get; set; }


        /// <summary>
        /// Holds metadata about the client connection, including the remote IP address and other optional
        /// network-related details associated with the current request.
        /// </summary>
        [DataMember]
        [Description("Holds metadata about the client connection, including the remote IP address and other optional network-related details associated with the current request.")]
        [Filter(FilterType.Contains)]
        [DefaultValue(null)]
        [StringLength(64)]
        public string? ConnectionInfo { get; set; }

        /// <summary>
        /// User Roles.
        /// </summary>
        [Description("User Roles.")]
        [Ignore(IgnoreType.Db)]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public List<string>? Roles { get; set; }

        /// <summary>
        /// User permissions.
        /// </summary>
        [Description("User permissions.")]
        [Ignore(IgnoreType.Db)]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.Contains)]
        public List<string>? Permissions { get; set; }

        /// <summary>Time when the user was last updated.</summary>
        [Description("Time when the user was last updated.")]
        [DataMember]
        [DefaultValue(null)]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }


        /// <summary>Time when the record was marked as removed.</summary>
        [Description("Time when the record was marked as removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }


        /// <summary>
        /// List of user groups where this user belongs.
        /// </summary>
        [Description("List of user groups where this user belongs.")]
        [DataMember]
        [ForeignKey(typeof(GXUserGroup), typeof(GXUserGroupUser))]
        [Filter(FilterType.Contains)]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<GXUserGroup>? UserGroups { get; set; }

        /// <summary>
        /// List of user actions.
        /// </summary>
        //Actions are not saved for the DB column.
        [Description("List of user actions.")]
        [DataMember, Ignore(IgnoreType.Db)]
        [Filter(FilterType.Contains)]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<GXLog>? Actions { get; set; }

        /// <summary>
        /// User errors.
        /// </summary>
        [Description("User errors.")]
        [DataMember, Ignore(IgnoreType.Db)]
        [Filter(FilterType.Contains)]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<GXLog>? Errors { get; set; }

        /// <summary>Saved block configurations belonging to this user.</summary>
        /// <remarks>
        /// Allowed list is also known as safelist or white list.
        /// Disallowed list is also known as black list.
        /// </remarks>


        /// <summary>
        /// User depending block settings.
        /// </summary>
        [Description("User depending block settings.")]
        [DataMember]
        [ForeignKey(typeof(GXBlock))]
        [Filter(FilterType.Contains)]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<GXBlock>? BlockSettings { get; set; }

        /// <summary>
        /// User workflows.
        /// </summary>
        [Description("User workflows.")]
        [DataMember]
        [Filter(FilterType.Contains)]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<GXWorkflow>? Workflows { get; set; }

        /// <summary>
        /// User workflow groups.
        /// </summary>
        [Description("User workflow groups.")]
        [DataMember]
        [Filter(FilterType.Contains)]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<GXWorkflowGroup>? WorkflowGroups { get; set; }

        /// <summary>
        /// User component views.
        /// </summary>
        [Description("User component views.")]
        [DataMember]
        [Filter(FilterType.Contains)]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<GXComponentView>? ComponentViews { get; set; }

        /// <summary>
        /// User component view groups.
        /// </summary>
        [Description("User component view groups.")]
        [DataMember]
        [Filter(FilterType.Contains)]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<GXComponentViewGroup>? ComponentViewGroups { get; set; }


        /// <summary>
        /// Saved application settings belonging to this user.
        /// </summary>
        [Description("Saved application settings belonging to this user.")]
        [DataMember]
        [ForeignKey(typeof(GXUserProperty))]
        [Filter(FilterType.Contains)]
        public List<GXUserProperty>? Properties { get; set; }

        /// <summary>
        /// User favorites.
        /// </summary>
        [Description("User favorites.")]
        [DataMember, ForeignKey(typeof(GXFavorite))]
        [Filter(FilterType.Contains)]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<GXFavorite>? Favorites { get; set; }

        /// <summary>
        /// Executed tasks.
        /// </summary>
        [Description("Executed tasks.")]
        [DataMember]
        [ForeignKey(typeof(GXTask))]
        [Filter(FilterType.Contains)]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<GXTask>? Tasks { get; set; }

        /// <summary>
        /// Executed tasks.
        /// </summary>
        [Description("User notifications.")]
        [DataMember]
        [ForeignKey(typeof(GXNotification))]
        [Filter(FilterType.Contains)]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<GXNotification>? Notifications { get; set; }

        /// <summary>
        /// Schedules.
        /// </summary>
        [Description("Schedules.")]
        [DataMember]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<GXSchedule>? Schedules { get; set; }

        /// <summary>
        /// Schedule groups.
        /// </summary>
        [Description("Schedule groups.")]
        [DataMember]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<GXScheduleGroup>? ScheduleGroups { get; set; }

        /// <summary>
        /// User language.
        /// </summary>
        [Description("User language.")]
        [DataMember]
        [Filter(FilterType.Contains)]
        public string? Language { get; set; }

        /// <summary>
        /// Given name.
        /// </summary>
        [Description("Given name.")]
        [DataMember]
        [Filter(FilterType.Contains)]
        public string? GivenName { get; set; }

        /// <summary>Surname.</summary>
        [Description("Surname.")]
        [DataMember]
        [Filter(FilterType.Contains)]
        public string? Surname { get; set; }

        /// <summary>
        /// Latitude and Longitude.
        /// </summary>
        [Description("Latitude and Longitude.")]
        [DataMember]
        [StringLength(26)]
        [Filter(FilterType.Contains)]
        public string? Coordinates { get; set; }

        /// <summary>
        /// Street address.
        /// </summary>
        [Description("Street address.")]
        [DataMember]
        [StringLength(64)]
        [Filter(FilterType.Contains)]
        public string? StreetAddress { get; set; }

        /// <summary>
        /// Postal code.
        /// </summary>
        [Description("Postal code.")]
        [DataMember]
        [StringLength(64)]
        [Filter(FilterType.Contains)]
        public string? PostalCode { get; set; }

        /// <summary>
        /// State or province.
        /// </summary>
        [Description("State or province.")]
        [DataMember]
        [StringLength(64)]
        [Filter(FilterType.Contains)]
        public string? StateOrProvince { get; set; }

        /// <summary>
        /// Country.
        /// </summary>
        [Description("Country.")]
        [DataMember]
        [StringLength(64)]
        [Filter(FilterType.Contains)]
        public string? Country { get; set; }

        /// <summary>
        /// Date of birth.
        /// </summary>
        [Description("Date of birth.")]
        [DataMember]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? DateOfBirth { get; set; }

        /// <summary>
        /// GXAppearance image used as the user's profile picture; null means no picture.
        /// </summary>
        [Description("GXAppearance image used as the user's profile picture; null means no picture.")]
        [DataMember, ForeignKey(OnDelete = ForeignKeyDelete.Restrict)]
        public GXAppearance? ProfilePicture { get; set; }

        /// <summary>
        /// Time zone.
        /// </summary>
        [Description("Time zone.")]
        [DataMember]
        public string? TimeZone { get; set; }

        /// <summary>User agents.</summary>
        [Description("User agents.")]
        [DataMember]
        [ForeignKey(typeof(GXAgent))]
        [Filter(FilterType.Contains)]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<GXAgent>? Agents { get; set; }

        /// <summary>
        /// User agent groups.
        /// </summary>
        [Description("User agent groups.")]
        [DataMember]
        [ForeignKey(typeof(GXAgent))]
        [Filter(FilterType.Contains)]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<GXAgentGroup>? AgentGroups { get; set; }

        /// <summary>
        /// User devices.
        /// </summary>
        [Description("User devices.")]
        [DataMember]
        [Filter(FilterType.Contains)]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<GXDevice>? Devices { get; set; }

        /// <summary>
        /// User device groups.
        /// </summary>
        [Description("User device groups.")]
        [DataMember]
        [Filter(FilterType.Contains)]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<GXDeviceGroup>? DeviceGroups { get; set; }

        /// <summary>
        /// User stamps tell what user has stamped and when.
        /// </summary>
        [Description("User stamps tell what user has stamped and when.")]
        [DataMember]
        [ForeignKey(typeof(GXUserStamp))]
        [Filter(FilterType.Contains)]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public List<GXUserStamp>? Stamps { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            string? value = Email;
            if (!string.IsNullOrEmpty(UserName))
            {
                value += " " + UserName;
            }
            return value ?? nameof(GXUser);
        }

    }
}
