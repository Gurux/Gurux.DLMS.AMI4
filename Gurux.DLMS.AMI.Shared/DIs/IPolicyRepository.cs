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
    [System.ComponentModel.Description("This interface is used to handle policies.")]
    public interface IPolicyRepository
    {
        /// <summary>
        /// List policies.
        /// </summary>
        /// <returns>Policys.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List policies.")]
        Task<IEnumerable<GXPolicy>> ListAsync(
            ListPolicys? request = null,
            ListPolicysResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read policy.
        /// </summary>
        /// <param name="id">Policy id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>A task whose result contains the requested policy.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read policy.")]
        Task<GXPolicy> ReadAsync(Guid id, Expression<Func<GXPolicy, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Read by policy name.
        /// </summary>
        /// <param name="name">Policy name.</param>
        /// <returns>A task whose result contains the policy with the requested name.</returns>
        [System.ComponentModel.Description("Read by policy name.")]
        Task<GXPolicy> ReadByNameAsync(string name);

        /// <summary>
        /// Inserts new policies or updates existing ones.
        /// </summary>
        /// <param name="policies">The records to insert or update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new policies or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(IEnumerable<GXPolicy> policies, CancellationToken cancellationToken = default);

        /// <summary>Delete policies.</summary>
        /// <param name="policies">Policy(s) to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete policies.</returns>
        [System.ComponentModel.Description("Delete policies.")]
        Task DeleteAsync(IEnumerable<Guid> policies, CancellationToken cancellationToken = default);

        /// <summary>Add missing built-in defaults while preserving existing rows.</summary>
        /// <returns>A task representing completion of the request to add missing built-in defaults while preserving existing rows.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [System.ComponentModel.Description("Add missing built-in defaults while preserving existing rows.")]
        Task EnsureDefaultsAsync(CancellationToken cancellationToken = default);

        /// <summary>Reset selected built-ins while preserving identity and assignments.</summary>
        /// <returns>A task representing completion of the request to reset selected built-ins while preserving identity and assignments.</returns>
        /// <param name="policyIds">Identifiers of existing built-in policies to reset. The selection must not be empty.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [System.ComponentModel.Description("Reset selected built-ins while preserving identity and assignments.")]
        Task ResetToDefaultsAsync(IEnumerable<Guid> policyIds, CancellationToken cancellationToken = default);
    }
}