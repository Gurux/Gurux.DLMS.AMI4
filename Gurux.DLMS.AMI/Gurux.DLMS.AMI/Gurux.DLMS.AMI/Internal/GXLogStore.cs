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
using System.Data.Common;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Common.Enums;
using Gurux.Service.Orm.Enums;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Writes shared logs and applies category and ownership restrictions.</summary>
internal static class GXLogStore
{
    internal sealed record Descriptor(string Source, string? Parent, string ViewPolicy);
    internal static readonly Descriptor[] Descriptors = [
        new("system", null, GXSystemLogPolicies.View),
        new("key-management", "KeyManagement", GXKeyManagementLogPolicies.View),
        new("agent", "Agent", GXAgentLogPolicies.View),
        new("gateway", "Gateway", GXGatewayLogPolicies.View),
        new("module", "Module", GXModuleLogPolicies.View),
        new("schedule", "Schedule", GXScheduleLogPolicies.View),
        new("workflow", "Workflow", GXWorkflowLogPolicies.View),
        new("script", "Script", GXScriptLogPolicies.View),
        new("user-action", "User", GXUserActionPolicies.View),
        new("user-error", "User", GXUserErrorPolicies.View),
        new("device-error", "Device", GXDeviceLogPolicies.View),
        new("device-action", "Device", GXDeviceActionPolicies.View)
    ];
    internal static Descriptor GetDescriptor(string source) => Descriptors.FirstOrDefault(d => d.Source == source)
        ?? throw new ArgumentException("Unknown application log type.", nameof(source));

    public static GXSelectArgs BuiltinTypes(string source)
    {
        string name = "BUILTIN:" + source.ToUpperInvariant();
        return GXSelectArgs.Select<GXLogType>(t => GXSql.One, t => t.NormalizedName == name);
    }

    public static System.Linq.Expressions.Expression<Func<GXLog, bool>> SourcePredicate(string source,
        System.Linq.Expressions.Expression<Func<GXLog, bool>>? predicate = null)
    {
        var types = BuiltinTypes(source);
        System.Linq.Expressions.Expression<Func<GXLog, bool>> category = l => GXSql.Exists<GXLog, GXLogType>(l => l.LogType!, t => t.Id, types);
        if (predicate == null)
        {
            return category;
        }

        var body = new ParameterVisitor(predicate.Parameters[0], category.Parameters[0]).Visit(predicate.Body)!;
        return System.Linq.Expressions.Expression.Lambda<Func<GXLog, bool>>(
            System.Linq.Expressions.Expression.AndAlso(category.Body, body), category.Parameters);
    }
    public static async Task EnsureBuiltinTypesAsync(GXDbConnection connection, CancellationToken token = default)
    {
        await using var transaction = await connection.BeginTransactionAsync(token);
        foreach (var descriptor in Descriptors)
        {
            await EnsureTypeAsync(connection, descriptor, transaction, token);
        }
        await transaction.CommitAsync(token);
    }

