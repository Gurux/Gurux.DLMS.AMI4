using System;

namespace Gurux.DLMS.AMI.Client.Shared
{
    /// <summary>
    /// Statistic settings.
    /// </summary>
    public class StatisticSettings
    {
        /// <summary>
        /// Are user actions saved.
        /// </summary>
        public bool UserActions
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Log REST operations if it takes at least the given time in milliseconds.
        /// </summary>
        public int RestTrigger
        {
            get;
            set;
        } = 5000;

        /// <summary>
        /// Are device actions saved.
        /// </summary>
        public bool DeviceActions
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Are Schedule actions saved.
        /// </summary>
        public bool ScheduleActions
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Are Agent actions saved.
        /// </summary>
        public bool AgentActions
        {
            get;
            set;
        } = true;

    }
}