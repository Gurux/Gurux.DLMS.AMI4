
namespace Gurux.DLMS.AMI.Client.Shared
{
    /// <summary>
    /// Status settings.
    /// </summary>
    public class StatusSettings
    {
        /// <summary>
        /// Site version.
        /// </summary>
        public string? SiteVersion
        {
            get;
            set;
        }
        /// <summary>
        /// System start time.
        /// </summary>
        public DateTimeOffset StartTime
        {
            get;
            set;
        }
    }
}