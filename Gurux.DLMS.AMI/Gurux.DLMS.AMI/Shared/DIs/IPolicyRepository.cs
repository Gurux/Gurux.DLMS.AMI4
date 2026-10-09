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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle policies.
    /// </summary>
    public interface IPolicyRepository
    {
        /// <summary>
        /// List policies.
        /// </summary>
        /// <returns>Policys.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        Task<IEnumerable<GXPolicy>> ListAsync(
            ListPolicys? request = null,
            ListPolicysResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read policy.
        /// </summary>
        /// <param name="id">Policy id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<GXPolicy> ReadAsync(Guid id, Expression<Func<GXPolicy, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Read by policy name.
        /// </summary>
        /// <param name="name">Policy name.</param>
        /// <returns>Operation result.</returns>
        Task<GXPolicy> ReadByNameAsync(string name);

        /// <summary>
        /// Update policy(s).
        /// </summary>
        /// <param name="policies">Updated policy(s).</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> UpdateAsync(IEnumerable<GXPolicy> policies, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete policy(s).
        /// </summary>
        /// <param name="policies">Policy(s) to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<Guid> policies, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously restores the default policies.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>true if the refresh succeeds; otherwise, false.</returns>
        Task<bool> RestoreDefaultPolicies(CancellationToken cancellationToken = default);
    }
}
