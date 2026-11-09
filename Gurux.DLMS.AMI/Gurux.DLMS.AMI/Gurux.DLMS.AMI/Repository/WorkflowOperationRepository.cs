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
using System.Linq.Expressions;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Microsoft.AspNetCore.Authorization;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Persists authorized workflow operation state and publishes registered lifecycle events.</summary>
public sealed class WorkflowOperationRepository : IWorkflowOperationRepository
{
    private readonly ClaimsPrincipal _user;
    private readonly IDbConnectionFactory _dbFactory;
    private readonly IAuthorizationService _authorization;
    private readonly IWorkflowHandler _workflows;
    // Serializes lifecycle changes and publication acknowledgements within this server process.
    internal static readonly SemaphoreSlim Gate = new(1, 1);
    private string? ActorId => _user.Identity?.IsAuthenticated == true ? GXWorkflowIdentityAccess.ActorId(_user) : null;

    /// <summary>Creates a repository bound to the current caller.</summary>
    public WorkflowOperationRepository(IAuthorizationService authorizationService,
        IDbConnectionFactory dbFactory, IHttpContextAccessor httpContextAccessor, IWorkflowHandler workflows)
    {
        _user = httpContextAccessor.HttpContext?.User ?? throw new UnauthorizedAccessException();
        _dbFactory = dbFactory;
        _authorization = authorizationService;
        _workflows = workflows;
    }

    private void EnsureOwner(GXWorkflowOperation operation)
    {
        if (!_user.IsInRole(GXRoles.Admin) && (ActorId == null || operation.ActorId != ActorId))
            {
            throw new UnauthorizedAccessException("The workflow operation is unavailable.");
        }
    }

    private void Restrict(GXSelectArgs query, bool allUsers)
    {
        if (allUsers && _user.IsInRole(GXRoles.Admin))
            {
            return;
        }
        string actor = ActorId ?? throw new UnauthorizedAccessException();
        query.Where.And<GXWorkflowOperation>(o => o.ActorId == actor);
        query.Where.And<GXWorkflowOperation>(o => o.Code != "workflow.event" && o.Code != "workflow.delivery");
    }

