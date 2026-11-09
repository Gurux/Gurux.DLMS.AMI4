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
using System.Data;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm;
namespace Gurux.DLMS.AMI.Server;

internal static partial class ServerSettings
{
    /// <summary>Returns an existing nonremoved user with the admin role.</summary>
    public static async Task<GXUser?> GetDefaultAdmin(GXDbConnection connection,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        GXSelectArgs arg = GXSelectArgs.Select<GXUser>(s => s.Id!, where => where.Removed == null);
        arg.Joins.AddInnerJoin<GXUser, GXUserRole>(j => j.Id!, j => j.UserId!);
        arg.Joins.AddInnerJoin<GXUserRole, GXRole>(j => j.RoleId!, j => j.Id);
        arg.Where.And<GXRole>(where => where.Name == GXRoles.Admin);
        var admins = await connection.SelectAsync<GXUser>(transaction, arg, cancellationToken);
        if (admins.Count != 0)
        {
            var admin = admins.First();
            return admin;
        }
        return null;
    }

}
