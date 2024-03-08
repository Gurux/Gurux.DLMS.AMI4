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
    /// Cookie storage is used to save user cookies.
    /// </summary>
    public interface IGXCookieStorage
    {
        /// <summary>
        /// Set cookie value.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value</param>
        /// <param name="days">Expiration days.</param>
        Task SetValueAsync(
            string key,
            string? value,
            int days = 30);

        /// <summary>
        /// Get cookie value.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="def">Default value.</param>
        /// <returns>Cookie value.</returns>
        Task<string?> GetValueAsync(
            string key,
            string? def = null);
    }
}