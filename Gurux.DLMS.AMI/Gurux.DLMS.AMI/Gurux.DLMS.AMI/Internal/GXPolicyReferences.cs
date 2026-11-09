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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Model;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Checks stable policy memberships, including tables owned by inactive modules.</summary>
internal static class GXPolicyReferences
{
    public static async Task EnsureUnusedAsync(GXDbConnection connection, IDbTransaction transaction,
        IEnumerable<string> policyNames, CancellationToken cancellationToken)
    {
        var names = policyNames.Select(n => n.ToUpperInvariant()).Distinct(StringComparer.Ordinal).ToArray();
        if (names.Length == 0)
        {
            return;
        }

        var policies = await connection.SelectAsync<GXPolicy>(transaction,
                    GXSelectArgs.Select<GXPolicy>(p => p.Id, p => names.Contains(p.NormalizedName)), cancellationToken);
        var ids = policies.Select(p => p.Id).ToHashSet();
        var schema = new GXSchemaManager(connection);
        var settings = new GXSqlBuilder(connection.Connection, null).Settings;
        var referenced = new List<string>();
        foreach (var table in schema.GetTables(transaction))
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Requirements are the policy's own children, not resource assignments.
            if (table.EndsWith(nameof(GXPolicyRequirement), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var column = schema.GetColumns(transaction, table).FirstOrDefault(c => c.Equals("PolicyId", StringComparison.OrdinalIgnoreCase));
            if (column == null)
            {
                continue;
            }

            using var command = connection.Connection.CreateCommand();
            command.Transaction = (System.Data.Common.DbTransaction)transaction;
            command.CommandText = "SELECT " + settings.EscapeIdentifier(null, column) + " FROM " + settings.EscapeIdentifier(null, table)
                + " WHERE " + settings.EscapeIdentifier(null, column) + " IS NOT NULL";
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var value = reader.GetValue(0);
                Guid id;
                if (value is Guid guid)
                {
                    id = guid;
                }
                else if (value is byte[] bytes && bytes.Length == 16)
                {
                    id = new Guid(bytes);
                }
                else if (!Guid.TryParse(Convert.ToString(value), out id))
                {
                    throw new InvalidOperationException($"Cannot verify policy memberships in '{table}': invalid PolicyId.");
                }

                if (ids.Contains(id))
                {
                    referenced.Add(table);
                    break;
                }
            }
        }
        if (referenced.Count != 0)
        {
            throw new InvalidOperationException("Policy is used by resources in: " + string.Join(", ", referenced) + ". Remove or replace those memberships before deleting it.");
        }
    }
}