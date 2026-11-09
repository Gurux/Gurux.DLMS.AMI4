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
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Shared.DTOs.Workflow;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Model;

namespace Gurux.DLMS.AMI.Server.Repository;

/// <summary>Stores Profile Generic rows and tracks successfully collected intervals independently of row counts.</summary>
/// <param name="factory">Creates database connections for stored history and interval metadata.</param>
/// <param name="accessor">Provides the current user or agent identity for access and task-claim validation.</param>
/// <param name="tasks">Queues agent tasks when requested history needs collection.</param>
/// <param name="notifications">Receives value and attribute invalidations after a completed interval is committed.</param>
/// <param name="workflow">Optional publisher of workflow events when the latest attribute value changes.</param>
/// <remarks>Time ranges include their start and exclude their end. Successful empty reads still record interval coverage.</remarks>
public sealed class ProfileHistoryRepository(IDbConnectionFactory factory, IHttpContextAccessor accessor, ITaskRepository tasks, NotificationCache notifications, IWorkflowHandler? workflow = null)
{
    internal static readonly SemaphoreSlim Gate = new(1, 1);
    private ClaimsPrincipal User => accessor.HttpContext?.User ?? throw new UnauthorizedAccessException();
    private static void ValidateRange(DateTimeOffset start, DateTimeOffset end)
    {
        if (start >= end || end - start > TimeSpan.FromDays(366)) {
            throw new ArgumentException("Invalid profile interval.");
        }
    }

    private static Guid? Reference(object? value)
    {
        if (value is null or DBNull) {
            return null;
        }
        if (value is Guid id) {
            return id;
        }
        if (value is byte[] bytes && bytes.Length == 16) {
            return new Guid(bytes);
        }
        return Guid.TryParse(Convert.ToString(value), out var parsed) ? parsed : null;
    }

    private async Task<GXAttribute> AttributeAsync(GXDbConnection db, Guid id, CancellationToken token, bool visible, IDbTransaction? transaction = null)
    {
        var q = visible && !User.IsInRole(GXRoles.Admin)
            ? GXQuery.GetAttributesByUser(a => "*", ServerHelpers.GetUserId(User), id)
            : GXSelectArgs.SelectAll<GXAttribute>(a => a.Id == id);
        var attribute = await db.SingleOrDefaultAsync<GXAttribute>(transaction, q, token) ?? throw new UnauthorizedAccessException("Profile is unavailable.");
        var refs = (await db.SelectAsync<object[]>(transaction, GXSelectArgs.Select<GXAttribute>(a => new { a.Template, a.Object }, a => a.Id == id), token)).Single();
        attribute.Template = new() { Id = Reference(refs[0]) ?? throw new ArgumentException("Profile template is unavailable.") };
        attribute.Object = new() { Id = Reference(refs[1]) ?? throw new ArgumentException("Profile object is unavailable.") };
        var template = await db.SingleOrDefaultAsync<GXAttributeTemplate>(transaction, GXSelectArgs.SelectAll<GXAttributeTemplate>(a => a.Id == attribute.Template!.Id), token);
        var obj = await db.SingleOrDefaultAsync<GXObject>(transaction, GXSelectArgs.SelectAll<GXObject>(o => o.Id == attribute.Object!.Id && o.Removed == null), token);
        if (obj != null)
        {
            var objectRefs = (await db.SelectAsync<object[]>(transaction, GXSelectArgs.Select<GXObject>(o => new { o.Template, o.Device }, o => o.Id == obj.Id), token)).Single();
            obj.Template = Reference(objectRefs[0]) is Guid templateId ? new() { Id = templateId } : null;
            obj.Device = Reference(objectRefs[1]) is Guid deviceId ? new() { Id = deviceId } : null;
        }
        var objectTemplate = obj == null ? null : await db.SingleOrDefaultAsync<GXObjectTemplate>(transaction, GXSelectArgs.SelectAll<GXObjectTemplate>(o => o.Id == obj.Template!.Id), token);
        if (obj?.Device == null || await db.SingleOrDefaultAsync<GXDevice>(transaction,
            GXSelectArgs.Select<GXDevice>(d => d.Id, d => d.Id == obj.Device.Id && d.Removed == null), token) == null)
            {
            throw new UnauthorizedAccessException("The profile device is unavailable.");
        }
        if (template?.Index != 2 || objectTemplate?.ObjectType != 7) {
            throw new ArgumentException("The attribute must be a Profile Generic buffer.");
        }
        attribute.Template = template;
        attribute.Object = obj;
        return attribute;
    }

