using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Shared;
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Model;
using Microsoft.Data.Sqlite;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Gurux.DLMS.AMI.Module;
using Gurux.DLMS.AMI.Server.Module;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ModuleRuntime.Tests;

[TestClass]
public sealed class TypedSettingsRuntimeTests
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed class Model
    {
        [Required]
        public string? Name { get; set; }
        [Range(1, 10)]
        public int Count { get; set; } = 1;
    }

    private sealed class SettingsModule(string id) : GXAmiModuleBase
    {
        public override string Id => id;
        public override string Name => id;
        public override string Description => "Typed settings integration test";
        public IGXAmiModuleSettingsSnapshot? Snapshot { get; private set; }
        public int Changes { get; private set; }
        public override void Configure(AmiModuleBuilder builder)
        {
            builder.AddSettings<Model>(1, new Dictionary<int, Func<JsonObject, JsonObject>>
            {
                [0] = value => { value["Name"] = value["oldName"]?.GetValue<string>(); value.Remove("oldName"); return value; }
            });
        }
        public override Task StartAsync(AmiModuleContext context, CancellationToken cancellationToken)
        {
            Snapshot = context.Settings;
            return Task.CompletedTask;
        }
        public override Task SettingsChangedAsync(AmiModuleContext context, CancellationToken cancellationToken)
        {
            ++Changes;
            return Task.CompletedTask;
        }
    }

    private sealed class DatabaseFactory(string path) : IDbConnectionFactory
    {
        public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = new SqliteConnection($"Data Source={path};Pooling=False;Foreign Keys=False");
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        public async Task<GXDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default, bool notifySqlExcution = true)
            => new(await CreateOpenConnectionAsync(cancellationToken), null);
    }

    [TestMethod]
    public async Task StrictSettingsModelReadsConfigurationWithoutHostMetadata()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await using var runtime = new GXModuleRuntime(provider, new ConfigurationBuilder().Build(), NullLogger<GXModuleRuntime>.Instance);
        var module = new SettingsModule("typed-strict");
        await runtime.ActivateAsync(module, typeof(SettingsModule).Assembly.Location, "1.0.0", "{\"$schemaVersion\":1,\"Name\":\"meter\",\"$state\":{\"Cursor\":9},\"$resources\":{\"Id\":1}}");
        Assert.AreEqual("meter", module.Snapshot!.Read<Model>().Name);
        await runtime.UpdateSettingsAsync(module.Id, "{\"$schemaVersion\":1,\"Name\":\"updated\"}");
        Assert.AreEqual("updated", module.Snapshot.Read<Model>().Name);
        var stored = JsonNode.Parse(module.Snapshot.Value!)!;
        Assert.AreEqual(1, stored["$schemaVersion"]!.GetValue<int>());
        Assert.AreEqual(9, stored["$state"]!["Cursor"]!.GetValue<int>());
        Assert.AreEqual(1, stored["$resources"]!["Id"]!.GetValue<int>());
    }
    [TestMethod]
    public async Task PersistedMigrationIsAtomicAndPreservesDurableState()
    {
        string path = Path.Combine(Path.GetTempPath(), "typed-settings-" + Guid.NewGuid().ToString("N") + ".db");
        var factory = new DatabaseFactory(path);
        try
        {
            await using (var db = await factory.GetConnectionAsync())
            {
                var schema = new GXSchemaManager(db);
                schema.CreateTable<GXModule>(false, false);
                schema.CreateTable<GXModuleProperty>(false, false);
                var record = new GXModule("typed-database")
                {
                    Creator = new GXUser { Id = "test-user" }, AccessVisibility = ResourceVisibility.Inherit,
                    Enabled = true, Status = ModuleStatus.Installed, Type = 0, NewVersion = false,
                    CreationTime = DateTimeOffset.UtcNow
                };
                await db.InsertAsync(GXInsertArgs.Insert(record));
                record.SetSettings("{\"oldName\":\"meter\",\"$state\":{\"Cursor\":9}}");
                await AmiModuleSettingsStorage.SavePropertiesAsync(db, record, CancellationToken.None);
            }
            var snapshot = new AmiModuleSettings("{\"oldName\":\"meter\",\"$state\":{\"Cursor\":100}}", factory, "typed-database");
            var settingsSchema = new AmiModuleSettingsSchema<Model>(1, new Dictionary<int, Func<JsonObject, JsonObject>>
            {
                [0] = value => { value["Name"] = value["oldName"]?.GetValue<string>(); value.Remove("oldName"); return value; }
            });
            await snapshot.RegisterSchemaAsync(settingsSchema, CancellationToken.None);
            string? stored;
            await using (var db = await factory.GetConnectionAsync())
            {
                stored = (await AmiModuleSettingsStorage.ReadModuleAsync(db, "typed-database", CancellationToken.None))!.GetSettings();
            }
            Assert.AreEqual(1, JsonNode.Parse(stored!)!["$schemaVersion"]!.GetValue<int>());
            Assert.AreEqual(9, JsonNode.Parse(stored!)!["$state"]!["Cursor"]!.GetValue<int>());
            // A persisted future version must also be rejected before any database or snapshot change.
            await using (var db = await factory.GetConnectionAsync())
            {
                var current = (await AmiModuleSettingsStorage.ReadModuleAsync(db, "typed-database", CancellationToken.None))!;
                current.SetSettings("{\"$schemaVersion\":2,\"Name\":\"meter\",\"$state\":{\"Cursor\":9}}");
                await AmiModuleSettingsStorage.SavePropertiesAsync(db, current, CancellationToken.None);
                stored = (await AmiModuleSettingsStorage.ReadModuleAsync(db, "typed-database", CancellationToken.None))!.GetSettings();
            }
            var invalid = new AmiModuleSettings("{\"$schemaVersion\":2,\"Name\":\"meter\"}", factory, "typed-database");
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => invalid.RegisterSchemaAsync(settingsSchema, CancellationToken.None));
            await using (var db = await factory.GetConnectionAsync())
            {
                Assert.AreEqual(stored, (await AmiModuleSettingsStorage.ReadModuleAsync(db, "typed-database", CancellationToken.None))!.GetSettings());
            }
            Assert.AreEqual("{\"$schemaVersion\":2,\"Name\":\"meter\"}", invalid.Value);
        }
        finally
        {
            File.Delete(path);
        }
    }
    [TestMethod]
    public async Task ActivationWaitingForSettingsGateMigratesTheLatestPersistedConfiguration()
    {
        string path = Path.Combine(Path.GetTempPath(), "typed-settings-gate-" + Guid.NewGuid().ToString("N") + ".db");
        var factory = new DatabaseFactory(path);
        try
        {
            await using (var db = await factory.GetConnectionAsync())
            {
                var schema = new GXSchemaManager(db);
                schema.CreateTable<GXModule>(false, false);
                schema.CreateTable<GXModuleProperty>(false, false);
                var record = new GXModule("typed-gate")
                {
                    Creator = new GXUser { Id = "test-user" }, AccessVisibility = ResourceVisibility.Inherit,
                    Enabled = true, Status = ModuleStatus.Installed, Type = 0, NewVersion = false,
                    CreationTime = DateTimeOffset.UtcNow
                };
                await db.InsertAsync(GXInsertArgs.Insert(record));
                record.SetSettings("{\"oldName\":\"initial\",\"$state\":{\"Cursor\":9}}");
                await AmiModuleSettingsStorage.SavePropertiesAsync(db, record, CancellationToken.None);
            }
            await using var provider = new ServiceCollection().AddSingleton<IDbConnectionFactory>(factory).BuildServiceProvider();
            await using var runtime = new GXModuleRuntime(provider, new ConfigurationBuilder().Build(), NullLogger<GXModuleRuntime>.Instance);
            var module = new SettingsModule("typed-gate");
            Task activation;
            using (await AmiModuleSettingsStorage.LockAsync(module.Id, CancellationToken.None))
            {
                activation = runtime.ActivateAsync(module, typeof(SettingsModule).Assembly.Location, "1.0.0", "{\"oldName\":\"stale\"}");
                Assert.IsFalse(activation.IsCompleted);
                // Simulate the configuration writer that already owns the gate completing its save.
                await using var db = await factory.GetConnectionAsync();
                var current = (await AmiModuleSettingsStorage.ReadModuleAsync(db, module.Id, CancellationToken.None))!;
                current.SetSettings("{\"oldName\":\"latest\",\"$state\":{\"Cursor\":10}}");
                await AmiModuleSettingsStorage.SavePropertiesAsync(db, current, CancellationToken.None);
            }
            await activation;
            Assert.AreEqual("latest", module.Snapshot!.Read<Model>().Name);
            await using (var db = await factory.GetConnectionAsync())
            {
                var current = (await AmiModuleSettingsStorage.ReadModuleAsync(db, module.Id, CancellationToken.None))!;
                Assert.AreEqual("latest", JsonNode.Parse(current.GetSettings()!)!["Name"]!.GetValue<string>());
                Assert.AreEqual(10, JsonNode.Parse(current.GetSettings()!)!["$state"]!["Cursor"]!.GetValue<int>());
            }
        }
        finally
        {
            File.Delete(path);
        }
    }
    [TestMethod]
    public async Task ActivationMigratesLegacyAndPreservesDurableState()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await using var runtime = new GXModuleRuntime(provider, new ConfigurationBuilder().Build(), NullLogger<GXModuleRuntime>.Instance);
        var module = new SettingsModule("typed-legacy");
        await runtime.ActivateAsync(module, typeof(SettingsModule).Assembly.Location, "1.0.0", "{\"oldName\":\"meter\",\"$state\":{\"Cursor\":9},\"$resources\":{\"Id\":1}}");
        Assert.AreEqual("meter", module.Snapshot!.Read<Model>().Name);
        Assert.AreEqual(1, JsonNode.Parse(module.Snapshot.Value!)!["$schemaVersion"]!.GetValue<int>());
        Assert.AreEqual(9, JsonNode.Parse(module.Snapshot.Value!)!["$state"]!["Cursor"]!.GetValue<int>());
        await runtime.UpdateSettingsAsync(module.Id, "{\"$schemaVersion\":1,\"Name\":\"next\",\"$state\":{\"Cursor\":100}}");
        Assert.AreEqual(9, JsonNode.Parse(module.Snapshot.Value!)!["$state"]!["Cursor"]!.GetValue<int>());
        Assert.AreEqual(1, JsonNode.Parse(module.Snapshot.Value!)!["$resources"]!["Id"]!.GetValue<int>());
    }

    [TestMethod]
    public async Task InvalidUpdateAndFutureVersionLeaveSnapshotAndCallbacksUnchanged()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await using var runtime = new GXModuleRuntime(provider, new ConfigurationBuilder().Build(), NullLogger<GXModuleRuntime>.Instance);
        var module = new SettingsModule("typed-validation");
        await runtime.ActivateAsync(module, typeof(SettingsModule).Assembly.Location, "1.0.0", "{\"$schemaVersion\":1,\"Name\":\"meter\"}");
        string? before = module.Snapshot!.Value;
        await Assert.ThrowsExactlyAsync<ValidationException>(() => runtime.UpdateSettingsAsync(module.Id, "{\"$schemaVersion\":1,\"Count\":0}"));
        Assert.AreEqual(before, module.Snapshot.Value);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => runtime.UpdateSettingsAsync(module.Id, "{\"$schemaVersion\":2,\"Name\":\"meter\"}"));
        Assert.AreEqual(before, module.Snapshot.Value);
        Assert.AreEqual(0, module.Changes);
        Assert.ThrowsExactly<ValidationException>(() => runtime.NormalizeSettings(module.Id, "{\"$schemaVersion\":1,\"Count\":0}"));
        Assert.AreEqual(before, module.Snapshot.Value);
    }

    [TestMethod]
    public async Task InvalidActivationDoesNotStartModule()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await using var runtime = new GXModuleRuntime(provider, new ConfigurationBuilder().Build(), NullLogger<GXModuleRuntime>.Instance);
        var module = new SettingsModule("typed-future");
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => runtime.ActivateAsync(module, typeof(SettingsModule).Assembly.Location, "1.0.0", "{\"$schemaVersion\":2,\"Name\":\"meter\"}"));
        Assert.IsNull(module.Snapshot);
    }
}
