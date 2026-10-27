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

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Server is in maintenance mode.
    /// </summary>
    [System.ComponentModel.Description("Server is in maintenance mode.")]
    public class GXMaintenanceException : Exception
    {
        /// <summary>
        /// Retry after tells when server should be operating again.
        /// </summary>
        [System.ComponentModel.Description("Retry after tells when server should be operating again.")]
        public TimeSpan? RetryAfter { get; set; }

        /// <summary>
        /// Maintenance end time.
        /// </summary>
        [System.ComponentModel.Description("Maintenance end time.")]
        public DateTimeOffset? EndTime { get; set; }

        /// <summary>Initializes a new instance of GXMaintenanceException.</summary>
        [System.ComponentModel.Description("Initializes a new instance of GXMaintenanceException.")]
        public GXMaintenanceException() : base()
        {

        }

        /// <summary>Initializes a new instance of GXMaintenanceException.</summary>
        /// <param name="message"> The message that describes the error.</param>
        [System.ComponentModel.Description("Initializes a new instance of GXMaintenanceException.")]
        public GXMaintenanceException(string? message) : base(message)
        {

        }
    }
}
