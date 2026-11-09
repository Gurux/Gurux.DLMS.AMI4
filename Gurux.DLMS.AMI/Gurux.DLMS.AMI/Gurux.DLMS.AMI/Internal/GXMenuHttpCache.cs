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
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Gurux.DLMS.AMI.Server.Internal;

internal static class GXMenuHttpCache
{
    public static ActionResult<GetMenuResponse> Response(ControllerBase controller, GXMenu menu)
    {
        var result = new GetMenuResponse { Item = menu };
        var etag = ETag(menu);
        controller.Response.Headers.ETag = etag;
        controller.Response.Headers.CacheControl = "private, no-store";
        if (controller.Request.Headers.IfNoneMatch.SelectMany(value => (value ?? "").Split(',', StringSplitOptions.TrimEntries))
            .Any(value => value == etag || value == "W/" + etag || value == "*"))
        {
            return controller.StatusCode(StatusCodes.Status304NotModified);
        }

        return result;
    }

    public static string ETag(GXMenu menu)
    {
        var result = new GetMenuResponse { Item = menu };
        // The representation is already filtered for the requesting user by MenuRepository.
        var hash = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(result, JsonSerializerOptions.Web));
        return "\"" + Convert.ToHexString(hash) + "\"";
    }
}
