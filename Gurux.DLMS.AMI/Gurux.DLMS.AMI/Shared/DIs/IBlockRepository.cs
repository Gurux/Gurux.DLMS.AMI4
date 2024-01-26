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
using System.Security.Claims;
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
        /// <returns>Blocks.</returns>
        Task<GXBlock[]> ListAsync(
            ClaimsPrincipal user, 
            ListBlocks? request, 
            ListBlocksResponse? response,
            CancellationToken cancellationToken);

        /// <summary>
        /// Read block.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="id">Block id.</param>
        /// <returns></returns>
        Task<GXBlock> ReadAsync(ClaimsPrincipal user, Guid id);

        /// <summary>
        /// Update block(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="blocks">Updated block(s).</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            ClaimsPrincipal user, 
            IEnumerable<GXBlock> blocks,
            Expression<Func<GXBlock, object?>>? columns = null);

        /// <summary>
        /// Delete block(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="blocks">Block(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(ClaimsPrincipal user, IEnumerable<Guid> blocks, bool delete);

        /// <summary>
        /// Close block(s).
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="blocks">Blocks to close.</param>
        Task CloseAsync(ClaimsPrincipal user, IEnumerable<Guid> blocks);

        /// <summary>
        /// Get all users that can access this block.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="blockId">Block id.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal user, Guid? blockId);

        /// <summary>
        /// Get all users that can access blocks.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="blockIds">Block ids.</param>
        /// <returns></returns>
        Task<List<string>> GetUsersAsync(ClaimsPrincipal user, IEnumerable<Guid>? blockIds);
    }
}
