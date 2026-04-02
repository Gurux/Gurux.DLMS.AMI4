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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;
using System.Net;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This public interface is used to handle listed IP addresses.
    /// </summary>
    public interface IIpAddressRepository
    {
        /// <summary>
        /// List IP address.
        /// </summary>
        Task<GXIpAddress[]> ListAsync(
            ListIpAddress? request,
            ListIpAddressResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read IP address information.
        /// </summary>
        /// <param name="id">IP address id.</param>
        Task<GXIpAddress> ReadAsync(Guid id);

        /// <summary>
        /// Update IP addresses.
        /// </summary>
        /// <param name="list">List of IP addresses to update.</param>
        /// <param name="columns">Updated column(s).</param>
        Task<Guid[]> UpdateAsync(
            IEnumerable<GXIpAddress> list,
            Expression<Func<GXIpAddress, object?>>? columns = null);

        /// <summary>
        /// Delete IP addresses.
        /// </summary>
        /// <param name="list">List of IP addresses to delete.</param>
        Task DeleteAsync(IEnumerable<Guid> list);       
    }
}
