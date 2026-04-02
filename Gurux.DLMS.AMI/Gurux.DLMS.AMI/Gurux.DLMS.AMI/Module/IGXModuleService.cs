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

using Gurux.DLMS.AMI.Shared.DTOs.Module;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Module
{
    /// <summary>
    /// Module service methods.
    /// </summary>
    public interface IGXModuleService
    {
        /// <summary>
        /// Add new module.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="compressedFile">Name of the compressed file.</param>
        /// <returns></returns>
        Task<List<GXModule>> AddModuleAsync(ClaimsPrincipal? user, string compressedFile);

        /// <summary>
        /// Enable installed module.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="module">Module to enable.</param>
        /// <returns>True, if server restart is required before module can be used.</returns>
        public Task<bool> EnableModuleAsync(ClaimsPrincipal? user, GXModule module);

        /// <summary>
        /// Disable installed module.
        /// </summary>
        /// <param name="user">Current user.</param>
        /// <param name="module">Module to disable.</param>
        /// <returns>True, if server restart is required before module can be unloaded.</returns>
        public Task<bool> DisableModuleAsync(ClaimsPrincipal? user, GXModule module);

        /// <summary>
        /// Update module settings for the module.
        /// </summary>
        /// <param name="module">Module to update.</param>
        public Task UpdateModuleSettingsAsync(GXModule module);

        /// <summary>
        /// Delete installed module.
        /// </summary>
        /// <param name="module">Deleted module.</param>
        /// <returns>True, if server restart is required before module can be unloaded.</returns>
        public bool DeleteModule(GXModule module);

        /// <summary>
        /// Execute module async.
        /// </summary>
        /// <param name="module">Module to execute.</param>
        /// <param name="settings">Instance depending module settings.</param>
        public Task ExecuteAsync(GXModule module,
            string? settings);       
    }
}