    public static async Task RestrictTypesAsync(GXDbConnection connection, GXSelectArgs args,
        ClaimsPrincipal user, IAuthorizationService authorization, CancellationToken token = default)
    {
        var allowed = new List<long>();
        foreach (var type in await connection.SelectAsync<GXLogType>(GXSelectArgs.Select<GXLogType>(t => t.Id, t => t.Removed == null), token))
        {
            var names = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXLogType), type.Id, token);
            if (await GXResourcePolicyAccess.AllowsAsync(connection, authorization, user, names, token))
            {
                allowed.Add(type.Id);
            }
        }
        if (allowed.Count == 0)
        {
            args.Where.And<GXLog>(l => l.LogType == null);
        }
        else
        {
            var types = GXSelectArgs.Select<GXLogType>(t => GXSql.One, t => allowed.Contains(t.Id));
            args.Where.And<GXLog>(l => l.LogType == null || GXSql.Exists<GXLog, GXLogType>(l => l.LogType!, t => t.Id, types));
        }
    }

    [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "Preserves the established argument order used by repository and transaction callers.")]
    public static async Task RestrictSourcesAsync(GXSelectArgs args, ClaimsPrincipal user,
        IAuthorizationService authorization, CancellationToken token = default, string operation = "view",
        string? selectedSource = null, bool allUsers = true)
    {
        var builtinTypes = GXSelectArgs.Select<GXLogType>(t => GXSql.One,
            t => t.NormalizedName != null && t.NormalizedName.StartsWith("BUILTIN:"));
        System.Linq.Expressions.Expression<Func<GXLog, bool>> permitted = l => l.LogType == null
            || !GXSql.Exists<GXLog, GXLogType>(l => l.LogType!, t => t.Id, builtinTypes);
        var administrator = allUsers && user.IsInRole(Gurux.DLMS.AMI.Client.Shared.GXRoles.Admin);
        foreach (var descriptor in Descriptors.Where(d => selectedSource == null || d.Source == selectedSource))
        {
            token.ThrowIfCancellationRequested();
            var policy = descriptor.ViewPolicy[..descriptor.ViewPolicy.LastIndexOf('.')] + "." + operation;
            if (!(await authorization.AuthorizeAsync(user, null, policy)).Succeeded)
            {
                continue;
            }

            var source = descriptor.Source;
            var sourceTypes = BuiltinTypes(source);
            System.Linq.Expressions.Expression<Func<GXLog, bool>> predicate;
            if (administrator || descriptor.Parent == null)
            {
                predicate = l => GXSql.Exists<GXLog, GXLogType>(l => l.LogType!, t => t.Id, sourceTypes);
            }
            else if (source == "agent")
            {
                var parents = GXQuery.GetAgentsByUser(p => GXSql.One, user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value ?? string.Empty);
                predicate = l => GXSql.Exists<GXLog, GXLogType>(l => l.LogType!, t => t.Id, sourceTypes) && GXSql.Exists<GXLog, Gurux.DLMS.AMI.Shared.DTOs.Agent.GXAgent>(l => l.Creator!, p => p.Id, parents);
            }
            else if (source == "gateway")
            {
                var parents = GXQuery.GetGatewaysByUser(p => GXSql.One, user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value ?? string.Empty);
                predicate = l => GXSql.Exists<GXLog, GXLogType>(l => l.LogType!, t => t.Id, sourceTypes) && GXSql.Exists<GXLog, Gurux.DLMS.AMI.Shared.DTOs.Gateway.GXGateway>(l => l.Creator!, p => p.Id, parents);
            }
            else if (source == "key-management")
            {
                var parents = GXQuery.GetKeyManagementsByUser(p => GXSql.One, user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value ?? string.Empty);
                predicate = l => GXSql.Exists<GXLog, GXLogType>(l => l.LogType!, t => t.Id, sourceTypes) && GXSql.Exists<GXLog, Gurux.DLMS.AMI.Shared.DTOs.KeyManagement.GXKeyManagement>(l => l.Creator!, p => p.Id, parents);
            }
            else if (source == "module")
            {
                var parents = GXQuery.GetModulesByUser(p => GXSql.One, user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value ?? string.Empty);
                predicate = l => GXSql.Exists<GXLog, GXLogType>(l => l.LogType!, t => t.Id, sourceTypes) && GXSql.Exists<GXLog, Gurux.DLMS.AMI.Shared.DTOs.Module.GXModule>(l => l.SourceId!, p => p.Id, parents);
            }
            else if (source == "schedule")
            {
                var parents = GXQuery.GetSchedulesByUser(p => GXSql.One, user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value ?? string.Empty);
                predicate = l => GXSql.Exists<GXLog, GXLogType>(l => l.LogType!, t => t.Id, sourceTypes) && GXSql.Exists<GXLog, Gurux.DLMS.AMI.Shared.DTOs.Schedule.GXSchedule>(l => l.Creator!, p => p.Id, parents);
            }
            else if (source == "workflow")
            {
                var parents = GXQuery.GetWorkflowsByUser(p => GXSql.One, user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value ?? string.Empty);
                predicate = l => GXSql.Exists<GXLog, GXLogType>(l => l.LogType!, t => t.Id, sourceTypes) && GXSql.Exists<GXLog, Gurux.DLMS.AMI.Shared.DTOs.Workflow.GXWorkflow>(l => l.Creator!, p => p.Id, parents);
            }
            else if (source == "script")
            {
                var parents = GXQuery.GetScriptsByUser(p => GXSql.One, user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value ?? string.Empty);
                predicate = l => GXSql.Exists<GXLog, GXLogType>(l => l.LogType!, t => t.Id, sourceTypes) && GXSql.Exists<GXLog, Gurux.DLMS.AMI.Shared.DTOs.Script.GXScript>(l => l.Creator!, p => p.Id, parents);
            }
            else if (source == "user-action" || source == "user-error")
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value ?? string.Empty;
                var owners = GXSelectArgs.Select<GXUser>(u => GXSql.One, u => u.Id == userId && u.Removed == null);
                predicate = l => GXSql.Exists<GXLog, GXLogType>(l => l.LogType!, t => t.Id, sourceTypes)
                    && GXSql.Exists<GXLog, GXUser>(l => l.User!, u => u.Id!, owners);
            }
            else if (source == "device-error" || source == "device-action")
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value ?? string.Empty;
                var devices = GXQuery.GetDevicesByUser(d => GXSql.One, userId);
                predicate = l => GXSql.Exists<GXLog, GXLogType>(l => l.LogType!, t => t.Id, sourceTypes)
                    && GXSql.Exists<GXLog, Gurux.DLMS.AMI.Shared.DTOs.Device.GXDevice>(l => l.Creator!, d => d.Id, devices);
            }
            else
            {
                continue;
            }

            var body = new ParameterVisitor(predicate.Parameters[0], permitted.Parameters[0]).Visit(predicate.Body)!;
            permitted = System.Linq.Expressions.Expression.Lambda<Func<GXLog, bool>>(
                System.Linq.Expressions.Expression.OrElse(permitted.Body, body), permitted.Parameters);
        }
        args.Where.And(permitted);
    }
    private sealed class ParameterVisitor(System.Linq.Expressions.ParameterExpression oldParameter,
        System.Linq.Expressions.ParameterExpression newParameter) : System.Linq.Expressions.ExpressionVisitor
    {
        protected override System.Linq.Expressions.Expression VisitParameter(System.Linq.Expressions.ParameterExpression node)
            => node == oldParameter ? newParameter : base.VisitParameter(node);
    }


    public static async Task DeleteOwnedAsync(GXDbConnection connection, IDbTransaction transaction, string source,
        IEnumerable<Guid> identifiers, CancellationToken cancellationToken = default)
    {
        var ids = identifiers.Distinct().Select(id => (Guid?)id).ToArray();
        if (ids.Length == 0)
        {
            return;
        }

        System.Linq.Expressions.Expression<Func<GXLog, bool>> owner = log => ids.Contains(log.Creator);
        if (source == "device")
        {
            await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXLog>(SourcePredicate("device-error", owner)), cancellationToken);
            await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXLog>(SourcePredicate("device-action", owner)), cancellationToken);
        }
        else
        {
            GetDescriptor(source);
            await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXLog>(SourcePredicate(source, owner)), cancellationToken);
        }
    }

    public static async Task DeleteOwnedAsync(GXDbConnection connection, IDbTransaction transaction, string source,
        IEnumerable<string> identifiers, CancellationToken cancellationToken = default)
    {
        var ids = identifiers.Distinct(StringComparer.Ordinal).ToArray();
        if (ids.Length == 0)
        {
            return;
        }

        if (source == "user")
        {
            var users = GXSelectArgs.Select<GXUser>(u => GXSql.One, u => ids.Contains(u.Id));
            await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXLog>(log => GXSql.Exists<GXLog, GXUser>(l => l.User!, u => u.Id!, users)), cancellationToken);
        }
        else if (source == "module")
        {
            await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXLog>(SourcePredicate(source, log => ids.Contains(log.SourceId!))), cancellationToken);
        }
        else
        {
            throw new ArgumentException("The category does not have a string owner identifier.", nameof(source));
        }
    }

    [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "Preserves the established argument order used by repository and transaction callers.")]
    public static Task InsertAsync(GXDbConnection connection, string source, IEnumerable<GXLog> values,
        CancellationToken cancellationToken = default, IDbTransaction? transaction = null)
        => InsertCoreAsync(connection, values, GetDescriptor(source), cancellationToken, transaction);

    [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "Preserves the established argument order used by repository and transaction callers.")]
    public static Task InsertAsync(GXDbConnection connection, IEnumerable<GXLog> values,
        CancellationToken cancellationToken = default, IDbTransaction? transaction = null)
        => InsertCoreAsync(connection, values, null, cancellationToken, transaction);

    [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:CancellationToken parameters must come last", Justification = "Preserves the established argument order used by repository and transaction callers.")]
    private static async Task InsertCoreAsync(GXDbConnection connection, IEnumerable<GXLog> values,
        Descriptor? descriptor, CancellationToken cancellationToken, IDbTransaction? transaction)
    {
        var rows = values.ToArray();
        if (rows.Length == 0)
        {
            return;
        }

        for (int attempt = 0; ; ++attempt)
        {
            try
            {
                await InsertAttemptAsync(connection, rows, descriptor, cancellationToken, transaction);
                return;
            }
            catch (GXDatabaseException ex) when (transaction == null && attempt < 2 &&
                ex.ErrorType is DatabaseErrorType.UniqueConstraint or DatabaseErrorType.Deadlock)
            {
                // Retry the whole owned transaction so no partial type, policy or log write survives.
                await Task.Delay(TimeSpan.FromMilliseconds(25 * (attempt + 1)), cancellationToken);
            }
        }
    }

    private static async Task InsertAttemptAsync(GXDbConnection connection, GXLog[] rows,
        Descriptor? descriptor, CancellationToken cancellationToken, IDbTransaction? transaction)
    {
        await using var owned = transaction == null ? await connection.BeginTransactionAsync(cancellationToken) : null;
        var current = transaction ?? owned!;
        try
        {
            await LockLogUsersAsync(connection, current, rows, descriptor != null, cancellationToken);
            if (descriptor != null)
            {
                var type = await EnsureTypeAsync(connection, descriptor, current, cancellationToken);
                foreach (var row in rows)
                {
                    row.LogType = type;
                }
            }
            foreach (var row in rows)
            {
                if (row.PublicId == Guid.Empty)
                {
                    row.PublicId = Guid.NewGuid();
                }

                row.Id = 0;
                row.CreationTime ??= DateTimeOffset.UtcNow;
                row.Level ??= 1;
                row.Type ??= 0;
                row.Message ??= string.Empty;
            }
            await connection.InsertAsync(current, GXInsertArgs.InsertRange(rows), cancellationToken);
            if (owned != null)
            {
                await owned.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            if (owned?.Connection != null)
            {
                await owned.RollbackAsync(CancellationToken.None);
            }
            throw;
        }
    }

    private static async Task LockLogUsersAsync(GXDbConnection connection, IDbTransaction transaction,
        GXLog[] rows, bool builtin, CancellationToken cancellationToken)
    {
        if (connection.Connection is not (Microsoft.Data.SqlClient.SqlConnection or Oracle.ManagedDataAccess.Client.OracleConnection))
        {
            return;
        }
        var ids = rows.Select(row => row.User?.Id).OfType<string>().Distinct(StringComparer.Ordinal).ToArray();
        if (ids.Length == 0)
        {
            return;
        }
        // Take the same parent-key locks as user deletion, in indexed order,
        // before touching child log rows. Otherwise each blocked insert owns a
        // child key that the parent DELETE must scan, causing repeated deadlocks.
        using var command = connection.Connection.CreateCommand();
        command.Transaction = (DbTransaction)transaction;
        bool oracle = command is Oracle.ManagedDataAccess.Client.OracleCommand;
        if (command is Oracle.ManagedDataAccess.Client.OracleCommand oracleCommand) {
            oracleCommand.BindByName = true;
        }
        var names = new List<string>();
        for (int index = 0; index != ids.Length; ++index)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = (oracle ? "user" : "@user") + index;
            parameter.Value = ids[index];
            command.Parameters.Add(parameter);
            names.Add((oracle ? ":" : "") + parameter.ParameterName);
        }
        command.CommandText = oracle
            // Oracle FK inserts do not honor a parent's FOR UPDATE lock, so
            // participate in the deletion protocol explicitly, per user row.
            ? $"SELECT ID FROM GXUSER WHERE ID IN ({string.Join(",", names)}) ORDER BY ID FOR UPDATE"
            : $"SELECT ID FROM GXUser WITH (HOLDLOCK, ROWLOCK, INDEX(gxuser_id)) WHERE ID IN ({string.Join(",", names)}) ORDER BY ID";
        var existing = new HashSet<string>(StringComparer.Ordinal);
        using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken)) {
                existing.Add(reader.GetString(0));
            }
        }
        if (builtin)
        {
            // A request can finish after its user has been deleted. Keep its
            // diagnostic event with the already-supported optional user unset.
            foreach (var row in rows)
            {
                if (row.User?.Id is string id && !existing.Contains(id)) {
                    row.User = null;
                }
            }
        }
    }
    internal static async Task<GXLogType> EnsureTypeAsync(GXDbConnection connection, Descriptor descriptor,
        IDbTransaction transaction, CancellationToken cancellationToken = default)
    {
        var name = "builtin:" + descriptor.Source;
        var normalized = name.ToUpperInvariant();
        var type = await connection.SingleOrDefaultAsync<GXLogType>(transaction,
            GXSelectArgs.SelectAll<GXLogType>(t => t.NormalizedName == normalized), cancellationToken);
        if (type != null)
        {
            return type;
        }

        if (connection.DatabaseType == DatabaseType.MSSQL)
        {
            // Protect the missing unique key until commit/rollback, including across server processes.
            // Existing types take the fast path above, without serializing normal log writes.
            await using var command = ((DbConnection)transaction.Connection!).CreateCommand();
            command.Transaction = (DbTransaction)transaction;
            command.CommandTimeout = connection.CommandTimeout > 0 ? connection.CommandTimeout : 30;
            command.CommandText = "SELECT [Id] FROM [GXLogType] WITH (UPDLOCK, HOLDLOCK) WHERE [NormalizedName] = @name";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@name";
            parameter.DbType = DbType.String;
            parameter.Size = 64;
            parameter.Value = normalized;
            command.Parameters.Add(parameter);
            await command.ExecuteScalarAsync(cancellationToken);
            type = await connection.SingleOrDefaultAsync<GXLogType>(transaction,
                GXSelectArgs.SelectAll<GXLogType>(t => t.NormalizedName == normalized), cancellationToken);
            if (type != null)
            {
                return type;
            }
        }

        type = new GXLogType(name) { PublicId = Guid.NewGuid(), NormalizedName = normalized, Enabled = true, Level = 0, CreationTime = DateTimeOffset.UtcNow };
        var users = GXSelectArgs.Select<GXUser>(u => u.Id!);
        users.OrderBy.Add<GXUser>(u => u.Id!);
        users.Count = 1;
        type.Creator = await connection.SingleOrDefaultAsync<GXUser>(transaction, users, cancellationToken);
        var insert = GXInsertArgs.Insert(type);
        insert.Exclude<GXLogType>(t => t.Logs!);
        await connection.InsertAsync(transaction, insert, cancellationToken);
        var policyName = descriptor.ViewPolicy.ToUpperInvariant();
        var policy = await connection.SingleOrDefaultAsync<GXPolicy>(transaction,
            GXSelectArgs.Select<GXPolicy>(p => p.Id, p => p.NormalizedName == policyName && p.Removed == null), cancellationToken);
        if (policy != null)
        {
            await connection.InsertAsync(transaction, GXInsertArgs.Insert(new GXLogTypePolicy { LogTypeId = type.Id, PolicyId = policy.Id }), cancellationToken);
        }

        return type;
    }
}



