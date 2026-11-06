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

namespace Gurux.DLMS.AMI.Module
{
    /// <summary>
    /// Defines initialization, serialization, and cancellation operations for a module settings editor.
    /// </summary>
    /// <remarks>Save operations return serialized settings for the caller to persist.</remarks>
    public interface IGXAmiModuleSettings
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
