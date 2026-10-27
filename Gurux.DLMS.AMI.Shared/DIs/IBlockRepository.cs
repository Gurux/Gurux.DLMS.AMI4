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
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle blocks.
    /// </summary>
    [System.ComponentModel.Description("This interface is used to handle blocks.")]
    public interface IBlockRepository
    {
        /// <summary>
        /// List blocks.
        /// </summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Blocks.</returns>
        [System.ComponentModel.Description("List blocks.")]
        Task<IEnumerable<GXBlock>> ListAsync(
            ListBlocks? request = null,
            ListBlocksResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read block.
        /// </summary>
        /// <param name="id">Block id.</param>
        /// <param name="columns">Read columns.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Read block.")]
        Task<GXBlock> ReadAsync(Guid id, Expression<Func<GXBlock, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts new blocks or updates existing ones.
        /// </summary>
        /// <param name="blocks">The records to insert or update.</param>
        /// <param name="columns">The properties to include when updating existing records, or <see langword="null"/> to use the repository's default update selection. Repository-managed and read-only properties may be excluded.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Inserts new blocks or updates existing ones.")]
        Task<IEnumerable<Guid>> UpsertAsync(
            IEnumerable<GXBlock> blocks,
            Expression<Func<GXBlock, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>Delete blocks.</summary>
        /// <param name="blocks">Block(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Delete blocks.")]
        Task DeleteAsync(IEnumerable<Guid> blocks, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>Close blocks.</summary>
        /// <param name="blocks">Blocks to close.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Close blocks.")]
        Task CloseAsync(IEnumerable<Guid>? blocks, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this block.
        /// </summary>
        /// <param name="blockId">Block id.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access this block.")]
        Task<List<string>> GetUsersAsync(Guid? blockId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access blocks.
        /// </summary>
        /// <param name="blockIds">Block ids.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        [System.ComponentModel.Description("Get all users that can access blocks.")]
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? blockIds, CancellationToken cancellationToken = default);

        /// <summary>Ensure missing defaults exist without changing existing resources.</summary>
        [System.ComponentModel.Description("Ensure missing defaults exist without changing existing resources.")]
        Task EnsureDefaultsAsync(CancellationToken cancellationToken = default);

        /// <summary>Reset selected existing built-in resources in place, preserving identity and references.
        /// The selection must be nonempty and every target must be valid before any writes.</summary>
        [System.ComponentModel.Description("Reset selected existing built-in resources in place, preserving identity and references. The selection must be nonempty and every target must be valid before any writes.")]
        Task ResetToDefaultsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    }
}