    private void EnsurePublicMutation(GXWorkflowOperation operation)
    {
        EnsureOwner(operation);
        if (string.Equals(operation.Code, "workflow.event", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(operation.Code, "workflow.delivery", StringComparison.OrdinalIgnoreCase))
            {
            throw new UnauthorizedAccessException("Workflow delivery state is owned by the host.");
        }
    }

    /// <inheritdoc />
    public async Task StartAsync(GXWorkflowOperation operation, CancellationToken cancellationToken = default)
    {
        await _authorization.EnsureAnyPolicyAsync(_user, GXWorkflowPolicies.Run);
        ArgumentNullException.ThrowIfNull(operation);
        if (string.Equals(operation.Code, "workflow.event", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(operation.Code, "workflow.delivery", StringComparison.OrdinalIgnoreCase))
            {
            throw new UnauthorizedAccessException("Internal workflow delivery records are owned by the host.");
        }
        // Snapshot mutable caller-owned data before the first persistence await.
        var next = new GXWorkflowOperation
        {
            Id = operation.Id,
            Code = operation.Code,
            TargetId = operation.TargetId,
            ParentId = operation.ParentId,
            ActorId = ActorId,
            Data = operation.Data,
            Started = DateTimeOffset.UtcNow
        };
        if (next.Id == Guid.Empty || string.IsNullOrWhiteSpace(next.Code) || next.Code.Length > 128 ||
            next.Code != next.Code.Trim() || next.TargetId?.Length > 128 || next.ActorId?.Length > 450 ||
            next.ParentId == Guid.Empty || next.ParentId == next.Id ||
            operation.State != WorkflowOperationState.Running || operation.Finished != null ||
            operation.ErrorCode != null || operation.ErrorMessage != null)
            {
            throw new ArgumentException("Invalid initial workflow operation state.", nameof(operation));
        }
        if (operation.ActorId != null && operation.ActorId != next.ActorId)
            {
            throw new UnauthorizedAccessException("The initiating actor is assigned by the server.");
        }
        if (next.Data != null)
        {
            using var data = JsonDocument.Parse(next.Data);
            if (data.RootElement.ValueKind != JsonValueKind.Object)
                {
                throw new ArgumentException("Operation data must be a JSON object.", nameof(operation));
            }
        }
        next.IdempotencyKey = operation.IdempotencyKey;
        await StartTrustedAsync(_dbFactory, _workflows, next, cancellationToken, EnsureOwner);
    }
    /// <inheritdoc />
    public async Task FinishAsync(Guid operationId, WorkflowOperationState outcome, string? errorCode,
        string? errorMessage, CancellationToken cancellationToken = default)
    {
        await _authorization.EnsureAnyPolicyAsync(_user, GXWorkflowPolicies.Run);
        if (operationId == Guid.Empty || outcome is not (WorkflowOperationState.Completed or WorkflowOperationState.Failed or WorkflowOperationState.Cancelled)
            || errorCode?.Length > 128 || errorMessage?.Length > 2048)
            {
            throw new ArgumentException("Invalid terminal workflow operation state.");
        }
        await FinishTrustedAsync(_dbFactory, _workflows, operationId, outcome, errorCode, errorMessage, cancellationToken, EnsurePublicMutation);
    }
    /// <inheritdoc />
    public async Task<GXWorkflowOperation> ReadAsync(Guid id, Expression<Func<GXWorkflowOperation, object>>? columns = null,
        CancellationToken cancellationToken = default)
    {
        await _authorization.EnsureAnyPolicyAsync(_user, GXWorkflowOperationPolicies.View);
        var query = columns == null ? GXSelectArgs.SelectAll<GXWorkflowOperation>(o => o.Id == id)
            : GXSelectArgs.Select<GXWorkflowOperation>(columns, o => o.Id == id);
        Restrict(query, allUsers: true);
        await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
        return await connection.SingleOrDefaultAsync<GXWorkflowOperation>(query, cancellationToken)
            ?? throw new KeyNotFoundException("The workflow operation is unavailable.");
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GXWorkflowOperation>> ListAsync(ListWorkflowOperations? request = null,
        ListWorkflowOperationsResponse? response = null, CancellationToken cancellationToken = default)
    {
        await _authorization.EnsureAnyPolicyAsync(_user, GXWorkflowOperationPolicies.View);
        if (request is { Index: < 0 } or { Count: < 0 })
            {
            throw new ArgumentOutOfRangeException(nameof(request));
        }
        var query = GXSelectArgs.SelectAll<GXWorkflowOperation>();
        Restrict(query, request?.AllUsers == true);
        if (request != null)
        {
            query.Where.FilterBy(request.Filter);
            if (request.Included?.Any() == true)
                {
                query.Where.And<GXWorkflowOperation>(o => request.Included.Contains(o.Id));
            }
            if (request.Exclude?.Any() == true)
                {
                query.Where.And<GXWorkflowOperation>(o => !request.Exclude.Contains(o.Id));
            }
        }
        GXDateTimeFilters.Apply<GXWorkflowOperation>(query, request?.DateFilters);
        await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
        if (response != null)
        {
            var count = GXSelectArgs.Select<GXWorkflowOperation>(o => GXSql.Count(1));
            count.Where.Append(query.Where);
            response.Count = await connection.SingleOrDefaultAsync<long>(count, cancellationToken);
            response.WorkflowOperations = [];
            if (response.Count == 0)
                {
                return [];
            }
        }
        string order = string.IsNullOrEmpty(request?.OrderBy) ? nameof(GXWorkflowOperation.Started) : request.OrderBy;
        if (!new[] { nameof(GXWorkflowOperation.Id), nameof(GXWorkflowOperation.Code), nameof(GXWorkflowOperation.TargetId),
            nameof(GXWorkflowOperation.ParentId), nameof(GXWorkflowOperation.ActorId), nameof(GXWorkflowOperation.State),
            nameof(GXWorkflowOperation.Started), nameof(GXWorkflowOperation.Finished) }.Contains(order, StringComparer.Ordinal))
            {
            throw new ArgumentException("Invalid workflow operation sort column.", nameof(request));
        }
        query.OrderBy.Add<GXWorkflowOperation>(order);
        if (order != nameof(GXWorkflowOperation.Id))
            {
            query.OrderBy.Add<GXWorkflowOperation>(o => o.Id);
        }
        query.Descending = string.IsNullOrEmpty(request?.OrderBy) || request.Descending;
        if (request != null)
        { query.Index = request.Index; query.Count = request.Count; }
        var rows = await connection.SelectAsync<GXWorkflowOperation>(query, cancellationToken);
        if (response != null)
            {
            response.WorkflowOperations = rows;
        }
        return rows;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(IEnumerable<Guid> operations, CancellationToken cancellationToken = default)
    {
        await _authorization.EnsureAnyPolicyAsync(_user, GXWorkflowOperationPolicies.Delete);
        ArgumentNullException.ThrowIfNull(operations);
        var ids = operations.Distinct().ToArray();
        if (ids.Length == 0)
            {
            return;
        }
        if (ids.Contains(Guid.Empty))
            {
            throw new ArgumentException("Invalid workflow operation identifier.", nameof(operations));
        }
        await Gate.WaitAsync(cancellationToken);
        try
        {
            await using var connection = await _dbFactory.GetHistoryConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var rows = await connection.SelectAsync<GXWorkflowOperation>(transaction,
                GXSelectArgs.SelectAll<GXWorkflowOperation>(o => ids.Contains(o.Id)), cancellationToken);
            if (rows.Count != ids.Length)
                {
                throw new KeyNotFoundException("A workflow operation is unavailable.");
            }
            foreach (var row in rows)
            {
                EnsurePublicMutation(row);
                if (row.State == WorkflowOperationState.Running)
                    {
                    throw new InvalidOperationException("A running workflow operation cannot be deleted.");
                }
            }
            var parentIds = ids.Select(id => (Guid?)id).ToArray();
            var children = GXSelectArgs.Select<GXWorkflowOperation>(o => o.Id,
                o => parentIds.Contains(o.ParentId) && !ids.Contains(o.Id));
            if ((await connection.SelectAsync<GXWorkflowOperation>(transaction, children, cancellationToken)).Count != 0)
                {
                throw new InvalidOperationException("Delete child operations before deleting their parent.");
            }
            await connection.DeleteAsync(transaction, GXDeleteArgs.Delete<GXWorkflowOperation>(o => ids.Contains(o.Id)), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        finally { Gate.Release(); }
    }

    // These entry points are host-only: callers derive actor and target from authoritative server state.
    // HTTP lifecycle reports must validate their task assignment before using the record helpers.
    internal static async Task StartTrustedAsync(IDbConnectionFactory factory, IWorkflowHandler workflows,
        GXWorkflowOperation operation, CancellationToken token, Action<GXWorkflowOperation>? ensureAccess = null)
    {
        await Gate.WaitAsync(token);
        try
        {
            await using var connection = await factory.GetHistoryConnectionAsync(token);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, token);
            var stored = await StartRecordAsync(connection, transaction, operation, token, ensureAccess);
            await transaction.CommitAsync(token);
            await PublishPendingAsync(connection, workflows, stored);
        }
        finally { Gate.Release(); }
    }

    internal static async Task FinishTrustedAsync(IDbConnectionFactory factory, IWorkflowHandler workflows,
        Guid id, WorkflowOperationState outcome, string? errorCode, string? errorMessage,
        CancellationToken token, Action<GXWorkflowOperation>? ensureAccess = null)
    {
        await Gate.WaitAsync(token);
        try
        {
            await using var connection = await factory.GetHistoryConnectionAsync(token);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, token);
            var stored = await FinishRecordAsync(connection, transaction, id, outcome, errorCode, errorMessage, token, ensureAccess);
            await transaction.CommitAsync(token);
            await PublishPendingAsync(connection, workflows, stored);
        }
        finally { Gate.Release(); }
    }

    internal static async Task<GXWorkflowOperation> StartRecordAsync(GXDbConnection connection, IDbTransaction transaction,
        GXWorkflowOperation operation, CancellationToken token, Action<GXWorkflowOperation>? ensureAccess = null)
    {
        if (operation.Id == Guid.Empty || string.IsNullOrWhiteSpace(operation.Code) || operation.Code.Length > 128 ||
            operation.TargetId?.Length > 128 || operation.ActorId?.Length > 450 ||
            operation.ParentId == Guid.Empty || operation.ParentId == operation.Id || operation.State != WorkflowOperationState.Running ||
            operation.Finished != null || operation.ErrorCode != null || operation.ErrorMessage != null)
            {
            throw new ArgumentException("Invalid initial workflow operation state.");
        }
        string key = string.IsNullOrEmpty(operation.IdempotencyKey) ? operation.Id.ToString("N") : operation.IdempotencyKey;
        if (key.Length > 256) {
            throw new ArgumentException("The operation idempotency key is too long.");
        }
        if (operation.Data != null)
        {
            using var json = JsonDocument.Parse(operation.Data);
            if (json.RootElement.ValueKind != JsonValueKind.Object) {
                throw new ArgumentException("Operation data must be a JSON object.");
            }
        }
        GXWorkflowOperation? parent = null;
        if (operation.ParentId is { } parentId)
        {
            parent = await connection.SingleOrDefaultAsync<GXWorkflowOperation>(transaction,
                GXSelectArgs.SelectAll<GXWorkflowOperation>(o => o.Id == parentId), token)
                ?? throw new KeyNotFoundException("The parent workflow operation is unavailable.");
            ensureAccess?.Invoke(parent);
        }
        string? data = GXWorkflowService.AttachChain(operation.Data, parent);
        var existing = await connection.SingleOrDefaultAsync<GXWorkflowOperation>(transaction,
            GXSelectArgs.SelectAll<GXWorkflowOperation>(o => o.Id == operation.Id), token);
        if (existing != null)
        {
            ensureAccess?.Invoke(existing);
            if (existing.Code != operation.Code || existing.TargetId != operation.TargetId || existing.ParentId != operation.ParentId ||
                existing.ActorId != operation.ActorId || existing.Data != data || existing.IdempotencyKey != key)
                {
                throw new InvalidOperationException("The operation identifier is already bound to different initial data.");
            }
            return existing;
        }
        if (await connection.SingleOrDefaultAsync<GXWorkflowOperation>(transaction,
            GXSelectArgs.Select<GXWorkflowOperation>(o => o.Id, o => o.IdempotencyKey == key), token) != null)
            {
            throw new InvalidOperationException("This logical request already has an operation.");
        }
        var stored = new GXWorkflowOperation { Id = operation.Id, IdempotencyKey = key, Code = operation.Code,
            TargetId = operation.TargetId, ParentId = operation.ParentId, ActorId = operation.ActorId,
            Data = data, Started = DateTimeOffset.UtcNow };
        await connection.InsertAsync(transaction, GXInsertArgs.Insert(stored), token);
        return stored;
    }

    internal static async Task<GXWorkflowOperation> FinishRecordAsync(GXDbConnection connection, IDbTransaction transaction,
        Guid id, WorkflowOperationState outcome, string? errorCode, string? errorMessage, CancellationToken token,
        Action<GXWorkflowOperation>? ensureAccess = null)
    {
        if (outcome is not (WorkflowOperationState.Completed or WorkflowOperationState.Failed or WorkflowOperationState.Cancelled) ||
            errorCode?.Length > 128 || errorMessage?.Length > 2048)
            {
            throw new ArgumentException("Invalid terminal workflow operation state.");
        }
        var operation = await connection.SingleOrDefaultAsync<GXWorkflowOperation>(transaction,
            GXSelectArgs.SelectAll<GXWorkflowOperation>(o => o.Id == id), token)
            ?? throw new KeyNotFoundException("The workflow operation is unavailable.");
        ensureAccess?.Invoke(operation);
        if (operation.State != WorkflowOperationState.Running)
        {
            if (operation.State != outcome || operation.ErrorCode != errorCode || operation.ErrorMessage != errorMessage)
                {
                throw new InvalidOperationException("A terminal workflow operation cannot be changed.");
            }
        }
        else
        {
            operation.State = outcome;
            operation.Finished = DateTimeOffset.UtcNow;
            operation.ErrorCode = errorCode;
            operation.ErrorMessage = errorMessage;
            await connection.UpdateAsync(transaction, GXUpdateArgs.Update(operation,
                o => new { o.State, o.Finished, o.ErrorCode, o.ErrorMessage }), token);
        }
        return operation;
    }
    internal static async Task PublishPendingAsync(GXDbConnection connection, IWorkflowHandler workflows, GXWorkflowOperation operation)
    {
        // Existing schedule event codes use a dot; device operation event codes use a hyphen.
        string prefix = operation.Code == "schedule.run" ? "schedule." : operation.Code + "-";
        string outcome = operation.State switch
        {
            WorkflowOperationState.Completed => "completed",
            WorkflowOperationState.Failed => "failed",
            WorkflowOperationState.Cancelled => "cancelled",
            _ => "started"
        };
        var actor = operation.ActorId == null ? new ClaimsPrincipal(new ClaimsIdentity())
            : new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, operation.ActorId)], "WorkflowEvent"));
        async Task Publish(string suffix, string state, DateTimeOffset occurredAt, Expression<Func<GXWorkflowOperation, object>> acknowledgement)
        {
            string code = prefix + suffix;
            if (GXTriggerEventCatalog.TryResolveEvent(code, out _, out _))
            {
                var context = new GXWorkflowEventContext
                {
                    EventId = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(operation.Id.ToString("N") + ":" + code)).AsSpan(0, 16)),
                    EventCode = code,
                    OperationId = operation.Id,
                    TargetId = operation.TargetId,
                    OccurredAt = occurredAt,
                    Outcome = state,
                    ErrorCode = state == "started" ? null : operation.ErrorCode,
                    ErrorMessage = state == "started" ? null : operation.ErrorMessage
                };
                if (Guid.TryParse(operation.TargetId, out var target))
                {
                    if (operation.Code.StartsWith("device.", StringComparison.Ordinal))
                        {
                        context.DeviceId = target;
                    }
                    if (operation.Code == "schedule.run")
                        {
                        context.ScheduleId = target;
                    }
                }
                // Only task correlation is read from producer-owned data. It cannot override resource access or actor identity.
                if (operation.Data != null)
                {
                    using var data = JsonDocument.Parse(operation.Data);
                    if (data.RootElement.TryGetProperty("TaskId", out var task) && task.ValueKind == JsonValueKind.String && task.TryGetGuid(out var taskId))
                        {
                        context.TaskId = taskId;
                    }
                    if (data.RootElement.TryGetProperty("ChainId", out var chain) && chain.ValueKind == JsonValueKind.String && chain.TryGetGuid(out var chainId))
                        {
                        context.ChainId = chainId;
                    }
                    if (data.RootElement.TryGetProperty("Depth", out var depth) && depth.ValueKind == JsonValueKind.Number && depth.TryGetInt32(out var eventDepth))
                        {
                        context.Depth = eventDepth;
                    }
                }
                workflows.Publish(context, actor);
            }
            await connection.UpdateAsync(GXUpdateArgs.Update(operation, acknowledgement), CancellationToken.None);
        }
        if (!operation.StartedPublished)
        {
            operation.StartedPublished = true;
            await Publish("started", "started", operation.Started, o => o.StartedPublished);
        }
        if (operation.Finished is { } finished)
        {
            if (!operation.OutcomePublished)
            {
                operation.OutcomePublished = true;
                await Publish(outcome, outcome, finished, o => o.OutcomePublished);
            }
            if (!operation.FinishedPublished)
            {
                operation.FinishedPublished = true;
                await Publish("finished", outcome, finished, o => o.FinishedPublished);
            }
        }
    }
}


