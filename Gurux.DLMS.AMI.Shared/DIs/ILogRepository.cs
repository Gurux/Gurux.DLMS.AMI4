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

using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.Rest;
using System.Linq.Expressions;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// Provides operations for recording, querying, closing, and deleting log entries.
    /// </summary>
    [System.ComponentModel.Description("Provides operations for recording, querying, closing, and deleting log entries.")]
    public interface ILogRepository
    {
        /// <summary>Returns a repository bound to an existing log type by source, name or public identifier.</summary>
        /// <param name="source">Existing log type source, name, or public identifier.</param>
        /// <returns>A repository that records entries for the selected log type.</returns>
        [System.ComponentModel.Description("Returns a repository bound to an existing log type by source, name or public identifier.")]
        ILogRepository ForSource(string source);

        /// <summary>Adds an exception as a log entry.</summary>
        /// <param name="type">Log type source, name, or public identifier.</param>
        /// <param name="error">Exception whose message and stack trace are recorded.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A task whose result is the recorded exception log entry.</returns>
        [System.ComponentModel.Description("Adds an exception as a log entry.")]
        async Task<GXLog> AddAsync(string type, Exception error, CancellationToken cancellationToken = default)
        {
            var log = new GXLog(System.Diagnostics.TraceLevel.Error) { Message = error.Message, StackTrace = error.StackTrace };
            await AddAsync(type, [log], cancellationToken);
            return log;
        }

        /// <summary>Adds an exception associated with the specified source entity.</summary>
        /// <param name="type">Log type source, name, or public identifier.</param>
        /// <param name="owner">Source entity associated with the exception.</param>
        /// <param name="error">Exception whose message and stack trace are recorded.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A task whose result is the recorded exception log entry.</returns>
        [System.ComponentModel.Description("Adds an exception associated with the specified source entity.")]
        async Task<GXLog> AddAsync(string type, object owner, Exception error, CancellationToken cancellationToken = default)
        {
            var log = new GXLog(System.Diagnostics.TraceLevel.Error) { Message = error.Message, StackTrace = error.StackTrace };
            if (owner is Gurux.DLMS.AMI.Shared.DTOs.Authentication.GXUser user)
            {
                log.User = user;
            }
            else if (owner is Gurux.DLMS.AMI.Shared.DTOs.Module.GXModule module)
            {
                log.SourceId = module.Id;
            }
            else if (owner is Gurux.Service.Orm.Common.IUnique<Guid> entity)
            {
                log.Creator = entity.Id;
            }
            else
            {
                throw new ArgumentException("The log owner must have a supported identifier.", nameof(owner));
            }
            await AddAsync(type, [log], cancellationToken);
            return log;
        }

        /// <summary>Deletes the specified log entries.</summary>
        /// <param name="logs">Log entries to delete, identified by their public identifiers.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A task representing completion of the request to delete the specified log entries.</returns>
        [System.ComponentModel.Description("Deletes the specified log entries.")]
        Task DeleteAsync(IEnumerable<GXLog> logs, CancellationToken cancellationToken = default)
            => DeleteAsync(logs.Select(log => log.PublicId), cancellationToken: cancellationToken);
        /// <summary>
        /// Add logs.
        /// </summary>
        /// <param name="type">Log type.</param>
        /// <param name="logs">Log entries to add.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the identifiers of the added records.</returns>
        [System.ComponentModel.Description("Add logs.")]
        Task<IEnumerable<Guid>> AddAsync(
            string type,
            IEnumerable<GXLog> logs,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// List log entries.
        /// </summary>
        /// <returns>Log entries.</returns>
        /// <param name="request">Optional filtering, sorting, and paging parameters.</param>
        /// <param name="response">Optional response populated with the list and total count.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        [System.ComponentModel.Description("List log entries.")]
        Task<IEnumerable<GXLog>> ListAsync(
            ListLogs? request = null,
            ListLogsResponse? response = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Read log entry.
        /// </summary>
        /// <param name="id">Public identifier of the log entry to read.</param>
        /// <param name="columns">Read columns.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task whose result contains the requested log.</returns>
        [System.ComponentModel.Description("Read log entry.")]
        Task<GXLog> ReadAsync(Guid id,
            Expression<Func<GXLog, object>>? columns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete log entries.
        /// </summary>
        /// <param name="logs">Public identifiers of the log entries to delete.</param>
        /// <param name="delete">If true, objects are deleted, not marked as removed.</param>
        /// <param name="notify">If true, notifications are sent for the deletion.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to delete log entries.</returns>
        [System.ComponentModel.Description("Delete log entries.")]
        Task DeleteAsync(IEnumerable<Guid>? logs, bool delete = true, bool notify = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Close log entries.
        /// </summary>
        /// <param name="logs">Public identifiers of the log entries to close.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to close log entries.</returns>
        [System.ComponentModel.Description("Close log entries.")]
        Task CloseAsync(IEnumerable<Guid>? logs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear all log entries.
        /// </summary>
        /// <param name="request">Source identifiers whose logs are cleared.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing completion of the request to clear all log entries.</returns>
        [System.ComponentModel.Description("Clear all log entries.")]
        Task ClearAsync(ClearLog? request = null, CancellationToken cancellationToken = default);
    }
}
