﻿//
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
using Gurux.DLMS.AMI.Shared.DTOs.Report;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Services
{
    /// <summary>
    /// This interface is used to send reports.
    /// </summary>
    public interface IReport
    {
        /// <summary>
        /// Send reports async.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="report">Sended report.</param>
        /// <returns>Result as a string.</returns>
        Task SendAsync(ClaimsPrincipal user, GXReport report);

        /// <summary>
        /// Cancel the sending of the reports.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="reports">Cancelled reports.</param>
        void Cancel(ClaimsPrincipal user, IEnumerable<Guid>? reports);

        /// <summary>
        /// Update reports when user has updated report time or targets.
        /// </summary>
        Task UpdateAsync();
    }
}
