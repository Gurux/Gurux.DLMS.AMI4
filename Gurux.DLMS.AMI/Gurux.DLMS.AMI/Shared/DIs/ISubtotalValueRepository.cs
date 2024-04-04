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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Subtotal;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle subtotal values.
    /// </summary>
    public interface ISubtotalValueRepository
    {
        /// <summary>
        /// List subtotal values.
        /// </summary>
        /// <returns>List of subtotal values.</returns>
        Task<GXSubtotalValue[]> ListAsync(
            ClaimsPrincipal User,
            ListSubtotalValues? request,
            ListSubtotalValuesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read subtotal value information.
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="id">Subtotal value id.</param>
        /// <returns>Subtotal value information.</returns>
        Task<GXSubtotalValue> ReadAsync(ClaimsPrincipal User, Guid id);

        /// <summary>
        /// Add subtotal values(s).
        /// </summary>
        /// <param name="User">Current user.</param>
        /// <param name="values">Added subtotal values.</param>
        Task<Guid[]> AddAsync(ClaimsPrincipal User, IEnumerable<GXSubtotalValue> values);
    }
}
