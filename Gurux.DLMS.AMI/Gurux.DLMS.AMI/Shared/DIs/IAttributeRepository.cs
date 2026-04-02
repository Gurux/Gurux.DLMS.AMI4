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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle attributes.
    /// </summary>
    public interface IAttributeRepository
    {
        /// <summary>
        /// List attributes.
        /// </summary>
        /// <returns>Attributes.</returns>
        Task<GXAttribute[]> ListAsync(
            ListAttributes? request,
            ListAttributesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read attribute information.
        /// </summary>
        /// <param name="id">Attribute id.</param>
        /// <returns>Attribute information.</returns>
        Task<GXAttribute> ReadAsync(Guid id);

        /// <summary>
        /// Update attribute(s).
        /// </summary>
        /// <param name="attributers">Updated attribute(s).</param>
        /// <param name="columns">Updated columns(s).</param>
        Task<Guid[]> UpdateAsync(
            IEnumerable<GXAttribute> attributers,
            Expression<Func<GXAttribute, object?>>? columns = null);

        /// <summary>
        /// Delete attribute(s).
        /// </summary>
        /// <param name="attributes">Attribute(s) to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task DeleteAsync(IEnumerable<Guid> attributes, bool delete);

        /// <summary>
        /// Update attribute datatype.
        /// </summary>
        /// <param name="attributes"></param>
        /// <returns></returns>
        Task UpdateDatatypeAsync(IEnumerable<GXAttribute> attributes);
    }
}