    private async Task<GXTask> ClaimAsync(GXDbConnection db, Guid id, Guid attributeId, CancellationToken token, IDbTransaction? transaction = null)
    {
        var task = await db.SingleOrDefaultAsync<GXTask>(transaction, GXSelectArgs.SelectAll<GXTask>(t => t.Id == id), token);
        if (task != null)
        {
            var refs = (await db.SelectAsync<object[]>(transaction, GXSelectArgs.Select<GXTask>(t => new { t.OperatingAgent, t.Attribute, t.Object }, t => t.Id == id), token)).Single();
            task.OperatingAgent = Reference(refs[0]) is Guid assignedAgentId ? new() { Id = assignedAgentId } : null;
            task.Attribute = Reference(refs[1]) is Guid taskAttributeId ? new() { Id = taskAttributeId } : null;
            task.Object = Reference(refs[2]) is Guid objectId ? new() { Id = objectId } : null;
        }
        if (task?.Start == null || task.Ready != null || task.TaskType != TaskType.Read || task.OperatingAgent == null)
            {
            throw new UnauthorizedAccessException("An active profile task claim is required.");
        }
        var attribute = await AttributeAsync(db, attributeId, token, false, transaction);
        bool targetMatches = task.Attribute != null ? task.Attribute.Id == attributeId
            : task.Object != null ? task.Object.Id == attribute.Object!.Id && (task.Index == null || task.Index == 0 || task.Index == 2)
            : task.TargetDevice != null && task.TargetDevice == attribute.Object!.Device?.Id;
        if (!targetMatches) {
            throw new UnauthorizedAccessException("The profile is outside the claimed task target.");
        }
        Guid agentId = task.OperatingAgent.Id;
        var agent = await db.SingleOrDefaultAsync<GXAgent>(transaction, GXSelectArgs.SelectAll<GXAgent>(a => a.Id == agentId && a.Enabled == true && a.Removed == null), token);
        var membership = GXRepositoryUsers.Query(typeof(GXAgent), agentId);
        string userId = ServerHelpers.GetUserId(User);
        membership.Where.And<GXUser>(u => u.Id == userId);
        if (agent == null || (await db.SelectAsync<GXUser>(transaction, membership, token)).Count == 0) {
            throw new UnauthorizedAccessException("The assigned agent is unavailable.");
        }
        return task;
    }

    private static async Task<List<GXProfileHistoryParameters.ReadInterval>> SpansAsync(GXDbConnection db, Guid id, CancellationToken token, IDbTransaction? transaction = null)
    {
        EnsureSchema(db);
        return await GXProfileHistoryParameters.ListAsync(db, transaction, id, token);
    }

    private static void EnsureSchema(GXDbConnection db)
    {
        var schema = new GXSchemaManager(db);
        if (!schema.TableExist<GXObjectProperty>())
            {
            throw new ArgumentException("Profile history requires a database schema update. Apply the pending database table changes in server configuration.");
        }
    }

    private static GXProfileRange[] Coverage(IEnumerable<GXProfileHistoryParameters.ReadInterval> spans, DateTimeOffset now, bool refresh = true)
    {
        var complete = spans.Where(s => s.Completed).ToArray();
        DateTimeOffset fresh = refresh ? now.AddHours(-1) : DateTimeOffset.MaxValue;
        return GXProfileIntervals.Merge(complete.Where(s => s.Start < fresh && (!refresh || s.RowCount != 0 || s.CommittedAt >= now.AddDays(-1)))
            .Select(s => new GXProfileRange(s.Start, s.End < fresh ? s.End : fresh)));
    }

    private static GXProfileRange[] Clip(IEnumerable<GXProfileRange> ranges, DateTimeOffset start, DateTimeOffset end)
        => GXProfileIntervals.Merge(ranges.Where(r => r.Start < end && r.End > start)
            .Select(r => new GXProfileRange(r.Start > start ? r.Start : start, r.End < end ? r.End : end)));

