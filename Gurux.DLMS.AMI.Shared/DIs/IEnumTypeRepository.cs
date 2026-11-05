// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

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
