using System.Data.Common;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Client.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Repository;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;

const string cs = "Data Source=raw-notifications;Mode=Memory;Cache=Shared;Foreign Keys=False";
await using var keeper = new SqliteConnection(cs);
await keeper.OpenAsync();
var factory = new Factory(cs);
var notifications = new NotificationCache();
var receiver = Guid.NewGuid(); var processor = Guid.NewGuid(); var other = Guid.NewGuid();
await using (var db = await factory.GetConnectionAsync())
{
    var schema = new GXSchemaManager(db);
    schema.CreateTable(typeof(Gurux.DLMS.AMI.Shared.DTOs.Authentication.GXUser), false, false);
    schema.CreateTable(typeof(GXAgent), false, false);
    schema.CreateTable(typeof(GXRawNotification), false, false);
    foreach (var type in new[] { typeof(GXUserGroup), typeof(GXUserGroupUser), typeof(GXAgentGroup), typeof(GXAgentGroupAgent), typeof(GXUserGroupAgentGroup),
        typeof(GXRole), typeof(GXUserRole), typeof(GXUserClaim), typeof(GXRoleClaim), typeof(GXPermission), typeof(GXUserPermission), typeof(GXRolePermission), typeof(GXPolicy), typeof(GXPolicyRequirement) })
        schema.CreateTable(type, false, false);
    using (var command = keeper.CreateCommand())
    {
        command.CommandText = "INSERT INTO GXUser (Id, UserName) VALUES ('admin','Admin'), ('alice','Alice'), ('outsider','Outsider'), ('no-view','No View')";
        await command.ExecuteNonQueryAsync();
    }
    var viewPolicy = new GXPolicy(GXRawNotificationPolicies.View);
    await db.InsertAsync(GXInsertArgs.Insert(viewPolicy));
    await db.InsertAsync(GXInsertArgs.Insert(new GXPolicyRequirement { Id = Guid.NewGuid(), Policy = viewPolicy, Type = PolicyRequirementType.Role, Value = "raw-parser" }));
    var parserRole = new GXRole("raw-parser");
    await db.InsertAsync(GXInsertArgs.Insert(parserRole));
    foreach (var name in new[] { "alice", "outsider" })
        await db.InsertAsync(GXInsertArgs.Insert(new GXUserRole { UserId = name, RoleId = parserRole.Id }));
    foreach (var id in new[] { receiver, processor, other })
        await db.InsertAsync(GXInsertArgs.Insert(new GXAgent { Id = id, Name = id.ToString(), Active = true, Creator = new Gurux.DLMS.AMI.Shared.DTOs.Authentication.GXUser { Id = "admin" } }));
}
RawNotificationRepository Repository(string name = "admin", bool admin = true, IAuthorizationService? auth = null)
{
    using var services = new ServiceCollection().AddSingleton(notifications).BuildServiceProvider();
    return ActivatorUtilities.CreateInstance<RawNotificationRepository>(services, factory,
        new HttpContextAccessor { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, name),
                new Claim(ClaimTypes.Role, admin ? "admin" : "user")], "test")) } }, auth ?? new Allow());
}
var repo = Repository();
var connection = Guid.NewGuid();
GXRawNotification Chunk(long sequence, bool closed = false) => new() { Id = Guid.NewGuid(), Agent = new GXAgent { Id = receiver },
    ConnectionId = connection, SequenceNumber = sequence, Received = DateTimeOffset.UtcNow, Value = [(byte)sequence], ConnectionClosed = closed };
var first = Chunk(0);
await repo.AddAsync([first]);
await repo.AddAsync([first]);
var pending = await repo.ReadAsync(first.Id);
Check(pending.Agent?.Id == receiver && pending.Processor == null,
    "A pending notification must retain its receiving agent and have no processor.");
