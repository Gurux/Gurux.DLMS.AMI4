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

using Gurux.DLMS.AMI.Client.Pages.User;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Internal;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Enums;
using System.Data.Odbc;
using System.Runtime.ExceptionServices;
using System.Security.Claims;
using System.Threading.Channels;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Gurux.DLMS.AMI.Server.Cron;

/// <summary>A task resolver and its scheduled execution policy.</summary>
public sealed record GXBackgroundTaskRegistration(
    string TaskId, Func<IServiceProvider, IGXBackgroundTask> Resolve, bool Scheduled = false);

/// <summary>Serializes background executions and retains their status across HTTP requests.</summary>
/// <remarks>Initializes a new instance of <see cref="GXBackgroundTaskRunner"/>.</remarks>
/// <param name="serviceProvider">The service provider.</param>
/// <param name="dbFactory">The db factory.</param>
/// <param name="notificationCache">The notification cache.</param>
/// <param name="tasks">The tasks.</param>
/// <param name="logger">The logger.</param>
/// <param name="clock">The clock.</param>
public sealed class GXBackgroundTaskRunner(IServiceProvider serviceProvider,
    IDbConnectionFactory dbFactory, NotificationCache notificationCache,
    IEnumerable<GXBackgroundTaskRegistration> tasks,
    ILogger<GXBackgroundTaskRunner> logger, TimeProvider? clock = null) : BackgroundService
{
    sealed class Run
    {
        public required GXBackgroundTaskStatus Status;
        public required ClaimsPrincipal User;
        public required GXBackgroundTaskRegistration Registration;
        public CancellationToken Cancellation;
        public Exception? Failure;
        public string[] Recipients = [];
        public readonly TaskCompletionSource Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private readonly IDbConnectionFactory _dbFactory = dbFactory;
    private readonly NotificationCache _notificationCache = notificationCache;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IReadOnlyDictionary<string, GXBackgroundTaskRegistration> _tasks = tasks.ToDictionary(t => t.TaskId, StringComparer.Ordinal);
    private readonly ILogger<GXBackgroundTaskRunner> _logger = logger;
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly Channel<Run> _queue = Channel.CreateUnbounded<Run>(new() { SingleReader = true });
    private readonly Dictionary<Guid, Run> _runs = [];
    private readonly Queue<Guid> _completed = new();
    private readonly Lock _gate = new();
    bool _accepting;

    /// <summary>Tasks selected by the periodic cron trigger.</summary>
    public IEnumerable<string> ScheduledTaskIds => _tasks.Values.Where(t => t.Scheduled).Select(t => t.TaskId);

    /// <inheritdoc />
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _accepting = true;
        }
        return base.StartAsync(cancellationToken);
    }

    /// <summary>Start work detached from the initiating request's lifetime.</summary>
    public async Task<Guid> StartAsync(string taskId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return (await EnqueueAsync(taskId, user, CancellationToken.None, cancellationToken)).Status.RunId;
    }

    /// <summary>Enqueue work, await it, and return its run ID.</summary>
    public async Task<Guid> RunAsync(string taskId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var run = await EnqueueAsync(taskId, user, cancellationToken, cancellationToken);
        await AwaitAsync(run, cancellationToken);
        return run.Status.RunId;
    }

    async Task<Run> EnqueueAsync(string taskId, ClaimsPrincipal user, CancellationToken cancellationToken,
        CancellationToken enqueueCancellation)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
        {
            throw new ArgumentException("Unknown background task.", nameof(taskId));
        }
        // Clone identities, not just the principal wrapper, so caller changes cannot affect queued work.
        var captured = new ClaimsPrincipal(user.Identities.Select(i => i.Clone()));
        var owner = captured.FindFirstValue(ClaimTypes.NameIdentifier);
        if (captured.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(owner))
        {
            throw new UnauthorizedAccessException();
        }
        lock (_gate)
        {
            if (!_accepting) {
                throw new InvalidOperationException("Background tasks are not accepting work.");
            }
        }
        // Capture all recipients before publishing queued work. A queued run can be
        // cancelled by shutdown without ever entering the execution/user scope.
        List<string> administrators;
        await using (var connection = await _dbFactory.GetConnectionAsync(enqueueCancellation))
        {
            var admin = await ServerSettings.GetDefaultAdminUserAsync(connection)
                ?? throw new InvalidOperationException("No administrator is available for background task notifications.");
            administrators = await _serviceProvider.RunAsUserAsync(admin, async services =>
                await services.GetRequiredService<IUserRepository>().GetUserIdsInRoleAsync([GXRoles.Admin]), enqueueCancellation);
        }
        enqueueCancellation.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (!_accepting)
            {
                throw new InvalidOperationException("Background tasks are not accepting work.");
            }
            Prune();
            var run = new Run
            {
                Status = new() { RunId = Guid.NewGuid(), TaskId = taskId, OwnerId = owner, QueuedAt = _clock.GetUtcNow(), State = BackgroundTaskState.Queued },
                User = captured,
                Registration = task,
                Recipients = [.. administrators.Append(owner).Distinct(StringComparer.Ordinal)],
                Cancellation = cancellationToken
            };
            _runs.Add(run.Status.RunId, run);
            if (!_queue.Writer.TryWrite(run))
            {
                _runs.Remove(run.Status.RunId);
                throw new InvalidOperationException("Background tasks are stopping.");
            }
            return run;
        }
    }

    /// <summary>Performs the get task status operation.</summary>
    /// <param name="runId">The run id.</param>
    public GXBackgroundTaskStatus? GetTaskStatus(Guid runId)
    {
        lock (_gate)
        {
            Prune();
            return _runs.GetValueOrDefault(runId)?.Status;
        }
    }

    /// <summary>Await an existing retained run and propagate its execution failure.</summary>
    public Task WaitAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        Run run;
        lock (_gate)
        {
            run = _runs.GetValueOrDefault(runId) ?? throw new KeyNotFoundException("Unknown background execution.");
        }

        return AwaitAsync(run, cancellationToken);
    }

    static async Task AwaitAsync(Run run, CancellationToken cancellationToken)
    {
        await run.Completion.Task.WaitAsync(cancellationToken);
        if (run.Failure != null)
        {
            ExceptionDispatchInfo.Capture(run.Failure).Throw();
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var run in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, run.Cancellation);
                try
                {
                    cancellation.Token.ThrowIfCancellationRequested();
        
                    await _serviceProvider.RunAsUserAsync(run.User, async services =>
                    {
                        cancellation.Token.ThrowIfCancellationRequested();
                        lock (_gate)
                        {
                            if (run.Status.FinishedAt != null) {
                                return;
                            }
                            run.Status = run.Status with { State = BackgroundTaskState.Running, StartedAt = _clock.GetUtcNow() };
                            Notify(run, GXCronNotifications.Started);
                        }
                        var task = run.Registration.Resolve(services);
                        if (task.TaskId != run.Status.TaskId)
                        {
                            throw new InvalidOperationException("Background task registration does not match its handler.");
                        }

                        await task.RunAsync(cancellation.Token);
                        cancellation.Token.ThrowIfCancellationRequested();
                    }, cancellation.Token);
        
                    Complete(run, BackgroundTaskState.Succeeded);
                }
                catch (OperationCanceledException error)when (cancellation.IsCancellationRequested)
                {
                    Complete(run, BackgroundTaskState.Cancelled, failure: error);
                }
                catch (Exception error)
                {
                    _logger.LogError(error, "Background task {TaskId}, run {RunId}, failed", run.Status.TaskId, run.Status.RunId);
                    // Keep internal exception details in logs, not in the public status snapshot.
                    Complete(run, BackgroundTaskState.Failed, "Background task failed. See the server log for details.", error);
                }
                finally
                {
                    lock (_gate)
                    {
                        run.User = new ClaimsPrincipal();
                        run.Cancellation = default;
                    }
                }
            }
        }
        catch (OperationCanceledException)when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            lock (_gate)
            {
                _accepting = false;
            }
        
            while (_queue.Reader.TryRead(out var run))
            {
                Complete(run, BackgroundTaskState.Cancelled, failure: new OperationCanceledException("The server is stopping.", stoppingToken));
            }
        }
    }

    void Complete(Run run, BackgroundTaskState state, string? error = null, Exception? failure = null)
    {
        lock (_gate)
        {
            if (run.Status.FinishedAt != null)
            {
                return;
            }
            var wasQueued = run.Status.State == BackgroundTaskState.Queued;
            run.Failure = failure;
            run.Status = run.Status with { State = state, FinishedAt = _clock.GetUtcNow(), Error = error };
            // A running handler may outlive the shutdown deadline; retain its captured user
            // until its execution finally exits. Queued cancelled work needs no principal.
            if (wasQueued)
            {
                run.User = new ClaimsPrincipal();
                run.Cancellation = default;
            }
            _completed.Enqueue(run.Status.RunId);
            // Keep completed status history bounded by both age and count.
            Prune();
            Notify(run, GXCronNotifications.Completed);
        }
        // Always complete successfully; callers rethrow Failure, and detached runs cannot create unobserved task exceptions.
        run.Completion.TrySetResult();
    }

    // Called under _gate so shutdown cannot publish completion ahead of a start,
    // and each terminal transition is published once before waiters are released.
    void Notify(Run run, string notificationId)
    {
        try
        {
            var message = run.Status.State switch
            {
                BackgroundTaskState.Running => "Background task started.",
                BackgroundTaskState.Succeeded => "Background task succeeded.",
                BackgroundTaskState.Failed => "Background task failed.",
                BackgroundTaskState.Cancelled => "Background task cancelled.",
                _ => "Background task state changed."
            };
            _notificationCache.Add(run.Recipients, new GXTarget(GXConfigurationPolicies.Cron, notificationId, message)
            {
                Settings = JsonSerializer.Serialize(run.Status)
            });
        }
        catch (Exception error)
        {
            // Delivery errors cannot alter the execution's outcome or strand its waiters.
            _logger.LogError(error, "Background task {TaskId}, run {RunId}, notification failed", run.Status.TaskId, run.Status.RunId);
        }
    }

    void Prune()
    {
        var cutoff = _clock.GetUtcNow() - TimeSpan.FromHours(24);
        while (_completed.TryPeek(out var id) &&
            (_completed.Count > 1000 || _runs[id].Status.FinishedAt <= cutoff))
        {
            _completed.Dequeue();
            _runs.Remove(id);
        }
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _accepting = false;
            _queue.Writer.TryComplete();
            foreach (var run in _runs.Values.Where(r => r.Status.State == BackgroundTaskState.Queued).ToArray())
            {
                Complete(run, BackgroundTaskState.Cancelled, failure: new OperationCanceledException("The server is stopping."));
            }
        }
        try
        {
            await base.StopAsync(cancellationToken);
        }
        finally
        {
            // A handler that ignores cancellation may outlive the host's shutdown deadline.
            // Finalize its public status now; a later handler exit must not overwrite it.
            lock (_gate)
            {
                foreach (var run in _runs.Values.Where(r => r.Status.FinishedAt == null).ToArray())
                {
                    Complete(run, BackgroundTaskState.Cancelled, failure: new OperationCanceledException("The server is stopping."));
                }
            }
        }
    }
}
