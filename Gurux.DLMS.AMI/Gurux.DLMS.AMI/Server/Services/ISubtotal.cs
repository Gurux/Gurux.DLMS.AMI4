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
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Services
{
    /// <summary>
    /// This interface is used to calculate subtotals.
    /// </summary>
    public interface ISubtotal
    {
        /// <summary>
        /// Calculate subtotals.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="subtotals">Added subtotals.</param>
        void Calculate(ClaimsPrincipal user, IEnumerable<Guid>? subtotals);

        /// <summary>
        /// Cancel the calcutation of the subtotals.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="subtotals">Cancelled subtotals.</param>
        void Cancel(ClaimsPrincipal user, IEnumerable<Guid>? subtotals);

        /// <summary>
        /// Update subtotals when user has updated sub total time or targets.
        /// </summary>
        Task UpdateAsync();
    }
}
