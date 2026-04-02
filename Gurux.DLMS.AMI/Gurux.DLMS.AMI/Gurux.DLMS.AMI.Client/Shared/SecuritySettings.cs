namespace Gurux.DLMS.AMI.Client.Shared
{
    /// <summary>
    /// Security settings.
    /// </summary>
    public class SecuritySettings
    {
        /// <summary>
        /// Gets or sets the minimum length a password must be. Defaults to 6.
        /// </summary>
        public int RequiredLength { get; set; } = 6;

        /// <summary>
        /// Gets or sets the minimum number of unique characters which a password must contain. Defaults to 1.
        /// </summary>
        public int RequiredUniqueChars { get; set; } = 1;

        /// <summary>
        /// Gets or sets a flag indicating if passwords must contain a non-alphanumeric character. Defaults to true.
        /// </summary>
        /// <value>True if passwords must contain a non-alphanumeric character, otherwise false.</value>
        public bool RequireNonAlphanumeric { get; set; } = true;

        /// <summary>
        /// Gets or sets a flag indicating if passwords must contain a lower case ASCII character. Defaults to true.
        /// </summary>
        /// <value>True if passwords must contain a lower case ASCII character.</value>
        public bool RequireLowercase { get; set; } = true;

        /// <summary>
        /// Gets or sets a flag indicating if passwords must contain a upper case ASCII character. Defaults to true.
        /// </summary>
        /// <value>True if passwords must contain a upper case ASCII character.</value>
        public bool RequireUppercase { get; set; } = true;

        /// <summary>
        /// Gets or sets a flag indicating if passwords must contain a digit. Defaults to true.
        /// </summary>
        /// <value>True if passwords must contain a digit.</value>
        public bool RequireDigit { get; set; } = true;

        /// <summary>
        /// Gets or sets the list of allowed characters in the username used to validate user names. Defaults to abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+
        /// </summary>
        /// <value>
        /// The list of allowed characters in the username used to validate user names.
        /// </value>
        public string AllowedUserNameCharacters { get; set; } = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

        /// <summary>
        /// Gets or sets a flag indicating whether the application requires unique emails for its users. Defaults to false.
        /// </summary>
        /// <value>
        /// True if the application requires each user to have their own, unique email, otherwise false.
        /// </value>
        public bool RequireUniqueEmail { get; set; }


        /// <summary>
        /// Gets or sets a flag indicating whether a confirmed email address is required to sign in. Defaults to false.
        /// </summary>
        /// <value>True if a user must have a confirmed email address before they can sign in, otherwise false.</value>
        public bool RequireConfirmedEmail { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether a confirmed telephone number is required to sign in. Defaults to false.
        /// </summary>
        /// <value>True if a user must have a confirmed telephone number before they can sign in, otherwise false.</value>
        public bool RequireConfirmedPhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether a confirmed <see cref="IUserConfirmation{TUser}"/> account is required to sign in. Defaults to false.
        /// </summary>
        /// <value>True if a user must have a confirmed account before they can sign in, otherwise false.</value>
        public bool RequireConfirmedAccount { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether a new user can be locked out. Defaults to true.
        /// </summary>
        /// <value>
        /// True if a newly created user can be locked out, otherwise false.
        /// </value>
        public bool AllowedForNewUsers { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of failed access attempts allowed before a user is locked out,
        /// assuming lock out is enabled. Defaults to 5.
        /// </summary>
        /// <value>
        /// The number of failed access attempts allowed before a user is locked out, if lockout is enabled.
        /// </value>
        public int MaxFailedAccessAttempts { get; set; } = 5;

        /// <summary>
        /// Gets or sets the <see cref="TimeSpan"/> a user is locked out for when a lockout occurs. Defaults to 5 minutes.
        /// </summary>
        /// <value>The <see cref="TimeSpan"/> a user is locked out for when a lockout occurs.</value>
        public TimeSpan DefaultLockoutTimeSpan { get; set; } = TimeSpan.FromMinutes(5);

    }
}