    /// <summary>Returns paged stored rows and coverage, optionally scheduling collection of missing or forced intervals.</summary>
    /// <param name="request">Buffer attribute, time range, paging and forced reread options. Future end times are clipped to the current time.</param>
    /// <param name="read">Whether to queue a read task when the requested interval needs collection.</param>
    /// <param name="token">Token used to cancel database access and task creation.</param>
    /// <returns>Stored rows, total row count, interval coverage and any newly queued task identifiers.</returns>
    /// <exception cref="ArgumentException">The effective time range is empty, reversed or longer than 366 days.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller cannot access the requested buffer attribute.</exception>
    public async Task<GXProfileHistoryResponse> ListAsync(GXProfileHistoryRequest request, bool read, CancellationToken token)
    {
        request.End = request.End > DateTimeOffset.UtcNow ? DateTimeOffset.UtcNow : request.End;
        ValidateRange(request.Start, request.End);
        await using var db = await factory.GetHistoryConnectionAsync(token);
        var attribute = await AttributeAsync(db, request.AttributeId, token, true);
        var spans = await SpansAsync(db, attribute.Id, token);
        var current = spans.Where(s => s.Completed).ToArray();
        var complete = Clip(Coverage(spans, DateTimeOffset.UtcNow, false), request.Start, request.End);
        var covered = Coverage(spans, DateTimeOffset.UtcNow);
        var available = current.Where(s => s.AvailableFrom != null).OrderByDescending(s => s.CommittedAt).FirstOrDefault()?.AvailableFrom;
        var unavailable = available > request.Start ? Clip([new(request.Start, available.Value)], request.Start, request.End) : [];
        var missing = GXProfileIntervals.Missing(new(request.Start, request.End), request.Force ? [] : covered.Concat(unavailable), TimeSpan.FromDays(1));
        var q = GXSelectArgs.SelectAll<GXValue>(v => v.Attribute == attribute && v.Read >= request.Start && v.Read < request.End);
        q.OrderBy.Add<GXValue>(v => v.Read!);
        var total = GXSelectArgs.Select<GXValue>(v => GXSql.Count(1), v => v.Attribute == attribute && v.Read >= request.Start && v.Read < request.End);
        long count = await db.SingleOrDefaultAsync<long>(total, token);
        q.Index = Math.Max(0, request.Index);
        q.Count = Math.Clamp(request.Count, 1, 10000);
        var rows = await db.SelectAsync<GXValue>(q, token);
        var result = new GXProfileHistoryResponse
        {
            Count = count,
            Values = rows.Select(v => new GXProfileStoredRow { Id = v.Id, CapturedAt = v.Read!.Value, Value = v.Value }).ToArray(),
            Covered = complete,
            Missing = GXProfileIntervals.Missing(new(request.Start, request.End), complete.Concat(unavailable), TimeSpan.FromDays(1)),
            Unavailable = unavailable,
            EmptyRanges = Clip(current.Where(s => s.RowCount == 0).Select(s => new GXProfileRange(s.Start, s.End)), request.Start, request.End),
            RefreshRanges = GXProfileIntervals.Merge(Clip(complete, DateTimeOffset.UtcNow.AddHours(-1) > request.Start ? DateTimeOffset.UtcNow.AddHours(-1) : request.Start, request.End)
                .Concat(Clip(current.Where(s => s.RowCount == 0 && s.CommittedAt < DateTimeOffset.UtcNow.AddDays(-1)).Select(s => new GXProfileRange(s.Start, s.End)), request.Start, request.End)))
        };
        if (read && missing.Length != 0)
        {
            // One bounded intent; the agent obtains individual daily jobs from Begin.
            result.TaskIds = (await tasks.AddAsync([new GXTask
            {
                Attribute = attribute, Index = 2, TaskType = TaskType.Read,
                Data = JsonSerializer.Serialize(new GXProfileReadIntent { Start = request.Start, End = request.End, Force = request.Force })
            }], token)).ToArray();
        }
        return result;
    }

