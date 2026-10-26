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

namespace Gurux.DLMS.AMI.Module;

/// <summary>Validated host operations on AMI-owned resources, bound to one module.</summary>
public interface IAmiModuleResources
{
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
}
