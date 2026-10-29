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
    /// Provides persistence and lookup operations for enumeration type definitions.
    /// </summary>
    [System.ComponentModel.Description("Provides persistence and lookup operations for enumeration type definitions.")]
    public interface IEnumTypeRepository
    {
        /// <summary>Add missing built-in enumeration definitions without changing existing entries. Requires administrator role.</summary>
        /// <returns>A task representing completion of the request to add missing built-in enumeration definitions without changing existing entries. Requires administrator role.</returns>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        Task EnsureDefaultsAsync(CancellationToken cancellationToken = default);

        /// <summary>Restore the values of selected known defaults while preserving their identifiers. Requires administrator role.</summary>
        /// <returns>A task representing completion of the request to restore the values of selected known defaults while preserving their identifiers. Requires administrator role.</returns>
        /// <param name="ids">Identifiers of existing built-in resources to reset. The selection must not be empty.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        Task ResetToDefaultsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the enum type. The new type is added if it's not found.
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="name">Name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The database identifier of the existing or newly registered enumeration definition.</returns>
        [System.ComponentModel.Description("Get the enum type. The new type is added if it's not found.")]
        Task<int> GetLogTypeAsync(string type,
            string name,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the enum type. The new type is added if it's not found.
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="value">Enumeration value whose name is registered.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The database identifier of the existing or newly registered enumeration definition.</returns>
        [System.ComponentModel.Description("Get the enum type. The new type is added if it's not found.")]
        Task<int> GetLogTypeAsync(string type,
            Enum value,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get available enum types.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="request">The request parameters.</param>
        /// <param name="response">The response parameters.</param>
        /// <returns>A task whose result contains the matching enumeration types.</returns>
        [System.ComponentModel.Description("Get available enum types.")]
        Task<IEnumerable<GXEnumType>> ListAsync(ListEnumTypes? request = null,
            ListEnumTypesResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete enum type.
        /// </summary>
        /// <param name="type">Enumeration type name whose definitions are deleted.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete enum type.</returns>
        [System.ComponentModel.Description("Delete enum type.")]
        Task DeleteAsync(string type,
            CancellationToken cancellationToken = default);

    }
}
