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

using Gurux.DLMS.AMI.Client.Helpers.Selection;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.Rest;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Adapts the authorized script repository to method-level paging without sending entire script lists to the browser.</summary>
public static class GXRelationshipMethodPaging
{
    /// <summary>Counts matching methods while retaining only the requested page in memory.</summary>
    /// <param name="request">The method-level filter and page.</param>
    /// <param name="provider">Retrieves bounded authorized script batches with their methods.</param>
    /// <param name="cancellationToken">Cancels scanning or repository calls.</param>
    /// <returns>The requested method page and the matching count.</returns>
    /// <remarks>Ordering is stable by script identity followed by method identity. Counting scans matching scripts on the server.</remarks>
    public static async Task<GXRelationshipMethodPage> ReadAsync(GXRelationshipMethodRequest request,
        Func<ListScripts, CancellationToken, Task<ListScriptsResponse>> provider, CancellationToken cancellationToken)
    {
        var result = new GXRelationshipMethodPage();
        if (request.Included is { Length: 0 })
        {
            return result;
        }
        var included = request.Included?.ToHashSet();
        var excluded = request.Excluded?.ToHashSet() ?? [];
        int pageSize = Math.Clamp(request.Count, 1, 1000);
        int startIndex = Math.Max(0, request.Index);
        const int batchSize = 100;
        long scriptIndex = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await provider(new ListScripts
            {
                Index = scriptIndex, Count = batchSize, AllUsers = request.AllUsers,
                Filter = new GXScript(null) { Name = request.ScriptName }, OrderBy = nameof(GXScript.Id)
            }, cancellationToken);
            var batch = response.Scripts?.ToArray() ?? [];
            if (batch.Length == 0)
            {
                break;
            }
            foreach (var script in batch)
            {
                foreach (var method in (script.Methods ?? []).OrderBy(method => method.Id))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if ((included != null && !included.Contains(method.Id)) || excluded.Contains(method.Id))
                    {
                        continue;
                    }
                    if (result.Count >= startIndex && result.Items.Count < pageSize)
                    {
                        method.Script = new GXScript(null) { Id = script.Id, Name = script.Name };
                        result.Items.Add(method);
                    }
                    ++result.Count;
                }
            }
            scriptIndex += batch.Length;
            if (scriptIndex >= response.Count)
            {
                break;
            }
        }
        return result;
    }
}