Check((await repo.ListAsync()).Count() == 1, "Delivery retry must not duplicate rows.");
var conflicting = Chunk(0);
await Reject(() => repo.AddAsync([conflicting]));
var original = first.Value;
first.Value = [99];
await Reject(() => repo.AddAsync([first]));
first.Value = original;
var claims = await Task.WhenAll(Repository().GetNextAsync(processor, TimeSpan.FromMinutes(1)), Repository().GetNextAsync(other, TimeSpan.FromMinutes(1)));
Check(claims.Count(c => c.Any()) == 1, "Only one processor may claim a connection.");
var reserved = claims.Single(c => c.Any()).Single();
var owner = reserved.Processor!.Id; var token = reserved.LeaseToken!.Value;
var processing = await repo.ReadAsync(first.Id);
Check(processing.Agent?.Id == receiver && processing.Processor?.Id == owner && receiver != owner,
    "Joined receiver and processor must be read into their separate navigation properties.");
await Reject(() => repo.DoneAsync(owner, token));
await Reject(() => repo.DoneAsync(owner == processor ? other : processor, token));
await Reject(() => repo.DeleteAsync([first.Id]));
await repo.AddAsync([Chunk(1, true)]);
await Reject(() => repo.DoneAsync(owner, token));
var chunks = (await repo.GetLeasedAsync(owner, token)).ToArray();
Check(chunks.Select(c => c.SequenceNumber).SequenceEqual(new long[] { 0, 1 }), "Chunks must be returned in sequence order.");
Check(chunks.All(c => c.LeaseToken == token && c.Attempts == 1), "New chunks must join the existing lease.");
var extended = await repo.RenewLeaseAsync(owner, token, TimeSpan.FromMinutes(2));
Check(await repo.RenewLeaseAsync(owner, token, TimeSpan.FromSeconds(1)) >= extended, "Renewal must not shorten the existing lease.");
await repo.FailAsync(owner, token, "parser failed", DateTimeOffset.UtcNow.AddMinutes(-1));
await Reject(() => repo.RenewLeaseAsync(owner, token, TimeSpan.FromMinutes(1)));
var retried = (await repo.GetNextAsync(processor, TimeSpan.FromMinutes(1))).ToArray();
Check(retried.Length == 2 && retried.All(c => c.Attempts == 2 && c.LeaseToken != token), "Retry must reserve the entire connection with a new token.");
await repo.DoneAsync(processor, retried[0].LeaseToken!.Value);
Check(!(await repo.GetNextAsync(other, TimeSpan.FromMinutes(1))).Any(), "Completed data must not be claimed.");
Check((await repo.ListAsync()).All(c => c.Status == RawNotificationStatus.Completed && c.Processed != null), "Completion must persist.");
var response = new ListRawNotificationsResponse();
await repo.ListAsync(new ListRawNotifications { Count = 1, AllUsers = true }, response);
Check(response.Count == 2 && response.RawNotifications!.Count() == 1, "Paging count must include all matching rows.");
await repo.ClearAsync(DateTimeOffset.UtcNow.AddMinutes(-1));
Check((await repo.ListAsync()).Count() == 2, "Retention must preserve newer completions.");
await repo.ClearAsync(DateTimeOffset.UtcNow.AddMinutes(1));
Check(!(await repo.ListAsync()).Any(), "Retention must remove old completed connections.");
// Expired processing must include chunks that arrived after the last fetch.
connection = Guid.NewGuid();
var expiredFirst = Chunk(0);
await repo.AddAsync([expiredFirst]);
var expiring = (await repo.GetNextAsync(processor, TimeSpan.FromMinutes(1))).Single();
await repo.AddAsync([Chunk(1, true)]);
await using (var db = await factory.GetConnectionAsync())
{
    var update = new GXRawNotification { Id = expiredFirst.Id, LeaseExpires = DateTimeOffset.UtcNow.AddMinutes(-1) };
    await db.UpdateAsync(GXUpdateArgs.Update(update, n => n.LeaseExpires!));
}
await Reject(() => repo.DoneAsync(processor, expiring.LeaseToken!.Value));
await repo.RestartAsync([expiredFirst.Id]);
var restarted = (await repo.GetNextAsync(other, TimeSpan.FromMinutes(1))).ToArray();
Check(restarted.Length == 2 && restarted[0].Attempts == 2 && restarted[1].Attempts == 1,
    "Restart must include pending chunks alongside an expired lease.");
