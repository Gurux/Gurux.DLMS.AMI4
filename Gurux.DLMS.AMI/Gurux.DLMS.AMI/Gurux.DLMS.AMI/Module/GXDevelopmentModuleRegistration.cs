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

using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.Service.Orm;
namespace Gurux.DLMS.AMI.Server.Module;

/// <summary>Registers configured development modules independently of activation.</summary>
internal static class GXDevelopmentModuleRegistration
{
    /// <summary>Uses the saved activation choice; new configured development modules start by default.</summary>
    internal static bool ShouldActivate(GXModule? module) => module?.Enabled ?? true;

    internal static async Task EnsureAsync(GXDbConnection connection, GXModule module, string version, CancellationToken token)
    {
        using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);
        var id = module.Id;
        var existing = await connection.SingleOrDefaultAsync<GXModule>(transaction,
            GXSelectArgs.Select<GXModule>(m => m.Id, m => m.Id == id), token);
        if (existing == null)
        {
            var insert = GXInsertArgs.Insert(module);
            insert.Exclude<GXModule>(m => new { m.UserGroups, m.ModuleGroups, m.Logs, m.Versions,
                m.DeviceProperties, m.ObjectProperties, m.AttributeProperties, m.Properties });
            await connection.InsertAsync(transaction, insert, token);
            await AmiModuleSettingsStorage.SavePropertiesAsync(connection, module, token, transaction);
        }
        else
        {
            await connection.UpdateAsync(transaction, GXUpdateArgs.Update(module, m => new
            {
                m.Name,
                m.Icon,
                m.Description,
                m.Protocols,
                m.Path,
                m.FileName,
                m.Version,
                m.Status,
                m.Type
            }), token);
        }
        var parent = new GXModule(id);
        var release = await connection.SingleOrDefaultAsync<GXModuleVersion>(transaction,
            GXSelectArgs.Select<GXModuleVersion>(v => v.Id, v => v.Module == parent && v.Number == version), token);
        if (release == null)
        {
            await connection.InsertAsync(transaction, GXInsertArgs.Insert(new GXModuleVersion
            {
                Module = parent, Number = version, Prerelease = true, Enabled = true,
                CreationTime = DateTimeOffset.UtcNow, FileName = module.FileName, Description = module.Description
            }), token);
        }
        transaction.Commit();
    }
}
