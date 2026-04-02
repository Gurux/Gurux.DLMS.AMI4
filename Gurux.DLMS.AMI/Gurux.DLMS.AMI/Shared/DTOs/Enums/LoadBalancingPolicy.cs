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

using System.Xml.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.Enums
{
    /// <summary>
    /// Load balancing policy.
    /// </summary>
    [Flags]
    public enum LoadBalancingPolicy : byte
    {
        /// <summary>
        /// Select the alphabetically first available destination without considering load. 
        /// This is useful for dual destination fail-over systems.
        /// </summary>
        [XmlEnum("1")]
        FirstAlphabetical = 1,
        /// <summary>
        /// Select a destination randomly.
        /// </summary>
        [XmlEnum("2")]
        Random = 2,
        /// <summary>
        /// Select two random destinations and then select the one with the least assigned requests. 
        /// This avoids the overhead of LeastRequests and the worst case for Random where it selects a busy destination.
        /// </summary>
        [XmlEnum("4")]
        PowerOfTwoChoices = 4,
        /// <summary>
        /// Select a destination by cycling through them in order.
        /// </summary>
        [XmlEnum("8")]
        RoundRobin = 8,
        /// <summary>
        /// Select the destination with the least assigned requests. 
        /// This requires examining all destinations.
        /// </summary>
        [XmlEnum("16")]
        LeastRequests = 16,
    }
}
