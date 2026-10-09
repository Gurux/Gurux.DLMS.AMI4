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
using Gurux.DLMS.AMI.Shared.DTOs.User;

namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Defines hub callbacks used to broadcast server-side updates.
    /// </summary>
    public interface IGXHubEvents
    {
        /// <summary>
        /// Notifies clients that one or more target entities were created or updated.
        /// </summary>
        /// <param name="items">Updated target entities.</param>
        Task OnUpdate(IEnumerable<GXTarget> items);

        /// <summary>
        /// Notifies clients that one or more user stamps were created or updated.
        /// </summary>
        /// <param name="stamps">Updated user stamps.</param>
        Task StampUpdate(IEnumerable<GXUserStamp> stamps);
    }
}
