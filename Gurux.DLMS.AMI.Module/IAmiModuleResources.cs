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

using System.Globalization;

namespace Gurux.DLMS.AMI.Module;

/// <summary>Validated host operations on AMI-owned resources, bound to one module.</summary>
public interface IAmiModuleResources
{
    /// <summary>Returns the translated text, or the original text when no translation is available.</summary>
    /// <param name="cultureInfo">Culture used to select the translation language.</param>
    /// <param name="name">Original resource text.</param>
    /// <returns>The localized text.</returns>
    Task<string> GetLocalizedTextAsync(CultureInfo cultureInfo, string name);

    /// <summary>Creates missing capture policy, log type and menu defaults without overwriting user edits.</summary>
    /// <param name="definition">Policy, log type and menu defaults to ensure for this module.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task EnsureCaptureAsync(AmiModuleCapture definition, CancellationToken cancellationToken = default);
    /// <summary>Resolves and removes the log menu link owned by this module.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task RemoveLogMenuAsync(CancellationToken cancellationToken = default);
    /// <summary>Appends logs under this module's source and publishes committed changes.</summary>
    /// <param name="logs">Log entries to append under the calling module's source.</param>
    /// <param name="recipients">User identifiers to notify after the log entries are committed.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task containing the number of log entries appended by the host.</returns>
    Task<int> AddLogsAsync(IReadOnlyList<AmiModuleLog> logs, IReadOnlyList<string> recipients, CancellationToken cancellationToken = default);
    /// <summary>Deletes only this module's log entries.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task RemoveLogsAsync(CancellationToken cancellationToken = default);
    /// <summary>Creates this module's notification type and missing administrator notifications.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    Task EnsureNotificationDefaultsAsync(CancellationToken cancellationToken = default);
    /// <summary>Removes only the notification channel created by this module and its subscriptions.</summary>
    /// <remarks>The default implementation completes without removing resources.</remarks>
    /// <param name="cancellationToken">Token used to cancel removal.</param>
    /// <returns>A task that completes when notification defaults have been removed.</returns>
    Task RemoveNotificationDefaultsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
