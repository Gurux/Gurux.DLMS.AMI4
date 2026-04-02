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

using System.Linq.Expressions;
using Gurux.DLMS.AMI.Shared.DTOs.Subtotal;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle subtotals.
    /// </summary>
    public interface ISubtotalRepository
    {
        /// <summary>
        /// List subtotals.
        /// </summary>
        /// <returns>Subtotals.</returns>
        Task<GXSubtotal[]> ListAsync(
            ListSubtotals? request,
            ListSubtotalsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read subtotal.
        /// </summary>
        /// <param name="id">Subtotal id.</param>
        /// <returns></returns>
        Task<GXSubtotal> ReadAsync(Guid id);

        /// <summary>
        /// Update subtotal(s).
        /// </summary>
        /// <param name="subtotals">Updated subtotal(s).</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            IEnumerable<GXSubtotal> subtotals,
            Expression<Func<GXSubtotal, object?>>? columns = null);

        /// <summary>
        /// Delete subtotal(s).
        /// </summary>
        /// <param name="subtotals">Subtotal(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(IEnumerable<Guid> subtotals, bool delete);

        /// <summary>
        /// Get all users that can access this subtotal.
        /// </summary>
        /// <param name="subtotalId">subtotal id.</param>
        /// <returns>Users.</returns>
        Task<List<string>> GetUsersAsync(Guid? subtotalId);

        /// <summary>
        /// Get all users that can access subtotals.
        /// </summary>
        /// <param name="subtotalIds">subtotal ids.</param>
        /// <returns>Users.</returns>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? subtotalIds);

        /// <summary>
        /// Calculate subtotal(s).
        /// </summary>
        /// <param name="subtotals">Calculated subtotals.</param>
        Task CalculateAsync(IEnumerable<Guid>? subtotals);

        /// <summary>
        /// Cancel subtotal(s) calculation.
        /// </summary>
        /// <param name="subtotals">Canceled subtotals.</param>
        Task CancelAsync(IEnumerable<Guid>? subtotals);

        /// <summary>
        /// Clear calculated subtotal(s).
        /// </summary>
        /// <param name="subtotals">Cleared subtotals.</param>
        Task ClearAsync(IEnumerable<Guid>? subtotals);
    }
}
