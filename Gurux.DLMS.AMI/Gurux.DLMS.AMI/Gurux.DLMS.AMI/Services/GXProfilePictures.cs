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
using System.Data;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server.Services;

/// <summary>Validates profile image references without embedding image content in resource records.</summary>
public static class GXProfilePictures
{
    /// <summary>Requires an existing, non-removed image accessible under the caller's resource policies.</summary>
    public static async Task ValidateAsync(GXDbConnection connection, IAuthorizationService authorization,
        ClaimsPrincipal user, Guid? imageId, CancellationToken token = default)
    {
        if (imageId == null)
        {
            return;
        }
        if (imageId == Guid.Empty)
        {
            throw new ArgumentException("Invalid profile picture identifier.");
        }
        var query = GXSelectArgs.Select<GXAppearance>(a => new { a.Id, a.AccessVisibility, a.Creator, a.ResourceType, a.Removed }, a => a.Id == imageId);
        query.Joins.AddLeftJoin<GXAppearance, GXUser>(a => a.Creator!, u => u.Id!);
        query.Columns.Add<GXUser>(u => u.Id!);
        var image = await connection.SingleOrDefaultAsync<GXAppearance>(query, token);
        if (image == null || image.Removed != null || image.ResourceType != (byte)ResourceType.Image)
        {
            throw new ArgumentException("Profile picture must reference an existing image.");
        }
        if (!GXAppearanceAccess.CanView(user, image))
            {
            throw new UnauthorizedAccessException("Profile picture is not accessible.");
        }
        if (GXAppearanceAccess.IsOwnerOnly(image))
        {
            GXAppearanceAccess.RequirePrivateOwner(user, image);
            return;
        }
        var policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXAppearance), imageId.Value, token);
        if (!await GXResourcePolicyAccess.AllowsAsync(connection, authorization, user, policies, token))
        {
            throw new UnauthorizedAccessException("Profile picture is not accessible.");
        }
    }

    /// <summary>Prevents removing or retyping an image that is still selected by any profile owner.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "The existing parameter order is retained for source compatibility with modules and callers.")]
    public static async Task EnsureNotReferencedAsync(GXDbConnection connection, Guid imageId, CancellationToken token = default, IDbTransaction? transaction = null)
    {
        var image = new GXAppearance { Id = imageId };
        var users = GXSelectArgs.Select<GXUser>(a => GXSql.Count(1), a => a.ProfilePicture == image);
        var devices = GXSelectArgs.Select<GXDevice>(a => GXSql.Count(1), a => a.ProfilePicture == image);
        var manufacturers = GXSelectArgs.Select<GXManufacturer>(a => GXSql.Count(1), a => a.ProfilePicture == image);
        var models = GXSelectArgs.Select<GXDeviceModel>(a => GXSql.Count(1), a => a.ProfilePicture == image);
        var templates = GXSelectArgs.Select<GXDeviceTemplate>(a => GXSql.Count(1), a => a.ProfilePicture == image);
        if (await connection.SingleOrDefaultAsync<int>(transaction, users, token) != 0 ||
            await connection.SingleOrDefaultAsync<int>(transaction, devices, token) != 0 ||
            await connection.SingleOrDefaultAsync<int>(transaction, manufacturers, token) != 0 ||
            await connection.SingleOrDefaultAsync<int>(transaction, models, token) != 0 ||
            await connection.SingleOrDefaultAsync<int>(transaction, templates, token) != 0)
        {
            throw new InvalidOperationException("The image is used as a profile picture. Clear or replace its references first.");
        }
    }
}
