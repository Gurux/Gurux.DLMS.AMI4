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
    /// Defines initialization, serialization, and cancellation operations for a module settings editor.
    /// </summary>
    /// <remarks>Save operations return serialized settings for the caller to persist.</remarks>
    public interface IAmiModuleSettings
    {
        /// <summary>
        /// Initializes the component using the specified settings.
        /// </summary>
        /// <param name="settings">Serialized configuration, or <see langword="null"/> when no saved configuration is supplied.</param>
        void Initialize(string? settings);

        /// <summary>
        /// Asynchronously initializes the component using the specified settings.
        /// </summary>
        /// <param name="settings">Serialized configuration, or <see langword="null"/> when no saved configuration is supplied.</param>
        /// <returns>A task that represents the asynchronous initialization operation.</returns>
        Task InitializeAsync(string? settings);

        /// <summary>
        /// Validates the current settings and returns their serialized representation for persistence by the caller.
        /// </summary>
        /// <returns>Serialized settings, or <see langword="null"/> when no save result is produced.</returns>
        string? Save();

        /// <summary>
        /// Asynchronously validates the current settings and returns their serialized representation for persistence by the caller.
        /// </summary>
        /// <returns>A task containing serialized settings, or <see langword="null"/> when no save result is produced.</returns>
        Task<string?> SaveAsync();

        /// <summary>
        /// Cancels editing without producing settings to save.
        /// </summary>
        void Cancel();

        /// <summary>
        /// Asynchronously cancels editing without producing settings to save.
        /// </summary>
        /// <returns>A task that completes when the operation has finished.</returns>
        Task CancelAsync();

    }
}