await repo.DoneAsync(other, restarted[0].LeaseToken!.Value);
await repo.DeleteAsync([expiredFirst.Id]);

// Failed connections remain blocked until their retry time, including newly ingested chunks.
connection = Guid.NewGuid();
var blockedFirst = Chunk(0);
blockedFirst.Status = RawNotificationStatus.Completed;
blockedFirst.Processor = new GXAgent { Id = other };
blockedFirst.LeaseToken = Guid.NewGuid();
await repo.AddAsync([blockedFirst]);
var sanitized = await repo.ReadAsync(blockedFirst.Id);
Check(sanitized.Status == RawNotificationStatus.Pending && sanitized.LeaseToken == null && sanitized.Processor == null,
    "Ingest must ignore caller-supplied processing state.");
var blockedLease = (await repo.GetNextAsync(processor, TimeSpan.FromMinutes(1))).Single();
await repo.FailAsync(processor, blockedLease.LeaseToken!.Value, "permanent failure");
await repo.AddAsync([Chunk(1, true)]);
Check(!(await repo.GetNextAsync(other, TimeSpan.FromMinutes(1))).Any(), "New data must not bypass a permanent failure.");
await repo.RestartAsync([blockedFirst.Id]);
var futureLease = (await repo.GetNextAsync(processor, TimeSpan.FromMinutes(1))).ToArray();
await repo.FailAsync(processor, futureLease[0].LeaseToken!.Value, "temporary failure", DateTimeOffset.UtcNow.AddDays(1));
Check(!(await repo.GetNextAsync(other, TimeSpan.FromMinutes(1))).Any(), "Future retries must not run early.");
await repo.DeleteAsync([blockedFirst.Id]);

// A rejected batch must roll back earlier inserts from that batch.
connection = Guid.NewGuid();
var batchFirst = Chunk(0);
await Reject(() => repo.AddAsync([batchFirst, Chunk(2)]));
Check(!(await repo.ListAsync()).Any(), "Invalid batches must roll back all inserts.");
await Reject(() => repo.GetNextAsync(processor, TimeSpan.Zero));
using (var canceled = new CancellationTokenSource())
{
    canceled.Cancel();
    try { await repo.GetNextAsync(processor, TimeSpan.FromMinutes(1), cancellationToken: canceled.Token); throw new Exception("Cancellation was ignored."); }
    catch (OperationCanceledException) { }
}

var controller = new RawNotificationController(repo);
Check((await controller.Post(new AddRawNotification())).Result is Microsoft.AspNetCore.Mvc.BadRequestObjectResult,
    "Missing data must produce HTTP 400.");
Check((await controller.Post(new RawNotificationDone { ProcessorId = processor, LeaseToken = Guid.NewGuid() })).Result is Microsoft.AspNetCore.Mvc.ConflictObjectResult,
    "A stale token must produce HTTP 409.");
Check((await controller.Get(Guid.NewGuid())).Result is Microsoft.AspNetCore.Mvc.NotFoundResult,
    "Missing notifications must produce HTTP 404.");
Check((await controller.Post(new ClearRawNotification())).Result is Microsoft.AspNetCore.Mvc.BadRequestObjectResult,
    "Missing retention cutoff must produce HTTP 400.");
