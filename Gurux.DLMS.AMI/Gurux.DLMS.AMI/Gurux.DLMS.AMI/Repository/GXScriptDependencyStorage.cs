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

using System.Data;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Transactional storage and database filtering of compilation dependencies.</summary>
internal static class GXScriptDependencyStorage
{
    internal static List<GXScriptDependency> Read(GXDbConnection db, Guid scriptId)
        => db.Select<GXScriptDependency>(GXSelectArgs.SelectAll<GXScriptDependency>(d => d.ScriptId == scriptId));

    internal static void Replace(GXDbConnection db, IDbTransaction transaction, Guid scriptId, IEnumerable<GXScriptDependency> dependencies)
    {
        db.Delete(transaction, GXDeleteArgs.Delete<GXScriptDependency>(d => d.ScriptId == scriptId));
        foreach (var dependency in dependencies)
        {
            var row = new GXScriptDependency { Id = Guid.NewGuid(), ScriptId = scriptId,
                Name = dependency.Name, AssemblyVersion = dependency.AssemblyVersion };
            db.Insert(transaction, GXInsertArgs.Insert(row));
        }
    }

    internal static void ApplyRebuildFilter(GXSelectArgs scripts, IReadOnlyDictionary<string, string> versions)
    {
        var outdated = OutdatedQuery(versions);
        var all = GXSelectArgs.Select<GXScriptDependency>(d => d.ScriptId);
        scripts.Where.And<GXScript>(s =>
            GXSql.Exists<GXScript, GXScriptDependency>(s => s.Id, d => d.ScriptId, outdated) ||
            !GXSql.Exists<GXScript, GXScriptDependency>(s => s.Id, d => d.ScriptId, all));
    }

    internal static GXSelectArgs OutdatedQuery(IReadOnlyDictionary<string, string> versions)
    {
        var names = versions.Keys.ToArray();
        var query = GXSelectArgs.Select<GXScriptDependency>(d => d.ScriptId, d => !names.Contains(d.Name));
        foreach (var pair in versions)
        {
            var name = pair.Key;
            var version = pair.Value;
            query.Where.Or<GXScriptDependency>(d => d.Name == name && d.AssemblyVersion != version);
        }
        query.Distinct = true;
        return query;
    }
}
