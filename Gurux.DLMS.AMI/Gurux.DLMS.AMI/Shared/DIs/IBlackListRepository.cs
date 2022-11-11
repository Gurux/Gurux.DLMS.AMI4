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

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This public interface is used to handle black listed IP addresses.
    /// </summary>
    public interface IBlackListRepository
    {
        /// <summary>
        /// Add address for the blackList.
        /// </summary>
        /// <param name="address">IP address to add for the black list.</param>
        void Add(UInt64 address);

        /// <summary>
        /// Add addresses for the blackList.
        /// </summary>
        /// <param name="list">List of IP address to add for the black list.</param>
        void AddRange(IEnumerable<UInt64> list);

        /// <summary>
        /// Remove IP address from the black list.
        /// </summary>
        /// <param name="address">IP address to remove.</param>
        void Delete(UInt64 address);
       
        /// <summary>
        /// Remove IP addresses from the black list.
        /// </summary>
        /// <param name="list">IP addresses to remove.</param>
        void RemoveRange(IEnumerable<UInt64> list);

        /// <summary>
        /// Is IP address blocked.
        /// </summary>
        /// <param name="address">IP address verify.</param>
        /// <returns>True, if address is blocked.</returns>
        bool IsBlocked(UInt64 address);
    }
}
