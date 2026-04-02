using Gurux.DLMS.AMI.Shared.DTOs;
using System;

namespace Gurux.DLMS.AMI.Client.Shared
{
    /// <summary>
    /// Maintenance settings.
    /// </summary>
    public class MaintenanceSettings
    {
        /// <summary>
        /// Site is in Maintenance mode.
        /// </summary>
        public bool MaintenanceMode
        {
            get;
            set;
        }

        /// <summary>
        /// Maintenance mode message that is shown for the user.
        /// </summary>
        public string? Message
        {
            get;
            set;
        }

        /// <summary>
        /// Maintenance start time.
        /// </summary>
        public DateTimeOffset? StartTime
        {
            get;
            set;
        }
        /// <summary>
        /// Expected end time of the maintenance.
        /// </summary>
        public DateTimeOffset? EndTime
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (MaintenanceMode)
            {
                return "Site is on maintenance mode.";
            }
            return "Site is on normal mode.";
        }
    }
}