// Group membership scopes both receivers and processors, including an AllUsers request.
await using (var db = await factory.GetConnectionAsync())
{
    var schema = new GXSchemaManager(db);
    foreach (var type in new[] { typeof(GXUserGroup), typeof(GXUserGroupUser), typeof(GXAgentGroup), typeof(GXAgentGroupAgent), typeof(GXUserGroupAgentGroup) })
        schema.CreateTable(type, false, false);
    var users = Guid.NewGuid(); var agents = Guid.NewGuid();
    await db.InsertAsync(GXInsertArgs.Insert(new GXUserGroup { Id = users, Name = "Users", Default = false, CreationTime = DateTimeOffset.UtcNow }));
    await db.InsertAsync(GXInsertArgs.Insert(new GXAgentGroup { Id = agents, Name = "Agents", Active = true, Default = false, CreationTime = DateTimeOffset.UtcNow }));
    await db.InsertAsync(GXInsertArgs.Insert(new GXUserGroupUser { UserGroupId = users, UserId = "alice" }));
    await db.InsertAsync(GXInsertArgs.Insert(new GXUserGroupUser { UserGroupId = users, UserId = "no-view" }));
    await db.InsertAsync(GXInsertArgs.Insert(new GXUserGroupAgentGroup { UserGroupId = users, AgentGroupId = agents, CreationTime = DateTimeOffset.UtcNow }));
    foreach (var id in new[] { receiver, processor })
        await db.InsertAsync(GXInsertArgs.Insert(new GXAgentGroupAgent { AgentGroupId = agents, AgentId = id, CreationTime = DateTimeOffset.UtcNow }));
}
connection = Guid.NewGuid();
var visible = Chunk(0, true);
var hidden = Chunk(0, true); hidden.Agent = new GXAgent { Id = other };
notifications.TakeAll();
await repo.AddAsync([visible, hidden]);
var insertedEvents = notifications.TakeAll();
Check(insertedEvents.Count == 1 && insertedEvents[0].UserId == "alice" && insertedEvents[0].Notification.Type == GXRawNotificationPolicies.Add
    && insertedEvents[0].Notification.Id == visible.Id.ToString(), "New notifications must be routed only to view-authorized receiving-agent group members.");
await repo.AddAsync([visible, hidden]);
Check(notifications.TakeAll().Count == 0, "Idempotent retries must not publish duplicate events.");
var alice = Repository("alice", false);
Check((await alice.ListAsync(new ListRawNotifications { AllUsers = true })).Select(n => n.Id).SequenceEqual([visible.Id]),
    "AllUsers must not bypass group access.");
await Reject(() => alice.GetNextAsync(other, TimeSpan.FromMinutes(1)));
await Reject(() => alice.GetNextAsync(processor, TimeSpan.FromMinutes(1), other));
await Reject(() => alice.DeleteAsync([hidden.Id]));
var visibleLease = (await alice.GetNextAsync(processor, TimeSpan.FromMinutes(1))).Single();
Check(visibleLease.Id == visible.Id, "Claims must be restricted to accessible receivers.");
await alice.DoneAsync(processor, visibleLease.LeaseToken!.Value);
foreach (var table in new[] { "GXUserGroupUser", "GXUserGroupAgentGroup", "GXAgentGroupAgent", "GXUserGroup", "GXAgentGroup" })
{
    using var command = keeper.CreateCommand();
    command.CommandText = $"UPDATE {table} SET Removed = '2026-01-01'";
    await command.ExecuteNonQueryAsync();
    Check(!(await alice.ListAsync()).Any(), $"Removed membership in {table} must revoke access.");
    await Reject(() => alice.GetNextAsync(processor, TimeSpan.FromMinutes(1)));
    command.CommandText = $"UPDATE {table} SET Removed = NULL";
    await command.ExecuteNonQueryAsync();
}
Check((await new RawNotificationController(alice).Get(hidden.Id)).Result is Microsoft.AspNetCore.Mvc.NotFoundResult,
    "Inaccessible notifications must not be returned by the read endpoint.");
await repo.DeleteAsync([visible.Id, hidden.Id]);
var denied = Repository(auth: new Deny());
await Reject(() => denied.ListAsync());
Check((await new RawNotificationController(denied).Post(new ListRawNotifications())).Result is Microsoft.AspNetCore.Mvc.ForbidResult,
    "Denied repository permissions must produce HTTP 403.");
