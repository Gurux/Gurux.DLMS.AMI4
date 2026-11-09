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

using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.Enums;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Gateway;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Schedule;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Provides authorized access to custom and application logs.</summary>
public sealed partial class LogRepository : ILogRepository
{
    private readonly ClaimsPrincipal _user;
    private readonly IDbConnectionFactory _dbFactory;
    private readonly IEnumTypeRepository _enumTypes;
    private readonly IAuthorizationService _authorization;
    private readonly NotificationCache _notifications;
    private readonly IServiceProvider _services;
    private readonly string? _source;

    /// <summary>Creates a repository for authorized log access and notifications.</summary>
    public LogRepository(IAuthorizationService authorization, IDbConnectionFactory dbFactory,
        IHttpContextAccessor context, IEnumTypeRepository enumTypeRepository,
        NotificationCache notificationCache, IServiceProvider services)
    {
        _user = context.HttpContext?.User ?? throw new UnauthorizedAccessException();
        _authorization = authorization;
        _dbFactory = dbFactory;
        _enumTypes = enumTypeRepository;
        _notifications = notificationCache;
        _services = services;
    }

    private LogRepository(LogRepository other, string? source)
    {
        _user = other._user;
        _authorization = other._authorization;
        _dbFactory = other._dbFactory;
        _enumTypes = other._enumTypes;
        _notifications = other._notifications;
        _services = other._services;
        _source = source;
    }

    /// <inheritdoc />
    public ILogRepository ForSource(string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        return new LogRepository(this, source.Trim());
    }

    private static string? BuiltinSource(GXLogType type)
        => type.NormalizedName?.StartsWith("BUILTIN:", StringComparison.Ordinal) == true
            ? type.NormalizedName[8..].ToLowerInvariant() : null;

    private static string Policy(string operation, GXLogType type)
    {
        var source = BuiltinSource(type);
        var view = source == null ? GXLogPolicies.View : GXLogStore.GetDescriptor(source).ViewPolicy;
        return view[..view.LastIndexOf('.')] + "." + (source != null && operation == "delete" ? "clear" : operation);
    }

    private static string EnumTarget(string? source) => source switch
    {
        "system" => TargetType.SystemLog,
        "agent" => TargetType.AgentLog,
        "gateway" => TargetType.GatewayLog,
        "module" => TargetType.ModuleLog,
        "schedule" => TargetType.ScheduleLog,
        "workflow" => TargetType.WorkflowLog,
        "script" => TargetType.ScriptLog,
        "key-management" => TargetType.KeyManagementLog,
        "device-error" => TargetType.DeviceLog,
        "device-action" => TargetType.DeviceAction,
        "user-error" => TargetType.UserError,
        "user-action" => TargetType.UserAction,
        _ => TargetType.Log
    };

    private async Task<GXLogType> ResolveTypeAsync(GXDbConnection connection, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(_source);
        var normalized = _source.ToUpperInvariant();
        if (GXLogStore.Descriptors.Any(d => string.Equals(d.Source, _source, StringComparison.OrdinalIgnoreCase)))
        {
            normalized = "BUILTIN:" + normalized;
        }
        var query = Guid.TryParse(_source, out var id)
            ? GXSelectArgs.SelectAll<GXLogType>(t => t.PublicId == id && t.Removed == null)
            : GXSelectArgs.SelectAll<GXLogType>(t => t.NormalizedName == normalized && t.Removed == null);
        return await connection.SingleOrDefaultAsync<GXLogType>(query, token)
            ?? throw new ArgumentException("Unknown or removed log source.");
    }

    private async Task<(GXSelectArgs Query, GXLogType Type)> QueryAsync(GXDbConnection connection, string operation, CancellationToken token)
    {
        var type = await ResolveTypeAsync(connection, token);
        await _authorization.EnsureAnyPolicyAsync(_user, Policy(operation, type));
        var typeId = type.Id;
        var types = GXSelectArgs.Select<GXLogType>(t => GXSql.One, t => t.Id == typeId);
        return (GXSelectArgs.SelectAll<GXLog>(l => GXSql.Exists<GXLog, GXLogType>(l => l.LogType!, t => t.Id, types)), type);
    }

