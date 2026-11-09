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
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Server.Repository;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Trigger;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm;

namespace Gurux.DLMS.AMI.Server.Services;

internal sealed partial class GXWorkflowService
{
    private const string EventRecord = "workflow.event";
    private const string DeliveryRecord = "workflow.delivery";
    private static readonly SemaphoreSlim DeliveryGate = new(1, 1);
    private sealed record InvocationScope(Guid ChainId, int Depth, Guid DeliveryId, string? ActorId, CancellationToken Token);
    private static readonly AsyncLocal<InvocationScope?> Current = new();
    internal static Guid? CurrentOperationId => Current.Value?.DeliveryId is { } id && id != Guid.Empty ? id : null;
    internal static string? CurrentActorId => Current.Value?.ActorId;
    internal static void ThrowIfExecutionCancelled() => Current.Value?.Token.ThrowIfCancellationRequested();
    internal static object? CaptureExecutionScope() => Current.Value;

    internal static async Task<T> RunInExecutionScopeAsync<T>(object? captured, Func<Task<T>> action)
    {
        var previous = Current.Value;
        if (captured is InvocationScope scope)
        {
            scope.Token.ThrowIfCancellationRequested();
            Current.Value = scope;
        }
        try
        { return await action(); }
        finally { Current.Value = previous; }
    }

