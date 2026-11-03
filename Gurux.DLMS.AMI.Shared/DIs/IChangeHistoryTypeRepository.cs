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

using Gurux.DLMS.AMI.Shared.DTOs.History;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// Provides persistence and lookup operations for history type definitions.
    /// </summary>
    [System.ComponentModel.Description("Provides persistence and lookup operations for history type definitions.")]
    public interface IChangeHistoryTypeRepository
    {
        /// <summary>Adds missing built-in change history types without modifying existing definitions.</summary>
        /// <remarks>
        /// Requires administrator access. The built-in defaults are Schedule, Device, User, Gateway, Script and Agent.
        /// Each default uses its persistent registered GUID, its built-in name and RecordHistory set to false.
        /// Existing definitions, including customized names and recording settings, are preserved, as are custom types.
        /// Repeated calls do not add duplicates. The additions are committed in one transaction.
        /// </remarks>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A task representing completion of the addition of missing default types.</returns>
        [System.ComponentModel.Description("Adds missing default history types: Schedule, Device, User, Gateway, Script and Agent. Requires administrator access and preserves existing settings.")]
        Task EnsureDefaultsAsync(CancellationToken cancellationToken = default);

        /// <summary>Restores the built-in names and recording settings of selected existing default history types.</summary>
        /// <remarks>
        /// Requires administrator access. Only Schedule, Device, User, Gateway, Script and Agent defaults are eligible,
        /// identified by their persistent registered GUIDs rather than their current names.
        /// Restores the built-in name and sets RecordHistory to false. Identifiers, history items, revision records,
        /// unselected definitions and custom types are preserved. The selection must not be empty.
        /// Duplicate identifiers are processed once. Unknown, empty or missing identifiers cause the entire operation
        /// to fail without resetting any types. Use EnsureDefaultsAsync to recreate missing default definitions.
        /// All selected changes are committed in one transaction.
        /// </remarks>
        /// <param name="ids">Stable GUIDs of existing built-in history types to reset.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A task representing completion of the reset of the selected default types.</returns>
        [System.ComponentModel.Description("Restores selected default history type names and disables recording, preserving identifiers, history items and revision records. Requires administrator access.")]
        Task ResetToDefaultsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists history type definitions using the requested filters and paging.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">Optional filtering, ordering and paging criteria.</param>
        /// <param name="response">Optional response populated with the selected page and total matching count before paging.</param>
        /// <returns>A task whose result contains the matching history types.</returns>
        [System.ComponentModel.Description("Lists history type definitions using the requested filters and paging.")]
        Task<IEnumerable<GXChangeHistoryType>> ListAsync(ListChangeHistoryTypes? request = null,
            ListChangeHistoryTypesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads a history type definition by its stable GUID.
        /// </summary>
        /// <param name="id">The stable GUID of the history type definition.</param>
        /// <param name="columns">Properties to retrieve, or null to use the default projection.</param>
        /// <returns>A task whose result contains the requested history type definition.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Reads a history type definition by its stable GUID.")]
        Task<GXChangeHistoryType> ReadAsync(Guid id, Expression<Func<GXChangeHistoryType, object>>? columns = null, CancellationToken cancellationToken = default);


        /// <summary>
        /// Inserts new history type definitions or updates existing ones.
        /// </summary>
        /// <remarks>
        /// Existing definitions are matched by their stable GUID, which must remain unchanged when updating a definition.
        /// The Items navigation collection is excluded from inserts and updates; this operation does not modify history items.
        /// </remarks>
        /// <param name="types">The history type definitions to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing definitions, or <see langword="null"/> to use the default update selection. The stable GUID and Items navigation collection are not updateable.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the inserted or updated records.</returns>
        [System.ComponentModel.Description("Inserts new history type definitions or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXChangeHistoryType> types,
            Expression<Func<GXChangeHistoryType, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Permanently deletes history type definitions and their associated history items and revision records.
        /// </summary>
        /// <remarks>Deleting a history type cascades to all history items and revision records that reference it.</remarks>
        /// <param name="types">Stable GUIDs of the history type definitions to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the deletion.</returns>
        [System.ComponentModel.Description("Permanently deletes history type definitions and their associated history items and revision records.")]
        Task DeleteAsync(IEnumerable<Guid> types, CancellationToken cancellationToken = default);

    }
}
