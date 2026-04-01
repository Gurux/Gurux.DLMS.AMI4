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

namespace Gurux.DLMS.AMI.Components
{
    /// <summary>
    /// Local storage is used to save data to local storage.
    /// </summary>
    public interface IGXLocalStorage
    {
        /// <summary>
        /// Set value to local storage.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value</param>
        Task SetValueAsync(
            string key,
            string? value);

        /// <summary>
        /// Set value to local storage.
        /// </summary>
        /// <param name="group">Key group.</param>
        /// <param name="key">Key.</param>
        /// <param name="value">Value</param>
        Task SetValueAsync(
            string group,
            string key,
            string? value);

        /// <summary>
        /// Get value from local storage.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Value from local storage.</returns>
        Task<string?> GetValueAsync(
            string key);

        /// <summary>
        /// Get value from local storage.
        /// </summary>
        /// <param name="group">Key group.</param>
        /// <param name="key">Key</param>
        /// <returns>Value from local storage.</returns>
        Task<string?> GetValueAsync(
            string group,
            string key);

        /// <summary>
        /// Remove value from local storage.
        /// </summary>
        /// <param name="key">Key</param>
        Task RemoveAsync(string key);

        /// <summary>
        /// Remove value from local storage.
        /// </summary>
        /// <param name="group">Key group.</param>
        /// <param name="key">Key</param>
        Task RemoveAsync(string group, string key);

        /// <summary>
        /// Clear values from local storage.
        /// </summary>
        /// <param name="group">Key group.</param>
        Task ClearAsync(string group);

        /// <summary>
        /// Clear values from local storage.
        /// </summary>
        Task ClearAsync();
    }
}