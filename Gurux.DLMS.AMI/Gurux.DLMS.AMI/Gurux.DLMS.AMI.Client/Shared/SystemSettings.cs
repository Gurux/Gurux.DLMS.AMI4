namespace Gurux.DLMS.AMI.Client.Shared
{
    /// <summary>
    /// System settings.
    /// </summary>
    public class SystemSettings
    {
        /// <summary>
        /// Size of the database connection pool.
        /// </summary>
        public int PoolSize { get; set; } = 10;

        /// <summary>
        /// Is Swagger enabled.
        /// </summary>
        public bool UseSwagger { get; set; } = true;

        /// <summary>
        /// Site email address.
        /// </summary>
        public string? SiteEmailAddress
        {
            get;
            set;
        }

        /// <summary>
        /// Rows per page.
        /// </summary>
        public int RowsPerPage { get; set; } = 100;
    }
}