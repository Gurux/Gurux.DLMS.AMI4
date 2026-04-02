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

using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Triggers
{
    /// <summary>
    /// Agent trigger type.
    /// </summary>
    public class AgentTrigger : ITriggerAction
    {
        /// <inheritdoc />
        public string Name => "Agent";

        /// <inheritdoc />
        public Type? ConfigurationUI => null;

        /// <inheritdoc />
        public string? Icon => "oi oi-wifi";

        /// <summary>
        /// A new agent is added.
        /// </summary>
        public static string Add = "Add";
        /// <summary>
        /// Agent is updated.
        /// </summary>
        public static string Updated = "Updated";
        /// <summary>
        /// Agent error is occurred.
        /// </summary>
        public static string Error = "Error";
        /// <summary>
        /// Agent is removed.
        /// </summary>
        public static string Removed = "Removed";
        /// <summary>
        /// Agent is detected.
        /// </summary>
        public static string Detected = "Detected";
    }
}
