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
    public interface IBlockRepository
    {
        /// <summary>
        /// List blocks.
        /// </summary>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Blocks.</returns>
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
        Task<GXBlock> ReadAsync(Guid id, Expression<Func<GXBlock, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update block(s).
        /// </summary>
        /// <param name="blocks">Updated block(s).</param>
        /// <param name="columns">Updated columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<IEnumerable<Guid>> UpdateAsync(
            IEnumerable<GXBlock> blocks,
            Expression<Func<GXBlock, object>>? columns = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete block(s).
        /// </summary>
        /// <param name="blocks">Block(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task DeleteAsync(IEnumerable<Guid> blocks, bool delete = true, bool notify = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Close block(s).
        /// </summary>
        /// <param name="blocks">Blocks to close.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task CloseAsync(IEnumerable<Guid>? blocks, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access this block.
        /// </summary>
        /// <param name="blockId">Block id.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(Guid? blockId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all users that can access blocks.
        /// </summary>
        /// <param name="blockIds">Block ids.</param>
        /// <returns>Operation result.</returns>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<List<string>> GetUsersAsync(IEnumerable<Guid>? blockIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Regenerate blocks(s).
        /// </summary>
        /// <param name="blocks">Block(s) to regenerate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task RegenerateAsync(IEnumerable<Guid>? blocks,
            CancellationToken cancellationToken);
    }
}
