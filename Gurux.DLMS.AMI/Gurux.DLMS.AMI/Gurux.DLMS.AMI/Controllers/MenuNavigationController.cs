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

using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gurux.DLMS.AMI.Server.Controllers;

/// <summary>Discovers navigation menus assigned to a layout region.</summary>
[ApiController]
[Route("api/Menu/Navigation")]
[Authorize]
public sealed class MenuNavigationController(IMenuRepository menus, IDbConnectionFactory database) : ControllerBase
{
    /// <summary>Returns placed menus and links filtered for the requesting user's groups and policies.</summary>
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<IEnumerable<GetMenuResponse>>> Get(CancellationToken cancellationToken = default)
    {
        await using var connection = await database.GetConnectionAsync(cancellationToken);
        var query = GXSelectArgs.Select<GXMenu>(m => new { m.Id, m.Name, m.Order },
            m => (m.Kind == MenuKind.Navigation || m.Kind == MenuKind.MenuBar) && m.Location != null && m.Enabled == true && m.Removed == null);
        query.OrderBy.Add<GXMenu>(m => m.Order);
        query.OrderBy.Add<GXMenu>(m => m.Name!);
        var result = new List<GetMenuResponse>();
        foreach (var candidate in await connection.SelectAsync<GXMenu>(query, cancellationToken))
        {
            // The built-in sidebar already has its own consumer.
            if (candidate.Name == GXMenus.NavMenu)
            {
                continue;
            }
            // ReadAsync applies the same group and resource/link policy checks as named menus.
            var menu = await menus.ReadAsync(candidate.Id, cancellationToken: cancellationToken);
            if (menu.Id == Guid.Empty || (menu.Kind != MenuKind.Navigation && menu.Kind != MenuKind.MenuBar) || menu.Location == null ||
                menu.Enabled != true || menu.Removed != null || menu.PublishTime > DateTimeOffset.Now)
            {
                continue;
            }

            menu.Links = [.. (menu.Links ?? []).Where(l => l.Enabled == true && l.Removed == null).OrderBy(l => l.Order)];
            result.Add(new GetMenuResponse { Item = menu });
        }
        return result;
    }
}
