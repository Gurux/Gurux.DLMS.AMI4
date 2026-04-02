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

namespace Gurux.DLMS.AMI.Module
{
    /// <summary>
    /// Defines methods for initializing, saving, and managing the settings of an AMI module.
    /// </summary>
    /// <remarks>Implement this interface to provide initialization, persistence, and cancellation
    /// functionality for AMI module settings. Methods support both synchronous and asynchronous initialization, as well
    /// as saving and discarding changes.</remarks>
    public interface IAmiModuleSettings
    {
        /// <summary>
        /// Initializes the component using the specified settings.
        /// </summary>
        /// <param name="settings">An optional string containing configuration settings for initialization. If <see langword="null"/> or empty,
        /// default settings are used.</param>
        void Initialize(string? settings);

        /// <summary>
        /// Asynchronously initializes the component using the specified settings.
        /// </summary>
        /// <param name="settings">An optional string containing initialization settings. If <see langword="null"/> or empty, default settings
        /// are used.</param>
        /// <returns>A task that represents the asynchronous initialization operation.</returns>
        Task InitializeAsync(string? settings);

        /// <summary>
        /// Saves the current data to the underlying storage.
        /// </summary>
        /// <returns>A string containing the data of the saved resource.</returns>
        string? Save();

        /// <summary>
        /// Saves the current data to the underlying storage.
        /// </summary>
        /// <returns>A string containing the data of the saved resource.</returns>
        Task<string?> SaveAsync();

        /// <summary>
        /// Cancel changes.
        /// </summary>
        void Cancel();

        /// <summary>
        /// Cancel changes.
        /// </summary>
        Task CancelAsync();

    }
}