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

using System.Linq.Expressions;
using System.Net.Http.Json;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Http;

namespace Gurux.DLMS.AMI.Agent.Worker.Repositories;

/// <summary>Forwards log operations to the common log API.</summary>
class GXLogRepository : ILogRepository
{
    /// <summary>Optional component name attached to persisted log entries.</summary>
    private readonly string? _source;
    /// <summary>Sends requests to the configured AMI server.</summary>
    private readonly HttpClient _http;
    /// <summary>Creates an HTTP-backed log repository with optional component scoping.</summary>
    /// <param name="http">Configured HTTP client used for server communication.</param>
    public GXLogRepository(HttpClient http) { this._http = http; }
    /// <summary>Creates an HTTP-backed log repository with optional component scoping.</summary>
    /// <param name="http">Configured HTTP client used for server communication.</param>
    /// <param name="source">Original value copied without sharing mutable state.</param>
    private GXLogRepository(HttpClient http, string? source) : this(http) => _source = source;
    /// <inheritdoc/>
    public ILogRepository ForSource(string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        return new GXLogRepository(_http, source);
    }
    /// <summary>Builds the log endpoint with the repository's optional source filter.</summary>
    /// <param name="action">Queued device work or repository action being processed.</param>
    /// <returns>A log endpoint containing the optional escaped component source filter.</returns>
    private string Url(string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(_source);
        return "/api/Log/" + action + "?source=" + Uri.EscapeDataString(_source);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Guid>> AddAsync(string type, IEnumerable<GXLog> logs, CancellationToken cancellationToken = default)
    {
        var rows = logs.ToArray();
        var response = await _http.PostAsJson<AddLogResponse>(Url("Add"),
            new AddLog { Logs = rows, Type = type }, cancellationToken);
        return response?.Ids ?? [];
    }

    /// <inheritdoc/>
    public async Task ClearAsync(ClearLog? request = null, CancellationToken cancellationToken = default)
    {
        _ = await _http.PostAsJson<ClearLogResponse>(Url("Clear"), request ?? new ClearLog(), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task CloseAsync(IEnumerable<Guid>? logs, CancellationToken cancellationToken = default)
        => _ = await _http.PostAsJson<CloseLogResponse>(Url("Close"),
            new CloseLog { Ids = logs?.ToArray() }, cancellationToken);

    /// <inheritdoc/>
    public async Task DeleteAsync(IEnumerable<Guid>? logs, bool delete = true, bool notify = true,
        CancellationToken cancellationToken = default)
        => _ = await _http.PostAsJson<RemoveLogResponse>(Url("Delete"),
            new RemoveLog { Ids = logs?.ToArray(), Delete = delete }, cancellationToken);

    /// <inheritdoc/>
    public async Task<IEnumerable<GXLog>> ListAsync(ListLogs? request = null, ListLogsResponse? response = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _http.PostAsJson<ListLogsResponse>(Url("List"), request ?? new ListLogs(), cancellationToken);
        if (response != null && result != null)
        { response.Count = result.Count; response.Logs = result.Logs; }
        return result?.Logs ?? [];
    }

    /// <inheritdoc/>
    public async Task<GXLog> ReadAsync(Guid id, Expression<Func<GXLog, object>>? columns = null, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync(Url("") + "&id=" + id, cancellationToken);
        await response.EnsureSuccessAsync(cancellationToken);
        return (await response.Content.ReadFromJsonAsync<GetLogResponse>(cancellationToken))?.Item
            ?? throw new HttpRequestException("Log was not returned.");
    }
}