    /// <summary>Creates or reuses daily collection intervals for the caller's current profile task claim.</summary>
    /// <param name="request">Identifiers of the claimed task and its buffer attribute.</param>
    /// <param name="token">Token used to cancel claim validation and interval persistence.</param>
    /// <returns>At most 366 intervals, each no longer than one day, excluding coverage that does not need reading.</returns>
    /// <remarks>Explicit task intents control the range and forced reread. Without an intent, collection starts from existing history or the preceding day.</remarks>
    /// <exception cref="ArgumentException">The stored read intent or its effective time range is invalid.</exception>
    /// <exception cref="UnauthorizedAccessException">The task, attribute or current task claim does not belong to the caller.</exception>
    public async Task<GXProfileBeginResponse> BeginAsync(GXProfileBeginRequest request, CancellationToken token)
    {
        await Gate.WaitAsync(token);
        try
        {
            await using var db = await factory.GetHistoryConnectionAsync(token);
            var task = await ClaimAsync(db, request.TaskId, request.AttributeId, token);
            var attribute = await AttributeAsync(db, request.AttributeId, token, false);
            var spans = await SpansAsync(db, request.AttributeId, token);
            var now = DateTimeOffset.UtcNow;
            GXProfileReadIntent? intent = null;
            if (!string.IsNullOrWhiteSpace(task.Data))
            {
                try { intent = JsonSerializer.Deserialize<GXProfileReadIntent>(task.Data); }
                catch (JsonException) { throw new ArgumentException("Invalid profile read intent."); }
                if (intent?.Kind != GXProfileReadIntent.Format) {
                    throw new ArgumentException("Invalid profile read intent.");
                }
            }
            var start = intent?.Start ?? (spans.Count == 0 ? now.AddDays(-1) : spans.Min(s => s.Start));
            var end = intent?.End ?? now;
            if (end > now) {
                end = now;
            }
            if (intent != null) {
                ValidateRange(start, end);
            }
            else if (start >= end) {
                return new();
            }
            var current = spans.Where(s => s.Completed).ToArray();
            var available = current.Where(s => s.AvailableFrom != null).OrderByDescending(s => s.CommittedAt).FirstOrDefault()?.AvailableFrom;
            var covered = Coverage(spans, now).AsEnumerable();
            if (available > start) {
                covered = covered.Append(new(start, available.Value < end ? available.Value : end));
            }
            var missing = GXProfileIntervals.Missing(new(start, end), intent?.Force == true ? [] : covered, TimeSpan.FromDays(1)).Take(366);
            var jobs = new List<GXProfileReadInterval>();
            await using var transaction = await db.BeginTransactionAsync(token);
            foreach (var range in missing)
            {
                var span = spans.FirstOrDefault(s => !s.Completed && s.TaskId == task.Id && s.ClaimStart == task.Start && s.Start == range.Start && s.End == range.End);
                if (span == null)
                {
                    span = new GXProfileHistoryParameters.ReadInterval { Id = Guid.NewGuid(), AttributeId = request.AttributeId, ObjectId = attribute.Object!.Id, TaskId = task.Id, ClaimStart = task.Start, Start = range.Start, End = range.End };
                    await GXProfileHistoryParameters.SaveAsync(db, transaction, span, true, token);
                }
                jobs.Add(new() { IntervalId = span.Id, AttributeId = request.AttributeId, TaskId = task.Id, Start = span.Start, End = span.End });
            }
            await transaction.CommitAsync(token);
            return new() { Intervals = jobs.ToArray() };
        }
        finally { Gate.Release(); }
    }

