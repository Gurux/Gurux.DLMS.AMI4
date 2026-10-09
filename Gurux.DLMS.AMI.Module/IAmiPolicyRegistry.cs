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
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Module
{
    /// <summary>
    /// Defines methods for managing authorization policies by name.
    /// </summary>
    public interface IAmiPolicyRegistry
    {
        /// <summary>
        /// Retrieves an authorization policy by its name.
        /// </summary>
        /// <param name="name">The name of the authorization policy to retrieve.</param>
        /// <returns>The authorization policy associated with the specified name, or null if not found.</returns>
        AuthorizationPolicy? Get(string name);

        /// <summary>
        /// Adds a new authorization policy associated with the specified name.
        /// </summary>
        /// <param name="name">The name of the authorization policy to add.</param>
        /// <param name="policy">The authorization policy to associate with the specified name.</param>
        void Add(string name, AuthorizationPolicy policy);

        /// <summary>
        /// Removes the authorization policy associated with the specified name.
        /// </summary>
        /// <param name="name">The name of the authorization policy to remove.</param>
        /// <returns></returns>
        bool Remove(string name);

        /// <summary>
        /// Retrieves all policy names.
        /// </summary>
        /// <returns>A read-only collection of policy names.</returns>
        IReadOnlyCollection<string> GetAllPolicyNames();
    }
}
