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

using System.Security.Claims;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Applies resource visibility independently of modification permissions.</summary>
internal static class GXAppearanceAccess
{
    // Appearances have no parent group; inherited visibility resolves to Private.
    public static bool IsOwnerOnly(GXAppearance appearance)
        => appearance.AccessVisibility is null or ResourceVisibility.Inherit or ResourceVisibility.Private;

    public static bool CanView(ClaimsPrincipal caller, GXAppearance appearance)
        => CanReadPrivate(caller, appearance) || appearance.AccessVisibility == ResourceVisibility.Public ||
            (appearance.AccessVisibility == ResourceVisibility.Internal && caller.Identity?.IsAuthenticated == true);

    public static bool CanReadPrivate(ClaimsPrincipal caller, GXAppearance appearance)
        => caller.Identity?.IsAuthenticated == true &&
            (caller.IsInRole(GXRoles.Admin) || (!string.IsNullOrEmpty(appearance.Creator?.Id) &&
                appearance.Creator.Id == ServerHelpers.GetUserId(caller, false)));

    public static void RequirePrivateOwner(ClaimsPrincipal caller, GXAppearance appearance)
    {
        if (!CanReadPrivate(caller, appearance)) {
            throw new UnauthorizedAccessException();
        }
    }

    public static void RequireAuthenticated(ClaimsPrincipal caller)
    {
        if (caller.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(ServerHelpers.GetUserId(caller, false)))
            {
            throw new UnauthorizedAccessException();
        }
    }

    public static void Restrict(GXSelectArgs query, ClaimsPrincipal caller)
    {
        if (caller.Identity?.IsAuthenticated == true && caller.IsInRole(GXRoles.Admin)) {
            return;
        }
        var callerId = ServerHelpers.GetUserId(caller, false);
        if (caller.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(callerId))
        {
            var owner = new GXUser { Id = callerId };
            query.Where.And<GXAppearance>(a => a.AccessVisibility == ResourceVisibility.Public ||
                a.AccessVisibility == ResourceVisibility.Internal || a.Creator == owner);
        }
        else {
            query.Where.And<GXAppearance>(a => a.AccessVisibility == ResourceVisibility.Public);
        }
    }
}
