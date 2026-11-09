// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

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
        public bool MaintenanceMode { get; set; }

        /// <summary>
        /// Maintenance mode message that is shown for the user.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Maintenance start time.
        /// </summary>
        public DateTimeOffset? StartTime { get; set; }
        /// <summary>
        /// Expected end time of the maintenance.
        /// </summary>
        public DateTimeOffset? EndTime { get; set; }

        /// <summary>Captures date values for maintenance token resolution without modifying the template.</summary>
        public IReadOnlyDictionary<string, string?> GetTokenValues() => new Dictionary<string, string?>
        {
            ["maintenance:start"] = StartTime?.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            ["maintenance:end"] = EndTime?.ToString("O", System.Globalization.CultureInfo.InvariantCulture)
        };

        /// <summary>Validates the schedule and template message size.</summary>
        public void Validate()
        {
            if (Message?.Length > 65536)
                {
                throw new ArgumentException("Maintenance message must not exceed 65536 characters.");
            }
            if (StartTime != null && EndTime != null && EndTime <= StartTime)
                {
                throw new ArgumentException("EndTime must be after StartTime.");
            }
        }

        /// <summary>Evaluates the optional start-inclusive, end-exclusive maintenance interval.</summary>
        public bool IsActive(DateTimeOffset now) => MaintenanceMode &&
            (!StartTime.HasValue || now >= StartTime.Value) &&
            (!EndTime.HasValue || now < EndTime.Value);

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