    private async Task RestrictAsync(GXDbConnection connection, GXSelectArgs query, GXLogType type, string operation, CancellationToken token, bool allUsers = true)
    {
        await GXLogStore.RestrictTypesAsync(connection, query, _user, _authorization, token);
        var source = BuiltinSource(type);
        if (source != null)
        {
            await GXLogStore.RestrictSourcesAsync(query, _user, _authorization, token, operation == "delete" ? "clear" : operation, source, allUsers);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GXLog>> ListAsync(ListLogs? request = null,
        ListLogsResponse? response = null, CancellationToken cancellationToken = default)
    {
        if (request?.Index < 0 || request?.Count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request));
        }
        await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
        GXLogType? type = null;
        GXSelectArgs query;
        if (string.IsNullOrWhiteSpace(_source))
        {
            query = GXSelectArgs.SelectAll<GXLog>();
            query.Joins.AddLeftJoin<GXLog, GXLogType>(l => l.LogType!, t => t.Id);
            query.Columns.Add<GXLogType>(t => new { t.Id, t.PublicId, t.Name, t.NormalizedName });
            // Category policies and built-in ownership rules also apply to the combined view.
            await GXLogStore.RestrictTypesAsync(connection, query, _user, _authorization, cancellationToken);
            await GXLogStore.RestrictSourcesAsync(query, _user, _authorization, cancellationToken, allUsers: request?.AllUsers == true);
            if (!(await _authorization.AuthorizeAsync(_user, null, GXLogPolicies.View)).Succeeded)
            {
                var builtins = GXSelectArgs.Select<GXLogType>(t => GXSql.One,
                    t => t.NormalizedName != null && t.NormalizedName.StartsWith("BUILTIN:"));
                query.Where.And<GXLog>(l => GXSql.Exists<GXLog, GXLogType>(l => l.LogType!, t => t.Id, builtins));
            }
        }
        else
        {
            (query, type) = await QueryAsync(connection, "view", cancellationToken);
            await RestrictAsync(connection, query, type, "view", cancellationToken, request?.AllUsers == true);
        }
        query.Joins.AddLeftJoin<GXLog, GXUser>(l => l.User!, u => u.Id!);
        query.Columns.Add<GXUser>(u => new { u.Id, u.UserName });
        if (request?.Included?.Any() == true)
        {
            query.Where.And<GXLog>(l => request.Included.Contains(l.PublicId));
        }
        if (request?.Exclude != null)
        {
            foreach (var excluded in request.Exclude.Distinct())
            {
                var identifier = excluded;
                query.Where.And<GXLog>(l => l.PublicId != identifier);
            }
        }
        GXDateTimeFilters.Apply<GXLog>(query, request?.DateFilters);
        // Apply text filters last so earlier identifiers keep their table qualification.
        if (request?.Filter != null)
        {
            query.Where.FilterBy(request.Filter);
        }
        if (response != null)
        {
            var total = GXSelectArgs.Select<GXLog>(l => GXSql.Count(1));
            total.Joins.Append(query.Joins);
            total.Where.Append(query.Where);
            response.Count = await connection.SingleOrDefaultAsync<long>(total, cancellationToken);
        }
        query.Index = (request?.Index ?? 0);
        query.Count = (request?.Count ?? 0);
        query.Descending = string.IsNullOrEmpty(request?.OrderBy) || request.Descending;
        if (string.IsNullOrEmpty(request?.OrderBy))
        {
            query.OrderBy.Add<GXLog>(l => l.CreationTime!);
            query.OrderBy.Add<GXLog>(l => l.Id);
        }
        else
        {
            query.OrderBy.Add<GXLog>(request.OrderBy);
        }
        var rows = await connection.SelectAsync<GXLog>(query, cancellationToken);
        if (type != null)
        {
            type.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXLogType), type.Id, cancellationToken);
            foreach (var row in rows)
            {
                row.LogType = type;
            }
        }
        if (response != null)
        {
            response.Logs = rows;
        }
        return rows;
    }

    /// <inheritdoc />
    public async Task<GXLog> ReadAsync(Guid id, Expression<Func<GXLog, object>>? columns = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
        var (query, type) = await QueryAsync(connection, "view", cancellationToken);
        query.Where.And<GXLog>(l => l.PublicId == id);
        query.Joins.AddLeftJoin<GXLog, GXUser>(l => l.User!, u => u.Id!);
        query.Columns.Add<GXUser>(u => new { u.Id, u.UserName });
        await RestrictAsync(connection, query, type, "view", cancellationToken);
        var row = await connection.SingleOrDefaultAsync<GXLog>(query, cancellationToken)
            ?? throw new GXAmiForbiddenException(id.ToString());
        row.LogType = type;
        type.Policies = await GXResourcePolicyStorage.ReadAsync(connection, typeof(GXLogType), type.Id, cancellationToken);
        return row;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Guid>> AddAsync(string type, IEnumerable<GXLog> logs,
        CancellationToken cancellationToken = default)
    {
        var rows = logs.ToArray();
        await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
        var selected = await ResolveTypeAsync(connection, cancellationToken);
        await _authorization.EnsureAnyPolicyAsync(_user, Policy("add", selected));
        var source = BuiltinSource(selected);
        foreach (var row in rows)
        {
            row.Type = await _enumTypes.GetLogTypeAsync(EnumTarget(source), type, cancellationToken);
            row.CreationTime = DateTimeOffset.UtcNow;
            if (source is "user-error" or "user-action")
            {
                row.User ??= new GXUser
                {
                    Id = ServerHelpers.GetUserId(_user)
                };
            }
            row.LogType = selected;
        }
        await GXLogStore.InsertAsync(connection, rows, cancellationToken);
        foreach (var row in rows)
        {
            await NotifyAsync(row, NotificationAction.Add, "add", cancellationToken);
        }

        return [.. rows.Select(l => l.PublicId)];
    }

    /// <inheritdoc />
    public async Task ClearAsync(ClearLog? request = null, CancellationToken cancellationToken = default)
    {
        await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
        var (query, type) = await QueryAsync(connection, "clear", cancellationToken);
        var source = BuiltinSource(type);
        if (request?.Modules != null && source != "module" || request?.Users != null && source is not ("user-error" or "user-action"))
        {
            throw new ArgumentException("The clear scope does not match the selected log source.", nameof(request));
        }
        var identifiers = request?.Ids ?? request?.Groups;
        if (identifiers != null)
        {
            var owners = identifiers.Select(id => (Guid?)id).ToArray();
            if (owners.Length == 0)
            {
                return;
            }

            query.Where.And<GXLog>(l => owners.Contains(l.Creator));
        }
        var names = source == "module" ? request?.Modules : request?.Users;
        if (names != null)
        {
            var owners = names.ToArray();
            if (owners.Length == 0)
            {
                return;
            }

            if (source == "module")
            {
                query.Where.And<GXLog>(l => owners.Contains(l.SourceId!));
            }
            else if (source is "user-error" or "user-action")
            {
                var users = GXSelectArgs.Select<GXUser>(u => GXSql.One, u => owners.Contains(u.Id!));
                query.Where.And<GXLog>(l => GXSql.Exists<GXLog, GXUser>(l => l.User!, u => u.Id!, users));
            }
            else
            {
                throw new ArgumentException("String source identifiers are unsupported for this log source.", nameof(request));
            }
        }
        await RestrictAsync(connection, query, type, "clear", cancellationToken);
        // Clear is source-scoped: do not materialize or transmit deleted item identifiers.
        // Only owner-scoped built-in logs need a distinct list of notification owners.
        query.Columns.Clear();
        query.Distinct = true;
        var ownerScoped = true;
        switch (source)
        {
            case "agent":
            case "gateway":
            case "device-error":
            case "device-action":
            case "key-management":
            case "schedule":
            case "script":
            case "workflow":
                query.Columns.Add<GXLog>(l => l.Creator!);
                break;
            case "module":
                query.Columns.Add<GXLog>(l => l.SourceId!);
                break;
            case "user-action":
            case "user-error":
                query.Columns.Add<GXLog>(l => l.User!);
                break;
            default:
                ownerScoped = false;
                break;
        }
        IEnumerable<GXLog> notificationScopes = ownerScoped
            ? await connection.SelectAsync<GXLog>(query, cancellationToken)
            : [new GXLog()];
        var deletion = GXDeleteArgs.DeleteAll<GXLog>();
        deletion.Where.Append(query.Where);
        if (await connection.DeleteAsync(deletion, cancellationToken) == 0)
        {
            return;
        }
        var recipients = new HashSet<string>();
        foreach (var row in notificationScopes)
        {
            row.LogType = type;
            recipients.UnionWith(await GetRecipientsAsync(row, NotificationAction.Clear, cancellationToken));
        }
        _notifications.Add(recipients, new GXTarget(NotificationType(type, "clear"))
        {
            Source = type.PublicId.ToString()
        });
    }

    /// <inheritdoc />
    public async Task CloseAsync(IEnumerable<Guid>? logs, CancellationToken cancellationToken = default)
    {
        var ids = logs?.Distinct().ToArray() ?? [];
        await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
        var (query, type) = await QueryAsync(connection, "close", cancellationToken);
        if (ids.Length == 0)
        {
            return;
        }
        query.Where.And<GXLog>(l => ids.Contains(l.PublicId));
        query.Where.And<GXLog>(l => l.Closable && l.Closed == null);
        await RestrictAsync(connection, query, type, "close", cancellationToken);
        var rows = await connection.SelectAsync<GXLog>(query, cancellationToken);
        if (rows.Count == 0)
        {
            return;
        }
        foreach (var row in rows)
        {
            row.Closed = DateTimeOffset.UtcNow;
        }

        await connection.UpdateAsync(GXUpdateArgs.UpdateRange(rows, l => l.Closed!), cancellationToken);
        foreach (var row in rows)
        { row.LogType = type; await NotifyAsync(row, NotificationAction.Close, "close", cancellationToken); }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(IEnumerable<Guid>? logs, bool delete = true, bool notify = true,
        CancellationToken cancellationToken = default)
    {
        if (!delete)
        { await CloseAsync(logs, cancellationToken); return; }
        var ids = logs?.Distinct().ToArray() ?? [];
        await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
        var (query, type) = await QueryAsync(connection, "delete", cancellationToken);
        if (ids.Length == 0)
        {
            return;
        }
        query.Where.And<GXLog>(l => ids.Contains(l.PublicId));
        await RestrictAsync(connection, query, type, "delete", cancellationToken);
        var rows = await connection.SelectAsync<GXLog>(query, cancellationToken);
        if (rows.Count == 0)
        {
            return;
        }
        var selected = rows.Select(row => row.PublicId).ToArray();
        await connection.DeleteAsync(GXDeleteArgs.Delete<GXLog>(l => selected.Contains(l.PublicId)), cancellationToken);
        if (notify)
        {
            foreach (var row in rows)
            {
                row.LogType = type;
                await NotifyAsync(row, NotificationAction.Remove, "delete", cancellationToken);
            }
        }
    }

    private async Task NotifyAsync(GXLog row, NotificationAction action, string operation, CancellationToken token)
    {
        var recipients = await GetRecipientsAsync(row, action, token);
        _notifications.Add(recipients, new GXTarget(NotificationType(row.LogType, operation), row.PublicId, row.Message)
        {
            Level = row.Level,
            Source = row.LogType?.PublicId.ToString()
        });
    }

    private static string NotificationType(GXLogType? type, string operation)
    {
        var source = type == null ? null : BuiltinSource(type);
        var view = source == null ? GXLogPolicies.View : GXLogStore.GetDescriptor(source).ViewPolicy;
        return view[..view.LastIndexOf('.')] + "." + operation;
    }

    private async Task<IEnumerable<string>> GetRecipientsAsync(GXLog row, NotificationAction action, CancellationToken token)
    {
        var source = row.LogType == null ? null : BuiltinSource(row.LogType);
        var view = source == null ? GXLogPolicies.View : GXLogStore.GetDescriptor(source).ViewPolicy;
        await using var connection = await _dbFactory.GetHistoryConnectionAsync(token);
        var subscribed = await connection.SelectAsync<GXUser>(GXQuery.GetUsersByPermission(action, view), token);
        if (subscribed.Count == 0)
        {
            return [];
        }
        IEnumerable<string> recipients = subscribed.Select(u => u.Id!);
        List<string>? owners = source switch
        {
            "agent" when row.Creator.HasValue => await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXAgent), row.Creator.Value, token: token),
            "gateway" when row.Creator.HasValue => await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXGateway), row.Creator.Value, token: token),
            "device-error" or "device-action" when row.Creator.HasValue => await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXDevice), row.Creator.Value, token: token),
            "key-management" when row.Creator.HasValue => await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXKeyManagement), row.Creator.Value, token: token),
            "schedule" when row.Creator.HasValue => await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXSchedule), row.Creator.Value, token: token),
            "script" when row.Creator.HasValue => await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXScript), row.Creator.Value, token: token),
            "workflow" when row.Creator.HasValue => await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXWorkflow), row.Creator.Value, token: token),
            "module" when row.SourceId != null => await GXRepositoryUsers.GetUsersAsync(connection, typeof(GXModule), row.SourceId, token: token),
            "user-action" or "user-error" => [row.User?.Id ?? ServerHelpers.GetUserId(_user)],
            _ => null
        };
        if (owners == null && source != null && GXLogStore.GetDescriptor(source).Parent != null)
        {
            owners = [];
        }
        if (owners != null)
        {
            recipients = recipients.Intersect(owners);
        }
        return [.. recipients];
    }

    // Lifecycle operations share this repository's persistence rules and notification queue.
    internal static IModuleLogRepository ForModule(GXModuleRepositoryScope scope) => new ModuleOperations(scope);

    private sealed class ModuleOperations(GXModuleRepositoryScope scope) : IModuleLogRepository
    {
        public async Task<int> AddAsync(IReadOnlyList<AmiModuleLog> logs, IReadOnlyList<string> recipients, CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(logs);
            ArgumentNullException.ThrowIfNull(recipients);
            if (logs.Count == 0)
            {
                return 0;
            }
            var type = await scope.LogTypes.ReadAsync(token);
            if (type?.Enabled != true || type.Removed != null)
            {
                return 0;
            }
            foreach (var entry in logs)
            {
                if (entry.Duration < 0)
                {
                    throw new ArgumentException("Invalid log duration.");
                }
                if (entry.UserId != null && await scope.Connection.SingleOrDefaultAsync<GXUser>(scope.Transaction, GXSelectArgs.Select<GXUser>(u => u.Id!, u => u.Id == entry.UserId && u.Removed == null), token) == null)
                {
                    throw new ArgumentException("The log user is unavailable.");
                }
                var categoryId = await scope.Enums.EnsureLogCategoryAsync(entry.Category, token);
                var row = new GXLog(entry.Level)
                {
                    PublicId = Guid.NewGuid(),
                    LogType = type,
                    Type = categoryId,
                    SourceId = scope.ModuleId,
                    User = entry.UserId == null ? null : new GXUser { Id = entry.UserId },
                    CreationTime = entry.CreationTime ?? DateTimeOffset.UtcNow,
                    Message = entry.Message,
                    Data = entry.Data,
                    Duration = entry.Duration,
                    ExecutionId = entry.ExecutionId,
                    Closable = entry.Closable
                };
                Validator.ValidateObject(row, new ValidationContext(row), true);
                await GXLogStore.InsertAsync(scope.Connection, [row], token, scope.Transaction);
                await GXModuleRepositoryNotifications.QueueAsync(scope, "log.add", row.PublicId.ToString(), token, recipients);
            }
            return logs.Count;
        }

        public async Task RemoveAsync(CancellationToken token)
        {
            var moduleId = scope.ModuleId;
            if (await scope.Connection.DeleteAsync(scope.Transaction, GXDeleteArgs.Delete<GXLog>(l => l.SourceId == moduleId), token) != 0)
            {
                await GXModuleRepositoryNotifications.QueueAsync(scope, "log.clear", "", token);
            }
        }
    }
}
