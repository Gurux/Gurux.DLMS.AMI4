using System;

namespace Gurux.DLMS.AMI.Client.Shared
{
    /// <summary>
    /// Cron settings.
    /// </summary>
    public class CronSettings
    {
        /// <summary>
        /// Last run time.
        /// </summary>
        public DateTimeOffset? Run
        {
            get;
            set;
        }

        /// <summary>
        /// Cron interval in hours.
        /// </summary>
        public int Interval
        {
            get;
            set;
        } = 24;

        /// <summary>
        /// When Cron is run at the next time.
        /// </summary>
        public DateTimeOffset? EstimatedNextTime { get; set; }
    }
}