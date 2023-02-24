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

using Gurux.DLMS.AMI.Shared.DTOs.Authentication;

namespace Gurux.DLMS.AMI.Script
{
    public interface IGXAmi
    {
        /// <summary>
        /// Source object.
        /// </summary>
        object? Sender
        {
            get;
            set;
        }

        /// <summary>
        /// Received data.
        /// </summary>
        /// <remarks>
        /// This is used when meter sends push message.
        /// </remarks>
        object? Data
        {
            get;
            set;
        }

        /// <summary>
        /// User that owns the script.
        /// </summary>
        GXUser? User
        {
            get;
            set;
        }

        /// <summary>
        /// Select objects using filter.
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="filter">Filter</param>
        /// <returns>Found object or null if not found.</returns>
        T[] Select<T>(T filter);

        /// <summary>
        /// Select objects using filter.
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="filter">Filter</param>
        /// <returns>Found object or null if not found.</returns>
        Task<T[]> SelectAsync<T>(T filter);

        /// <summary>
        /// Get single object.
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="filter">Filter</param>
        /// <returns>Found object or null if not found.</returns>
        T? SingleOrDefault<T>(T filter);

        /// <summary>
        /// Get single object.
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="filter">Filter</param>
        /// <returns>Found object or null if not found.</returns>
        Task<T?> SingleOrDefaultAsync<T>(T filter);

        /// <summary>
        /// Adds new value for the database.
        /// </summary>
        /// <param name="value">Added value.</param>
        void Add(object value);

        /// <summary>
        /// Adds new value for the database.
        /// </summary>
        /// <param name="value">Added value.</param>
        Task AddAsync(object value);

        /// <summary>
        /// Remove value from the database.
        /// </summary>
        /// <param name="value">Removed value.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        Task RemoveAsync(object value, bool delete);

        /// <summary>
        /// Remove value from the database.
        /// </summary>
        /// <param name="value">Removed value.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        void Remove(object value, bool delete);

        /// <summary>
        /// Clear values from the database.
        /// </summary>
        /// <remarks>
        /// Only errors and logs can be cleared. 
        /// It's not possible to clear for example users or devices for security reasons.
        /// </remarks>
        Task ClearAsync<T>(IEnumerable<T>? items);

        /// <summary>
        /// Clear values from the database.
        /// </summary>
        /// <remarks>
        /// Only errors and logs can be cleared. 
        /// It's not possible to clear for example users or devices for security reasons.
        /// </remarks>
        void Clear<T>(IEnumerable<T>? items);

        /// <summary>
        /// Updates the content of the value for the database.
        /// </summary>
        /// <param name="value">Added value.</param>
        void Update(object value);

        /// <summary>
        /// Updates the content of the value for the database.
        /// </summary>
        /// <param name="value">Added value.</param>
        Task UpdateAsync(object value);

        /// <summary>
        /// Get required service by type.
        /// </summary>
        /// <param name="type">Required service type.</param>
        /// <returns>Required service or null if not found.</returns>
        object? GetService(Type type);

        /// <summary>
        /// Get required service.
        /// </summary>
        /// <typeparam name="T">Service type.</typeparam>
        /// <returns>Required service or null if not found.</returns>
        T? GetService<T>();
    }
}
