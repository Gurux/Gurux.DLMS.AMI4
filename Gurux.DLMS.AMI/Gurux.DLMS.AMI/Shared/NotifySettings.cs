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
using Gurux.DLMS.AMI.Shared.DTOs;
using System.Diagnostics;

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Notify settings.
    /// </summary>
    public class NotifySettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public NotifySettings()
        {
            NetworkType = 1;
        }

        /// <summary>
        /// Is notify active.
        /// </summary>
        public bool Active { get; set; } = true;
        /// <summary>
        /// Network type. UDP = 0 and TCP = 1.
        /// </summary>
        public int NetworkType { get; set; } = 1;

        /// <summary>
        /// Interface type.
        /// </summary>
        public int Interface { get; set; }

        /// <summary>
        /// Use logical name referencing.
        /// </summary>
        public bool UseLogicalNameReferencing { get; set; } = true;

        /// <summary>
        /// System title of the server.
        /// </summary>
        public string? SystemTitle { get; set; }

        /// <summary>
        /// Block cipher key key.
        /// </summary>
        public string? BlockCipherKey { get; set; }

        /// <summary>
        /// Authentication key.
        /// </summary>
        public string? AuthenticationKey { get; set; }

        /// <summary>
        /// Notify port. Server waits notify, event or push messages to this port.
        /// </summary>
        public int Port { get; set; } = 4059;

        /// <summary>
        /// Expiration time in seconds.
        /// </summary>
        /// <remarks>
        /// Sometimes only part of the push message is received. Received data is clear after expiration time.
        /// </remarks>
        public int ExpirationTime { get; set; } = 10;

        /// <summary>
        /// Script method is used to parse received notification message.
        /// </summary>
        public Guid? ScriptMethod { get; set; }

        /// <summary>
        /// Used trace level.
        /// </summary>
        public TraceLevel TraceLevel
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Helpers.GetProperties(this);
        }

    }
}
