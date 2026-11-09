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

using System.Security.Cryptography;
using System.Text.Json;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Conditional responses for the requesting user's visible active blocks.</summary>
internal static class GXBlockHttpCache
{
    public static ActionResult<ListBlocksResponse> Response(ControllerBase controller, ListBlocksResponse result)
    {
        var etag = ETag(result);
        controller.Response.Headers.ETag = etag;
        controller.Response.Headers.CacheControl = "private, no-store";
        if (controller.Request.Headers.IfNoneMatch.SelectMany(value => (value ?? "").Split(',', StringSplitOptions.TrimEntries))
            .Any(value => value == etag || value == "W/" + etag || value == "*"))
        {
            return controller.StatusCode(StatusCodes.Status304NotModified);
        }
        return result;
    }

    public static string ETag(ListBlocksResponse result)
    {
        // The repository has already applied active, closed and permission filters.
        var bytes = JsonSerializer.SerializeToUtf8Bytes(result, JsonSerializerOptions.Web);
        return "\"" + Convert.ToHexString(SHA256.HashData(bytes)) + "\"";
    }
}