    internal static int ExecutionSetting(GXWorkflow workflow, string name, int defaultValue, int maximum)
    {
        var values = workflow.Properties?.Where(p => p.Module == null && p.Removed == null && p.Name == name).ToArray() ?? [];
        if (values.Length == 0)
            {
            return defaultValue;
        }
        if (values.Length != 1 || !int.TryParse(values[0].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            || value < 1 || value > maximum)
            {
            throw new ArgumentException($"{name} must contain one integer from 1 to {maximum}.");
        }
        return value;
    }

    internal static void ValidateExecutionSettings(GXWorkflow workflow)
    {
        _ = ExecutionSetting(workflow, "execution.timeout-seconds", 60, 3600);
        _ = ExecutionSetting(workflow, "execution.max-depth", 16, 64);
    }

    internal static string? AttachChain(string? data, GXWorkflowOperation? parent = null)
    {
        var current = Current.Value;
        current?.Token.ThrowIfCancellationRequested();
        JsonObject? inherited = parent?.Data == null ? null : JsonNode.Parse(parent.Data) as JsonObject;
        Guid chainId = current?.ChainId ?? (Guid.TryParse(inherited?["ChainId"]?.ToString(), out var inheritedId) ? inheritedId : Guid.Empty);
        int depth = current != null ? current.Depth + 1 : (int.TryParse(inherited?["Depth"]?.ToString(), out int inheritedDepth) ? inheritedDepth + 1 : 0);
        if (chainId == Guid.Empty)
        {
            if (data == null)
                {
                return null;
            }
            var original = JsonNode.Parse(data) as JsonObject ?? throw new ArgumentException("Operation data must be a JSON object.");
            bool changed = original.Remove("ChainId") | original.Remove("Depth");
            return changed ? original.ToJsonString() : data;
        }
        var json = data == null ? new JsonObject() : JsonNode.Parse(data) as JsonObject ?? throw new ArgumentException("Operation data must be a JSON object.");
        json["ChainId"] = chainId.ToString();
        json["Depth"] = depth;
        return json.ToJsonString();
    }

    private static void SetChain(GXActionInfo invocation)
    {
        var current = Current.Value;
        current?.Token.ThrowIfCancellationRequested();
        invocation.ChainId = current?.ChainId ?? (invocation.EventContext?.ChainId is { } id && id != Guid.Empty ? id : invocation.EventId);
        invocation.Depth = current != null ? current.Depth + 1 : invocation.EventContext?.Depth ?? 0;
        if (invocation.Depth < 0)
            {
            throw new ArgumentException("Workflow event depth cannot be negative.");
        }
        invocation.Depth = Math.Min(invocation.Depth, 65);
        if (invocation.EventContext != null)
        {
            invocation.EventContext.ChainId = invocation.ChainId;
            invocation.EventContext.Depth = invocation.Depth;
        }
    }

    private static string SerializeInvocation(GXActionInfo invocation)
    {
        if (invocation.Sender is not (null or GXWorkflowEventContext or GXUser or ClaimsPrincipal or ApplicationUser))
            {
            throw new ArgumentException("Durable custom workflow events must use GXWorkflowEventContext.");
        }
        var user = invocation.Sender as GXUser;
        var account = invocation.Sender as ApplicationUser;
        return JsonSerializer.Serialize(new
        {
            Trigger = invocation.Trigger.FullName,
            invocation.Activity,
            invocation.WorkflowId,
            invocation.EventId,
            invocation.QueuedAt,
            invocation.Manual,
            invocation.EventContext,
            invocation.ChainId,
            invocation.Depth,
            UserId = user?.Id ?? account?.Id,
            UserName = user?.UserName ?? account?.UserName,
            ApplicationUser = account != null
        });
    }

    private static GXActionInfo DeserializeInvocation(GXWorkflowOperation record)
    {
        using var json = JsonDocument.Parse(record.Data ?? throw new InvalidOperationException("The workflow delivery has no data."));
        var root = json.RootElement;
        string name = root.GetProperty("Trigger").GetString()!;
        var types = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).OfType<Type>().Distinct().ToArray();
        if (types.Length != 1)
            {
            throw new InvalidOperationException("The workflow event source is unavailable or ambiguous.");
        }
        var context = root.GetProperty("EventContext").ValueKind == JsonValueKind.Null ? null : root.GetProperty("EventContext").Deserialize<GXWorkflowEventContext>();
        bool manual = root.GetProperty("Manual").GetBoolean();
        string? userId = root.GetProperty("UserId").GetString();
        var invocation = new GXActionInfo
        {
            Trigger = types[0],
            Activity = root.GetProperty("Activity").GetString()!,
            Sender = context ?? (object?)(userId == null ? null : root.GetProperty("ApplicationUser").GetBoolean()
                ? new ApplicationUser { Id = userId, UserName = root.GetProperty("UserName").GetString() }
                : new GXUser { Id = userId, UserName = root.GetProperty("UserName").GetString() }),
            WorkflowId = root.GetProperty("WorkflowId").ValueKind == JsonValueKind.Null ? null : root.GetProperty("WorkflowId").GetGuid(),
            EventId = root.GetProperty("EventId").GetGuid(),
            ActorId = record.ActorId,
            Manual = manual,
            EventContext = context,
            QueuedAt = root.GetProperty("QueuedAt").GetDateTimeOffset(),
            ChainId = root.GetProperty("ChainId").GetGuid(),
            Depth = root.GetProperty("Depth").GetInt32()
        };
        if (manual)
            {
            invocation.Sender = record.ActorId == null ? new ClaimsPrincipal() :
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, record.ActorId)], "WorkflowEvent"));
        }
        return invocation;
    }

    private bool EnqueueDurable(GXActionInfo invocation)
        => Task.Run(() => EnqueueDurableAsync(invocation, CancellationToken.None)).GetAwaiter().GetResult();

    private async Task<bool> EnqueueDurableAsync(GXActionInfo invocation, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var factory = _serviceProvider?.GetService<IDbConnectionFactory>();
        if (factory == null)
            {
            return false;
        } // Isolated queue inspection tools can operate without a database.
        SetChain(invocation);
        await PersistEventAsync(factory, invocation, token);
        signal.Release();
        return true;
    }

    private static async Task<Guid[]> MatchingWorkflowsAsync(GXDbConnection connection, IDbTransaction transaction,
        GXActionInfo invocation, CancellationToken token)
    {
        if (invocation.Manual)
            {
            return invocation.WorkflowId is { } id ? [id] : [];
        }
        var query = GXSelectArgs.Select<GXWorkflow>(w => w.Id,
            w => w.Enabled == true && w.Removed == null && w.Phase == TriggerPhase.After);
        query.Joins.AddInnerJoin<GXWorkflow, GXTriggerActivity>(w => w.TriggerActivity!, a => a.Id);
        query.Joins.AddInnerJoin<GXTriggerActivity, GXTrigger>(a => a.Trigger!, t => t.Id);
        query.Where.And<GXTriggerActivity>(a => a.Name == invocation.Activity);
        query.Where.And<GXTrigger>(t => t.ClassName == invocation.Trigger.FullName);
        return (await connection.SelectAsync<GXWorkflow>(transaction, query, token)).Select(w => w.Id).ToArray();
    }

    internal static async Task PersistEventAsync(IDbConnectionFactory factory, GXActionInfo invocation, CancellationToken token)
    {
        string key = $"{EventRecord}:{invocation.EventId:N}";
        string data = SerializeInvocation(invocation);
        // A history writer already owns the database transaction. Waiting for the delivery
        // gate here would invert the worker's gate-then-database acquisition order.
        bool ownsGate = !GXHistoryScope.IsActive;
        if (ownsGate)
        {
            await DeliveryGate.WaitAsync(token);
        }
        try
        {
            await using var connection = await factory.GetHistoryConnectionAsync(token);
            // Oracle can reject an unrelated event insert under Serializable while its
            // delivery worker changes the operation table. The recipient SELECT is one
            // consistent snapshot; commit that snapshot and its event atomically.
            bool oracle = connection.DatabaseType == Gurux.Service.Orm.Common.Enums.DatabaseType.Oracle;
            await using var transaction = await connection.BeginTransactionAsync(
                oracle ? IsolationLevel.ReadCommitted : IsolationLevel.Serializable, token);
            var existing = await connection.SingleOrDefaultAsync<GXWorkflowOperation>(transaction,
                GXSelectArgs.SelectAll<GXWorkflowOperation>(o => o.IdempotencyKey == key), token);
            if (existing == null)
            {
                // Snapshot recipients at publication time; recovery must not enroll newly created workflows.
                var envelope = JsonNode.Parse(data)!.AsObject();
                envelope["WorkflowIds"] = JsonSerializer.SerializeToNode(await MatchingWorkflowsAsync(connection, transaction, invocation, token));
                data = envelope.ToJsonString();
                try
                {
                    await connection.InsertAsync(transaction, GXInsertArgs.Insert(new GXWorkflowOperation
                    {
                        Id = invocation.EventId,
                        IdempotencyKey = key,
                        Code = EventRecord,
                        ActorId = invocation.ActorId,
                        Data = data,
                        Started = invocation.QueuedAt,
                        State = invocation.Depth > 64 ? WorkflowOperationState.Cancelled : WorkflowOperationState.Running,
                        Finished = invocation.Depth > 64 ? DateTimeOffset.UtcNow : null,
                        ErrorCode = invocation.Depth > 64 ? "chain-depth" : null,
                        ErrorMessage = invocation.Depth > 64 ? "The workflow event chain exceeded its maximum depth." : null,
                        StartedPublished = true,
                        OutcomePublished = true,
                        FinishedPublished = true
                    }), token);
                }
                catch (GXDatabaseException error) when (oracle && error.ErrorCode == 1)
                {
                    // A second server may publish the same event after our initial read.
                    // Resolve only that unique-key race; never retry publication or replace
                    // the winner's recipient snapshot or initiating actor.
                    await transaction.RollbackAsync(token);
                    var winner = await connection.SingleOrDefaultAsync<GXWorkflowOperation>(
                        GXSelectArgs.SelectAll<GXWorkflowOperation>(o => o.IdempotencyKey == key), token);
                    if (winner == null)
                        {
                        throw;
                    }
                    if (winner.ActorId != invocation.ActorId)
                        {
                        throw new InvalidOperationException("The workflow event identifier is already bound to another actor.");
                    }
                    return;
                }
            }
            else if (existing.ActorId != invocation.ActorId)
                {
                throw new InvalidOperationException("The workflow event identifier is already bound to another actor.");
            }
            await transaction.CommitAsync(token);
        }
        finally
        {
            if (ownsGate)
            {
                DeliveryGate.Release();
            }
        }
    }

    internal static async Task<int> FanOutAsync(IDbConnectionFactory factory, CancellationToken token)
    {
        await DeliveryGate.WaitAsync(token);
        try
        {
            await using var connection = await factory.GetConnectionAsync(token);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, token);
            var query = GXSelectArgs.SelectAll<GXWorkflowOperation>(o => o.Code == EventRecord && o.State == WorkflowOperationState.Running);
            query.OrderBy.Add<GXWorkflowOperation>(o => o.Started);
            query.OrderBy.Add<GXWorkflowOperation>(o => o.Id);
            query.Count = 100;
            var events = await connection.SelectAsync<GXWorkflowOperation>(transaction, query, token);
            int count = 0;
            foreach (var record in events)
            {
                try
                {
                    var invocation = DeserializeInvocation(record);
                    using var envelope = JsonDocument.Parse(record.Data!);
                    var workflowIds = envelope.RootElement.GetProperty("WorkflowIds").Deserialize<Guid[]>()
                        ?? throw new InvalidOperationException("The workflow event has no recipient snapshot.");
                    foreach (var workflowId in workflowIds)
                    {
                        var delivery = DeserializeInvocation(record);
                        delivery.WorkflowId = workflowId;
                        string key = $"{DeliveryRecord}:{delivery.EventId:N}:{workflowId:N}";
                        Guid id = new(SHA256.HashData(Encoding.UTF8.GetBytes(key)).AsSpan(0, 16));
                        if (await connection.SingleOrDefaultAsync<GXWorkflowOperation>(transaction,
                            GXSelectArgs.Select<GXWorkflowOperation>(o => o.Id, o => o.Id == id), token) == null)
                        {
                            await connection.InsertAsync(transaction, GXInsertArgs.Insert(new GXWorkflowOperation
                            {
                                Id = id,
                                IdempotencyKey = key,
                                Code = DeliveryRecord,
                                ParentId = record.Id,
                                TargetId = workflowId.ToString(),
                                ActorId = record.ActorId,
                                Data = SerializeInvocation(delivery),
                                Started = record.Started,
                                StartedPublished = true,
                                OutcomePublished = true,
                                FinishedPublished = true
                            }), token);
                            count++;
                        }
                    }
                    record.State = WorkflowOperationState.Completed;
                    record.Finished = DateTimeOffset.UtcNow;
                }
                catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or ArgumentException)
                {
                    record.State = WorkflowOperationState.Failed;
                    record.Finished = DateTimeOffset.UtcNow;
                    record.ErrorCode = "invalid-event";
                    record.ErrorMessage = error.Message[..Math.Min(error.Message.Length, 2048)];
                }
                await connection.UpdateAsync(transaction, GXUpdateArgs.Update(record, o => new { o.State, o.Finished, o.ErrorCode, o.ErrorMessage }), token);
            }
            await transaction.CommitAsync(token);
            return count;
        }
        finally { DeliveryGate.Release(); }
    }

    internal static async Task<GXWorkflowOperation?> LeaseNextAsync(IDbConnectionFactory factory, CancellationToken token)
    {
        await DeliveryGate.WaitAsync(token);
        try
        {
            await using var connection = await factory.GetConnectionAsync(token);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, token);
            var now = DateTimeOffset.UtcNow;
            var query = GXSelectArgs.SelectAll<GXWorkflowOperation>(o => o.Code == DeliveryRecord && o.State == WorkflowOperationState.Running &&
                (o.LeaseExpires == null || o.LeaseExpires < now));
            query.Count = 1;
            query.OrderBy.Add<GXWorkflowOperation>(o => o.Started);
            var delivery = await connection.SingleOrDefaultAsync<GXWorkflowOperation>(transaction, query, token);
            if (delivery != null)
            {
                delivery.LeaseId = Guid.NewGuid();
                delivery.LeaseExpires = now.AddSeconds(60);
                await connection.UpdateAsync(transaction, GXUpdateArgs.Update(delivery, o => new { o.LeaseId, o.LeaseExpires }), token);
            }
            await transaction.CommitAsync(token);
            return delivery;
        }
        finally { DeliveryGate.Release(); }
    }

    private static async Task HeartbeatAsync(IDbConnectionFactory factory, GXWorkflowOperation delivery, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(15), token);
            await using var connection = await factory.GetConnectionAsync(token);
            var expiry = DateTimeOffset.UtcNow.AddSeconds(60);
            var update = GXUpdateArgs.Update(new GXWorkflowOperation { Id = delivery.Id, LeaseExpires = expiry }, o => o.LeaseExpires!);
            update.Where.And<GXWorkflowOperation>(o => o.LeaseId == delivery.LeaseId && o.State == WorkflowOperationState.Running);
            if (await connection.UpdateAsync(update, token) != 1)
                {
                throw new InvalidOperationException("The workflow delivery lease was lost.");
            }
        }
    }

    internal static async Task CompleteDeliveryAsync(IDbConnectionFactory factory, GXWorkflowOperation delivery,
        GXWorkflowExecution history, bool interrupted, CancellationToken token)
    {
        await using var connection = await factory.GetConnectionAsync(token);
        delivery.State = interrupted ? WorkflowOperationState.Running : history.Status switch
        {
            WorkflowExecutionStatus.Failed or WorkflowExecutionStatus.TimedOut => WorkflowOperationState.Failed,
            WorkflowExecutionStatus.Cancelled => WorkflowOperationState.Cancelled,
            _ => WorkflowOperationState.Completed
        };
        delivery.Finished = interrupted ? null : DateTimeOffset.UtcNow;
        delivery.ErrorMessage = history.Message?[..Math.Min(history.Message.Length, 2048)];
        JsonObject data;
        try
        { data = JsonNode.Parse(delivery.Data ?? "{}") as JsonObject ?? new JsonObject(); }
        catch (JsonException) { data = new JsonObject { ["InvalidEnvelope"] = true }; }
        data["LastExecution"] = JsonSerializer.SerializeToNode(history);
        delivery.Data = data.ToJsonString();
        var expectedLease = delivery.LeaseId;
        delivery.LeaseId = null;
        delivery.LeaseExpires = null;
        var update = GXUpdateArgs.Update(delivery, o => new { o.State, o.Finished, o.ErrorCode, o.ErrorMessage, o.LeaseId, o.LeaseExpires, o.Data });
        update.Where.And<GXWorkflowOperation>(o => o.LeaseId == expectedLease);
        if (await connection.UpdateAsync(update, token) != 1)
            {
            throw new InvalidOperationException("The workflow delivery lease was lost.");
        }
    }

    private async Task ResumeSourcePublicationsAsync(IDbConnectionFactory factory, CancellationToken token)
    {
        // Replay source lifecycle events left unacknowledged by an interrupted publication.
        await WorkflowOperationRepository.Gate.WaitAsync(token);
        try
        {
            await using var connection = await factory.GetConnectionAsync(token);
            var query = GXSelectArgs.SelectAll<GXWorkflowOperation>(o => o.Code != EventRecord && o.Code != DeliveryRecord &&
                (o.StartedPublished == false || (o.Finished != null && (o.OutcomePublished == false || o.FinishedPublished == false))));
            query.OrderBy.Add<GXWorkflowOperation>(o => o.Started);
            query.OrderBy.Add<GXWorkflowOperation>(o => o.Id);
            query.Count = 100;
            foreach (var operation in await connection.SelectAsync<GXWorkflowOperation>(query, token))
            {
                try
                { await WorkflowOperationRepository.PublishPendingAsync(connection, this, operation); }
                catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException)
                {
                    // Quarantine malformed producer metadata so a single source cannot block unrelated deliveries.
                    string key = $"{EventRecord}:{operation.Id:N}:invalid-publication";
                    if (await connection.SingleOrDefaultAsync<GXWorkflowOperation>(GXSelectArgs.Select<GXWorkflowOperation>(o => o.Id, o => o.IdempotencyKey == key), token) == null)
                        {
                        await connection.InsertAsync(GXInsertArgs.Insert(new GXWorkflowOperation
                        {
                            Id = Guid.NewGuid(),
                            IdempotencyKey = key,
                            Code = EventRecord,
                            ParentId = operation.Id,
                            ActorId = operation.ActorId,
                            State = WorkflowOperationState.Failed,
                            Started = DateTimeOffset.UtcNow,
                            Finished = DateTimeOffset.UtcNow,
                            ErrorCode = "invalid-publication",
                            ErrorMessage = error.Message[..Math.Min(error.Message.Length, 2048)],
                            StartedPublished = true,
                            OutcomePublished = true,
                            FinishedPublished = true
                        }), token);
                    }
                    operation.StartedPublished = operation.OutcomePublished = operation.FinishedPublished = true;
                    await connection.UpdateAsync(GXUpdateArgs.Update(operation, o => new { o.StartedPublished, o.OutcomePublished, o.FinishedPublished }), token);
                }
            }
        }
        finally { WorkflowOperationRepository.Gate.Release(); }
    }
}

