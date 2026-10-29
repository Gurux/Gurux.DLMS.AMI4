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
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle agent properties.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle agent properties.")]
    public interface IAgentPropertiesRepository
    {
        /// <summary>
        /// List agent properties.
        /// </summary>
        /// <returns>List of agent properties.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        [System.ComponentModel.Description("List agent properties.")]
        Task<IEnumerable<GXAgentProperty>> ListAsync(
            ListAgentProperties? request = null,
            ListAgentPropertiesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read agent properties.
        /// </summary>
        /// <param name="name">Property name.</param>
        /// <param name="columns">Read columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="moduleId">Module Id.</param>
        /// <param name="agentId">Agent Id.</param>
        /// <returns>The matching property, or <see langword="null"/> if it does not exist.</returns>
        [System.ComponentModel.Description("Read agent properties.")]
        Task<GXAgentProperty?> ReadAsync(string agentId, string name, string? moduleId = null, Expression<Func<GXAgentProperty, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new agent properties or updates existing ones.
        /// </summary>
        /// <param name="properties">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to insert new agent properties or update existing ones.</returns>
        [System.ComponentModel.Description("Inserts new agent properties or updates existing ones.")]
        Task UpsertAsync(
            IEnumerable<GXAgentProperty> properties,
            Expression<Func<GXAgentProperty, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete agent properties.
        /// </summary>
        /// <param name="ids">Agent properties to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete agent properties.</returns>
        [System.ComponentModel.Description("Delete agent properties.")]
        Task DeleteAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    }
}