// Expiration alone must allow another processor to recover the full connection.
connection = Guid.NewGuid();
var recoveryChunk = Chunk(0, true);
await repo.AddAsync([recoveryChunk]);
var abandoned = (await repo.GetNextAsync(processor, TimeSpan.FromMinutes(1))).Single();
await using (var db = await factory.GetConnectionAsync())
{
    await db.UpdateAsync(GXUpdateArgs.Update(new GXRawNotification { Id = recoveryChunk.Id,
        LeaseExpires = DateTimeOffset.UtcNow.AddMinutes(-1) }, n => n.LeaseExpires!));
}
var recovered = (await repo.GetNextAsync(other, TimeSpan.FromMinutes(1))).Single();
Check(recovered.LeaseToken != abandoned.LeaseToken && recovered.Attempts == 2, "Expired work must be recovered with a new lease.");
await Reject(() => repo.DoneAsync(processor, abandoned.LeaseToken!.Value));
await repo.DoneAsync(other, recovered.LeaseToken!.Value);
await repo.DeleteAsync([recoveryChunk.Id]);

// Successful controller flow uses the same repository contracts as direct agent calls.
connection = Guid.NewGuid();
var httpChunk = Chunk(0, true);
var addedHttp = await controller.Post(new AddRawNotification { RawNotifications = [httpChunk] });
Check(addedHttp.Value!.Ids.Single() == httpChunk.Id, "Add endpoint must return persisted identifiers.");
var nextHttp = await controller.Post(new GetNextRawNotification { ProcessorId = processor, LeaseDuration = TimeSpan.FromMinutes(1) });
var httpToken = nextHttp.Value!.RawNotifications.Single().LeaseToken!.Value;
var leasedHttp = await controller.Post(new GetLeasedRawNotifications { ProcessorId = processor, LeaseToken = httpToken });
Check(leasedHttp.Value!.RawNotifications.Single().Id == httpChunk.Id, "Leased endpoint must return connection chunks.");
Check((await controller.Post(new RenewRawNotificationLease { ProcessorId = processor, LeaseToken = httpToken,
    LeaseDuration = TimeSpan.FromMinutes(2) })).Value != null, "Renew endpoint must return the expiration.");
Check((await controller.Post(new RawNotificationDone { ProcessorId = processor, LeaseToken = httpToken })).Value != null,
    "Done endpoint must acknowledge completed processing.");
Check((await controller.Post(new ClearRawNotification { ProcessedBefore = DateTimeOffset.UtcNow.AddMinutes(1) })).Value != null,
    "Clear endpoint must acknowledge retention cleanup.");
Check(!(await repo.ListAsync()).Any(), "Controller cleanup must persist.");
Console.WriteLine("Raw notification SQLite checks passed: ingestion, rollback, concurrent claims, streaming chunks, expiry, retry, completion, retention, group authorization and controller responses.");
static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
static async Task Reject(Func<Task> action) { try { await action(); } catch (ArgumentException) { return; } catch (InvalidOperationException) { return; } catch (UnauthorizedAccessException) { return; } throw new Exception("Expected operation to be rejected."); }
sealed class Factory(string cs) : IDbConnectionFactory
{
    public async Task<GXDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default, bool notifySqlExcution = true) => new(await CreateOpenConnectionAsync(cancellationToken), null);
    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default) { var c = new SqliteConnection(cs); await c.OpenAsync(cancellationToken); return c; }
}
sealed class Allow : IAuthorizationService
{
    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements) => Task.FromResult(AuthorizationResult.Success());
    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName) => Task.FromResult(AuthorizationResult.Success());
}







sealed class Deny : IAuthorizationService
{
    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements) => Task.FromResult(AuthorizationResult.Failed());
    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName) => Task.FromResult(AuthorizationResult.Failed());
}


