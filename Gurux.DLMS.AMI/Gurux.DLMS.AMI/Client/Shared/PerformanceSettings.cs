using Gurux.DLMS.AMI.Shared.Enums;
using System;

namespace Gurux.DLMS.AMI.Client.Shared
{
    /// <summary>
    /// Performance settings.
    /// </summary>
    public class PerformanceSettings
    {
        /// <summary>
        /// Notifications to ignore.
        /// </summary>
        public TargetType IgnoreNotification {get;set;}
    }
}