    private static Guid RowId(Guid attributeId, GXProfileRow row, int occurrence)
    {
        var data = JsonSerializer.Serialize(new { attributeId, time = row.CapturedAt.ToUniversalTime().Ticks, row.Value, row.Sequence, occurrence });
        return new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(data)).AsSpan(0, 16));
    }

    /// <summary>Atomically stores completed interval rows and coverage, then publishes changes to the latest value.</summary>
    /// <param name="request">Interval and task identifiers, completion state, captured rows and optional earliest available meter time.</param>
    /// <param name="token">Token used to cancel validation, persistence and post-commit notification queries.</param>
    /// <returns>A task that completes when the result has been accepted and any resulting notifications queued.</returns>
    /// <remarks>Incomplete submissions and already completed intervals make no changes. Rows must lie within the interval's inclusive start and exclusive end.</remarks>
    /// <exception cref="ArgumentException">A returned row is outside the assigned interval or has no serialized value.</exception>
    /// <exception cref="UnauthorizedAccessException">The interval, task claim or associated object is unavailable to the caller or no longer matches.</exception>
    public async Task CommitAsync(GXProfileCommit request, CancellationToken token)
    {
        await Gate.WaitAsync(token);
        try
        {
            await using var db = await factory.GetHistoryConnectionAsync(token);
            EnsureSchema(db);
            await using var transaction = await db.BeginTransactionAsync(IsolationLevel.Serializable, token);
            var span = await GXProfileHistoryParameters.FindAsync(db, transaction, request.SpanId, token)
                ?? throw new UnauthorizedAccessException("Profile interval is unavailable.");
            if (span.TaskId != request.TaskId) {
                throw new UnauthorizedAccessException("Profile interval does not belong to this task.");
            }
            var task = await ClaimAsync(db, request.TaskId, span.AttributeId, token, transaction);
            if (span.ClaimStart != task.Start) {
                throw new UnauthorizedAccessException("The profile task claim is stale.");
            }
            var attribute = await AttributeAsync(db, span.AttributeId, token, false, transaction);
            if (attribute.Object!.Id != span.ObjectId)
                {
                throw new UnauthorizedAccessException("The profile interval belongs to another object.");
            }
            if (!request.Completed) {
                return;
            }
            if (span.Completed) {
                return;
            }
            if (request.Rows.Any(r => r.CapturedAt < span.Start || r.CapturedAt >= span.End || r.Value == null)) {
                throw new ArgumentException("A profile row is outside its requested interval.");
            }
            var events = new List<GXWorkflowEventContext>();
            var occurrences = new Dictionary<string, int>();
            foreach (var row in request.Rows)
            {
                var key = JsonSerializer.Serialize(new { time = row.CapturedAt.ToUniversalTime().Ticks, row.Value, row.Sequence });
                occurrences.TryGetValue(key, out int occurrence);
                occurrences[key] = occurrence + 1;
                var id = RowId(attribute.Id, row, occurrence);
                if (await db.SingleOrDefaultAsync<GXValue>(transaction, GXSelectArgs.Select<GXValue>(v => v.Id, v => v.Id == id), token) != null) {
                    continue;
                }
                await db.InsertAsync(transaction, GXInsertArgs.Insert(new GXValue { Id = id, Attribute = new() { Id = attribute.Id }, Read = row.CapturedAt, Value = row.Value, User = false }), token);
            }
            var latest = request.Rows.OrderByDescending(r => r.CapturedAt).FirstOrDefault();
            if (latest != null && (attribute.Read == null || latest.CapturedAt >= attribute.Read))
            {
                events.AddRange(GXValueWorkflowEvents.Create(attribute.Id, attribute.Object?.Device?.Id, attribute.Value, latest.Value, request.TaskId));
                attribute.Read = latest.CapturedAt;
                attribute.Value = latest.Value;
                await db.UpdateAsync(transaction, GXUpdateArgs.Update(attribute, a => new { a.Read, a.Value }), token);
            }
            span.RowCount = request.Rows.Length;
            span.Completed = true;
            span.CommittedAt = DateTimeOffset.UtcNow;
            span.AvailableFrom = request.AvailableFrom;
            await GXProfileHistoryParameters.SaveAsync(db, transaction, span, false, token);
            await transaction.CommitAsync(token);
            foreach (var context in events) {
                workflow?.Publish(context, User);
            }
            var users = await GXRepositoryUsers.GetNotifiedUsersAsync(factory, typeof(GXAttribute), attribute.Id, NotificationAction.Edit, GXDevicePolicies.View);
            notifications.Add(users, new GXTarget(GXValuePolicies.Add));
            notifications.Add(users, new GXTarget(GXAttributePolicies.Edit));
        }
        finally { Gate.Release(); }
    }
}
