using Gurux.DLMS.AMI.Server.Services;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.Agent;
using Gurux.DLMS.AMI.Shared.DTOs.History;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Gurux.DLMS.AMI.Client.Shared;
using Gurux.DLMS.AMI.Client.Shared.KeyManagement;
using Gurux.DLMS.AMI.Data;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Server.Module;
using Gurux.DLMS.AMI.Server.Repository;
using Gurux.DLMS.AMI.Server.Services.KeyManagement;
using Gurux.DLMS.AMI.Services;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Common.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ModuleRuntime.Tests;

[TestClass]
public sealed class DevicePasswordTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task OperationalPasswordUsesDeviceAuthenticationInsteadOfTemplateAuthentication(bool hasDeviceSettings)
    {
        byte[] password = [49, 50, 51, 52];
        var device = new GXDevice
        {
            Settings = hasDeviceSettings ? JsonSerializer.Serialize(new GXDLMSSettings { Authentication = 1 }) : null,
            Template = new GXDeviceTemplate { Settings = JsonSerializer.Serialize(new GXDLMSSettings { Authentication = hasDeviceSettings ? (byte)0 : (byte)1 }) }
        };
        var key = new GXKeyManagementKey { KeyType = KeyManagementType.LLSPassword, Key = password };
        GXKeyPolicy.InitializeLegacy(key);
        var actor = new ClaimsPrincipal(new ClaimsIdentity([new Claim("Permission", GXKeyPermissions.Use)], "test"));
        await GXDeviceSettingsKeys.PopulateAsync(actor, device, [new GXKeyManagement { Keys = [key.ToMetadata()] }],
            false, (_, _, _) => Task.FromResult(password));
        CollectionAssert.AreEqual(password, JsonSerializer.Deserialize<GXDLMSSettings>(device.Settings!)!.Password);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task CreatedDevicePasswordIsEncryptedAndUsableByAgent(bool lateBinding)
    {
        var file = Path.Combine(Path.GetTempPath(), $"ami-device-password-{Guid.NewGuid():N}.db");
        try
        {
            var factory = new SqlConnectionFactory(DatabaseType.SqLite, $"Data Source={file};Foreign Keys=False;Pooling=False");
            await using var db = await factory.GetConnectionAsync();
            var server = typeof(DeviceRepository).Assembly.GetType("Gurux.DLMS.AMI.Server.ServerSettings")!;
            await (Task)server.GetMethod("InitializeDatabaseAsync", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, [db, NullLogger<GXConfiguration>.Instance])!;
            await db.InsertAsync(GXInsertArgs.Insert(new GXUser { Id = "password-owner", UserName = "owner" }));
            var accessor = new HttpContextAccessor();
            var services = new ServiceCollection().AddLogging().AddAuthentication().Services;
            services.AddSingleton<IDbConnectionFactory>(factory).AddSingleton<IHttpContextAccessor>(accessor)
                .AddSingleton<NotificationCache>().AddSingleton<IAuthorizationService, Allow>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite($"Data Source={file};Foreign Keys=False;Pooling=False"));
            services.AddIdentityCore<ApplicationUser>().AddRoles<ApplicationRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddSignInManager();
            server.GetMethod("AddRepositories", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [services]);
            var crypto = new GXCryptoService(Options.Create(new Gurux.DLMS.AMI.Server.Internal.CryptoOptions { Key = new string('0', 64) }));
            services.AddSingleton<IGXAmiCryptoService>(crypto);
            services.AddSingleton(DispatchProxy.Create<IWorkflowHandler, Workflow>());
            services.AddSingleton(DispatchProxy.Create<ITaskLateBindHandler, Workflow>());
            services.AddSingleton(DispatchProxy.Create<IGXModuleService, Workflow>());
            await using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var actor = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, "password-owner"), new Claim(ClaimTypes.Role, GXRoles.Admin),
                new Claim("Permission", GXRoles.Admin)], "test"));
            accessor.HttpContext = new DefaultHttpContext { User = actor, RequestServices = scope.ServiceProvider };
            await scope.ServiceProvider.GetRequiredService<ILogTypeRepository>().EnsureDefaultsAsync();
            var owner = new GXUser { Id = "password-owner" };
            var userGroup = new GXUserGroup("owners") { Creator = owner, Users = [owner] };
            await scope.ServiceProvider.GetRequiredService<IUserGroupRepository>().UpsertAsync([userGroup]);
            var group = new GXDeviceGroup("meters") { Default = true, Creator = owner, UserGroups = [userGroup] };
            await scope.ServiceProvider.GetRequiredService<IDeviceGroupRepository>().UpsertAsync([group]);
            var keyGroup = new GXKeyManagementGroup { Name = "device keys", Default = true, Creator = owner, UserGroups = [userGroup] };
            await scope.ServiceProvider.GetRequiredService<IKeyManagementGroupRepository>().UpsertAsync([keyGroup]);
            var template = new GXDeviceTemplate
            {
                Name = "default", Type = "meter", Creator = owner,
                Settings = JsonSerializer.Serialize(new GXDLMSSettings { Authentication = 0 })
            };
            await db.InsertAsync(GXInsertArgs.Insert(template));
            byte[] password = [49, 50, 51, 52];
            var device = new GXDevice("LLS meter")
            {
                Template = template, DeviceGroups = [group], MediaType = "Gurux.Net.GXNet",
                Settings = JsonSerializer.Serialize(new GXDLMSSettings { Authentication = 1, Password = password })
            };
            var devices = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
            await devices.UpsertAsync([device], CancellationToken.None, null, lateBinding, null);
            var stored = await db.SingleOrDefaultAsync<GXKeyManagementKey>(GXSelectArgs.SelectAll<GXKeyManagementKey>());
            Assert.IsNotNull(stored);
            Assert.IsNotNull(stored.Key);
            Assert.IsFalse(password.SequenceEqual(stored.Key), "Stored credentials must be encrypted.");
            CollectionAssert.AreEqual(password, await crypto.DecryptAsync(stored.Key));
            Assert.AreEqual(KeyState.Active, stored.State, "An installed device password must be immediately usable.");
            var keys = scope.ServiceProvider.GetRequiredService<IKeyManagementRepository>();
            var managementRow = await db.SingleOrDefaultAsync<GXKeyManagement>(GXSelectArgs.Select<GXKeyManagement>(m => m.Id, m => m.Device == device));
            Assert.IsNotNull(managementRow);
            var management = await keys.ReadAsync(managementRow.Id);
            Assert.IsNull(management.Keys!.Single().Key, "Metadata reads must not expose credentials.");
            var listed = (await keys.ListAsync(new Gurux.DLMS.AMI.Shared.Rest.ListKeyManagements
            {
                Filter = new GXKeyManagement { Device = new GXDevice { Id = device.Id } }, Select = ["KeyManagementKey"]
            }, null, CancellationToken.None)).ToArray();
            Assert.HasCount(1, listed, "The device key management must be found.");
            Assert.HasCount(1, listed[0].Keys!, "The device password metadata must be loaded.");
            Assert.AreEqual(KeyManagementType.LLSPassword, listed[0].Keys![0].KeyType);
            await using var runtime = new GXModuleRuntime(provider, new ConfigurationBuilder().Build(), NullLogger<GXModuleRuntime>.Instance);
            var acquisition = new GXKeyAcquisitionService(factory, keys, crypto, runtime);
            var settings = new GXDeviceSettingsService(devices, keys, acquisition);
            var response = await settings.ReadAsync(actor, device.Id, false, CancellationToken.None);
            CollectionAssert.AreEqual(password, JsonSerializer.Deserialize<GXDLMSSettings>(response.Settings!)!.Password);
            // Even client-supplied Active metadata must not activate ordinary imports.
            var ordinaryKey = new GXKeyManagementKey { KeyType = KeyManagementType.LLSPassword, Key = password };
            GXKeyPolicy.InitializeLegacy(ordinaryKey);
            var imported = new GXKeyManagement
            {
                Name = "pending import", KeyManagementGroups = [keyGroup], Keys = [ordinaryKey]
            };
            await keys.UpsertAsync([imported], null, CancellationToken.None);
            Assert.AreEqual(KeyState.PreActive, ordinaryKey.State);
            Assert.IsNull(ordinaryKey.ActivationDate);
            var redacted = await devices.ReadAsync(device.Id);
            Assert.IsNull(JsonSerializer.Deserialize<GXDLMSSettings>(redacted.Settings!)!.Password);

            await db.InsertAsync(GXInsertArgs.Insert(new GXObjectTemplate("association")
            {
                DeviceTemplate = template, ObjectType = 15, LogicalName = "0.0.40.0.0.255"
            }));
            var historyType = await db.SingleOrDefaultAsync<GXChangeHistoryType>(
                GXSelectArgs.SelectAll<GXChangeHistoryType>(t => t.Name == "KeyManagement"));
            historyType!.RecordHistory = true;
            await db.UpdateAsync(GXUpdateArgs.Update(historyType, t => t.RecordHistory));
            var changes = new GXPasswordChangeService(factory, devices, keys, crypto, new Allow(), scope.ServiceProvider);
            byte[] nextPassword = [110, 101, 120, 116, 45, 112, 97, 115, 115];
            var operation = await changes.CreateAsync(actor, new GXPasswordChangeRequest
            {
                ManagementId = management.Id, DeviceId = device.Id, NewPassword = nextPassword,
                AssociationLogicalName = "0.0.40.0.0.255"
            }, CancellationToken.None);
            Assert.AreEqual("Queued", operation.State);
            var noUse = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "password-owner")], "test"));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => changes.WorkAsync(noUse, operation.Id,
                new GXPasswordChangeClaim { TaskId = operation.TaskId, AgentId = Guid.NewGuid(), Start = DateTimeOffset.UtcNow }, CancellationToken.None));
            var duplicate = new GXPasswordChangeRequest
            {
                ManagementId = management.Id, DeviceId = device.Id, NewPassword = [100, 117, 112],
                AssociationLogicalName = "0.0.40.0.0.255"
            };
            Exception? duplicateError = null;
            try
            {
                await changes.CreateAsync(actor, duplicate, CancellationToken.None);
            }
            catch (Exception error)
            {
                duplicateError = error;
            }
            Assert.IsNotNull(duplicateError, "A second pending operation must be rejected atomically.");
            Assert.HasCount(1, await db.SelectAsync<GXPasswordChangeRecord>(GXSelectArgs.SelectAll<GXPasswordChangeRecord>()));
            Assert.IsFalse(JsonSerializer.Serialize(operation).Contains(Convert.ToBase64String(nextPassword)));
            var beforeWrite = await settings.ReadAsync(actor, device.Id, false, CancellationToken.None);
            CollectionAssert.AreEqual(password, JsonSerializer.Deserialize<GXDLMSSettings>(beforeWrite.Settings!)!.Password);
            var agent = new GXAgent { Id = Guid.NewGuid(), Name = "password agent", Creator = owner, Enabled = true };
            await db.InsertAsync(GXInsertArgs.Insert(agent));
            var agentGroup = new GXAgentGroup { Id = Guid.NewGuid(), Name = "password agents", Creator = owner };
            await db.InsertAsync(GXInsertArgs.Insert(agentGroup));
            await db.InsertAsync(GXInsertArgs.Insert(new GXAgentGroupAgent { AgentId = agent.Id, AgentGroupId = agentGroup.Id, CreationTime = DateTime.Now }));
            await db.InsertAsync(GXInsertArgs.Insert(new GXUserGroupAgentGroup { UserGroupId = userGroup.Id, AgentGroupId = agentGroup.Id, CreationTime = DateTime.Now }));
            await db.InsertAsync(GXInsertArgs.Insert(new GXAgentGroupDeviceGroup { AgentGroupId = agentGroup.Id, DeviceGroupId = group.Id, CreationTime = DateTime.Now }));
            var eligible = await db.SelectAsync<GXTask>(Gurux.DLMS.AMI.Server.Internal.GXQuery.GetNextTasks(t => t.Id,
                "password-owner", agent.Id, device.Id));
            Assert.IsTrue(eligible.Any(t => t.Id == operation.TaskId), "The ordinary agent dispatcher must find reference-only password tasks.");
            var dispatched = await scope.ServiceProvider.GetRequiredService<ITaskRepository>().ReadAsync(operation.TaskId);
            StringAssert.Contains(dispatched.Data!, operation.Id.ToString());
            var task = await db.SingleOrDefaultAsync<GXTask>(GXSelectArgs.SelectAll<GXTask>(t => t.Id == operation.TaskId));
            Assert.IsNotNull(task);
            Assert.IsFalse(task.Data!.Contains(Convert.ToBase64String(nextPassword)));
            task.Start = DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            task.OperatingAgent = agent;
            await db.UpdateAsync(GXUpdateArgs.Update(task, t => new { t.Start, t.OperatingAgent }));
            var claim = new GXPasswordChangeClaim { TaskId = task.Id, AgentId = agent.Id, Start = task.Start.Value };
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => changes.WorkAsync(actor, operation.Id,
                new GXPasswordChangeClaim { TaskId = task.Id, AgentId = agent.Id, Start = task.Start.Value.AddSeconds(-1) }, CancellationToken.None));
            var foreignAgentUser = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "foreign-user"),
                new Claim("Permission", GXRoles.Admin)], "test"));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => changes.WorkAsync(foreignAgentUser, operation.Id, claim, CancellationToken.None));
            var savedMedia = device.MediaSettings;
            device.MediaSettings = "changed connection profile";
            await db.UpdateAsync(GXUpdateArgs.Update(device, d => d.MediaSettings!));
            await Assert.ThrowsAsync<InvalidOperationException>(() => changes.WorkAsync(actor, operation.Id, claim, CancellationToken.None));
            device.MediaSettings = savedMedia;
            await db.UpdateAsync(GXUpdateArgs.Update(device, d => d.MediaSettings!));
            await Assert.ThrowsAsync<InvalidOperationException>(() => changes.ConfirmAsync(actor, operation.Id, claim, CancellationToken.None));
            using (var work = await changes.WorkAsync(actor, operation.Id, claim, CancellationToken.None))
            {
                CollectionAssert.AreEqual(password, work.CurrentPassword);
                CollectionAssert.AreEqual(nextPassword, work.NewPassword);
            }
            await changes.MarkWritingAsync(actor, operation.Id, claim, CancellationToken.None);
            await Assert.ThrowsAsync<InvalidOperationException>(() => changes.CancelAsync(actor, operation.Id, CancellationToken.None));
            await Assert.ThrowsAsync<InvalidOperationException>(() => changes.EnsureOrdinaryConnectionAllowedAsync(device.Id, CancellationToken.None));
            var stillOld = await settings.ReadAsync(actor, device.Id, false, CancellationToken.None);
            CollectionAssert.AreEqual(password, JsonSerializer.Deserialize<GXDLMSSettings>(stillOld.Settings!)!.Password);
            // An acknowledgement outage can outlast the old key's permitted protection interval.
            // Recovery must probe the new credential without releasing the now-expired old password.
            var expiredSource = await db.SingleOrDefaultAsync<GXKeyManagementKey>(GXSelectArgs.SelectAll<GXKeyManagementKey>(k => k.Id == operation.SourceKeyId));
            expiredSource!.ProtectStopDate = DateTimeOffset.UtcNow.AddSeconds(-1);
            await db.UpdateAsync(GXUpdateArgs.Update(expiredSource, k => k.ProtectStopDate!));
            using (var resumed = await changes.WorkAsync(actor, operation.Id, claim, CancellationToken.None))
            {
                Assert.AreEqual(0, resumed.CurrentPassword.Length, "Expired old credentials must not be released for a fallback connection.");
                CollectionAssert.AreEqual(nextPassword, resumed.NewPassword);
            }
            await changes.ConfirmAsync(actor, operation.Id, claim, CancellationToken.None);
            await changes.ConfirmAsync(actor, operation.Id, claim, CancellationToken.None);
            await changes.EnsureOrdinaryConnectionAllowedAsync(device.Id, CancellationToken.None);
            var afterWrite = await settings.ReadAsync(actor, device.Id, false, CancellationToken.None);
            CollectionAssert.AreEqual(nextPassword, JsonSerializer.Deserialize<GXDLMSSettings>(afterWrite.Settings!)!.Password);
            var archived = await db.SingleOrDefaultAsync<GXKeyManagementKey>(GXSelectArgs.SelectAll<GXKeyManagementKey>(k => k.Id == operation.SourceKeyId));
            Assert.AreEqual(KeyState.Deactivated, archived!.State);
            CollectionAssert.AreEqual(password, await crypto.DecryptAsync(archived.Key!));
            var noRecovery = new GXPasswordChangeService(factory, devices, keys, crypto, new Allow(GXKeyPermissions.Recovery), scope.ServiceProvider);
            await Assert.ThrowsAsync<Gurux.DLMS.AMI.Shared.GXAmiForbiddenException>(() => noRecovery.CreateAsync(actor, new GXPasswordChangeRequest
            {
                ManagementId = management.Id, DeviceId = device.Id, RestoreChangeId = operation.Id,
                AssociationLogicalName = "0.0.40.0.0.255"
            }, CancellationToken.None));
            var restore = await changes.CreateAsync(actor, new GXPasswordChangeRequest
            {
                ManagementId = management.Id, DeviceId = device.Id, RestoreChangeId = operation.Id,
                AssociationLogicalName = "0.0.40.0.0.255"
            }, CancellationToken.None);
            Assert.AreNotEqual(operation.SourceKeyId, restore.TargetKeyId, "Recovery must create a fresh identity.");
            var restored = await db.SingleOrDefaultAsync<GXKeyManagementKey>(GXSelectArgs.SelectAll<GXKeyManagementKey>(k => k.Id == restore.TargetKeyId));
            Assert.AreEqual(KeyState.PreActive, restored!.State);
            CollectionAssert.AreEqual(password, await crypto.DecryptAsync(restored.Key!));
            var restoreTask = await db.SingleOrDefaultAsync<GXTask>(GXSelectArgs.SelectAll<GXTask>(t => t.Id == restore.TaskId));
            restoreTask!.Start = DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            restoreTask.OperatingAgent = agent;
            await db.UpdateAsync(GXUpdateArgs.Update(restoreTask, t => new { t.Start, t.OperatingAgent }));
            var restoreClaim = new GXPasswordChangeClaim { TaskId = restoreTask.Id, AgentId = agent.Id, Start = restoreTask.Start.Value };
            await changes.MarkWritingAsync(actor, restore.Id, restoreClaim, CancellationToken.None);
            await changes.ConfirmAsync(actor, restore.Id, restoreClaim, CancellationToken.None);
            var recoveredDevice = await settings.ReadAsync(actor, device.Id, false, CancellationToken.None);
            CollectionAssert.AreEqual(password, JsonSerializer.Deserialize<GXDLMSSettings>(recoveredDevice.Settings!)!.Password);
            var original = await db.SingleOrDefaultAsync<GXKeyManagementKey>(GXSelectArgs.SelectAll<GXKeyManagementKey>(k => k.Id == operation.SourceKeyId));
            Assert.AreEqual(KeyState.Deactivated, original!.State, "Recovery must not reactivate archived identities.");
            var cancelled = await changes.CreateAsync(actor, new GXPasswordChangeRequest
            {
                ManagementId = management.Id, DeviceId = device.Id, NewPassword = nextPassword,
                AssociationLogicalName = "0.0.40.0.0.255"
            }, CancellationToken.None);
            await changes.CancelAsync(actor, cancelled.Id, CancellationToken.None);
            var discarded = await db.SingleOrDefaultAsync<GXKeyManagementKey>(GXSelectArgs.SelectAll<GXKeyManagementKey>(k => k.Id == cancelled.TargetKeyId));
            Assert.AreEqual(KeyState.Destroyed, discarded!.State);
            Assert.IsNull(discarded.Key, "Cancelled pending material must not remain in storage.");
            Assert.HasCount(0, await db.SelectAsync<GXPasswordChangeLock>(GXSelectArgs.SelectAll<GXPasswordChangeLock>()));
            var entries = await db.SelectAsync<GXChangeHistoryItem>(GXSelectArgs.SelectAll<GXChangeHistoryItem>(h => h.EntityId == management.Id.ToString()));
            Assert.IsTrue(entries.Count >= 2);
            foreach (var entry in entries)
            {
                foreach (var json in new[] { entry.BeforeJson, entry.AfterJson })
                {
                    if (json != null)
                    {
                        Assert.IsFalse(json.Contains(Convert.ToBase64String(nextPassword)));
                        Assert.IsFalse(json.Contains(Convert.ToBase64String(password)));
                    }
                }
            }
        }
        finally
        {
            File.Delete(file);
        }
    }

    private sealed class Allow(string? denied = null) : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
            => Task.FromResult(AuthorizationResult.Success());
        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
            => Task.FromResult(policyName == denied ? AuthorizationResult.Failed() : AuthorizationResult.Success());
    }

    public class Workflow : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args)
            => method!.ReturnType == typeof(void) ? null : throw new NotSupportedException(method.Name);
    }
}