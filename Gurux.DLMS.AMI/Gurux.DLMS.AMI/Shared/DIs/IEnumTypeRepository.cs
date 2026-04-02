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

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to handle log types.
    /// </summary>
    public interface IEnumTypeRepository
    {
        /// <summary>
        /// Get the enum type. The new type is added if it's not found.
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="name">Name.</param>
        /// <returns></returns>
        Task<int> GetLogTypeAsync(string type, string name);

        /// <summary>
        /// Get the enum type. The new type is added if it's not found.
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="value">Enumrated value.</param>
        /// <returns></returns>
        Task<int> GetLogTypeAsync(string type, Enum value);

        /// <summary>
        /// Get available enum types.
        /// </summary>
        Task<IEnumerable<GXEnumType>> ListAsync(ListEnumTypes? request,
            ListEnumTypesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete enum type.
        /// </summary>
        /// <param name="type">Deleted logs.</param>
        Task DeleteAsync(string type);

    }
}
