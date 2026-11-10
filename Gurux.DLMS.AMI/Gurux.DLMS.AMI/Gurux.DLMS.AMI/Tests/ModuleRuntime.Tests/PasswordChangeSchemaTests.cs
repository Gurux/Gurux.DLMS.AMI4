using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Services.KeyManagement;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Model;
using Gurux.Service.Orm.Common.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using Gurux.DLMS.AMI.Server.Controllers;
using Gurux.DLMS.AMI.Server.Repository;
using Gurux.DLMS.AMI.Hubs;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ModuleRuntime.Tests;

[TestClass]
public sealed class PasswordChangeSchemaTests
{
    [TestMethod]
    public async Task UpdateTablesCannotReportSuccessWithoutADatabaseConnection()
    {
        var factory = DispatchProxy.Create<IDbConnectionFactory, UnavailableDatabase>();
        var controller = new ConfigurationController(null!, null!, null!, null!);
        await Assert.ThrowsAsync<IOException>(() => controller.UpdateTables(factory, NullLogger<GXConfiguration>.Instance));
        Assert.AreEqual(1, ((UnavailableDatabase)(object)factory).Attempts);
    }

    public class UnavailableDatabase : DispatchProxy
    {
        public int Attempts;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == nameof(IDbConnectionFactory.GetConnectionAsync))
            {
                Attempts++;
                return Task.FromException<GXDbConnection>(new IOException("Test database unavailable."));
            }
            throw new NotSupportedException(method.Name);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task MissingPasswordTablesAreDetectedAndCreatedWithoutReset(bool removeLock)
    {
        var path = Path.Combine(Path.GetTempPath(), "ami-password-schema-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            var factory = new SqlConnectionFactory(DatabaseType.SqLite, $"Data Source={path};Foreign Keys=False;Pooling=False");
            await using var db = await factory.GetConnectionAsync();
            var settings = typeof(GXPasswordChangeService).Assembly.GetType("Gurux.DLMS.AMI.Server.ServerSettings")!;
            var initialize = settings.GetMethod("InitializeDatabaseAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
            await (Task)initialize.Invoke(null, [db, NullLogger<GXConfiguration>.Instance])!;
            var schema = new GXSchemaManager(db);
            using (var transaction = db.BeginTransaction())
            {
                schema.ForceDropTable<GXPasswordChangeRecord>(transaction);
                if (removeLock)
                {
                    schema.ForceDropTable<GXPasswordChangeLock>(transaction);
                }
                transaction.Commit();
            }
            var retained = new GXConfiguration { Name = "retained-schema-check", Settings = "metadata" };
            await db.InsertAsync(GXInsertArgs.Insert(retained));
            var repository = new ConfigurationRepository(factory, null!, Options.Create(new IdentityOptions()), new NotificationCache());
            var pending = (await repository.GetTableChangesAsync()).ToArray();
            Assert.IsTrue(pending.Any(change => change.TableName == nameof(GXPasswordChangeRecord) && change.Changes.Contains("Table is missing")),
                "The Update page must detect server-owned password tables.");
            Assert.AreEqual(removeLock, pending.Any(change => change.TableName == nameof(GXPasswordChangeLock) && change.Changes.Contains("Table is missing")));
            Assert.IsFalse(schema.TableExist<GXPasswordChangeRecord>(), "Inspecting changes must not apply them automatically.");
            using (var transaction = db.BeginTransaction())
            {
                var missing = pending.Where(change => change.Changes.Contains("Table is missing")).Select(change => change.TableName).ToHashSet();
                foreach (var type in new[] { typeof(GXPasswordChangeRecord), typeof(GXPasswordChangeLock) }.Where(type => missing.Contains(type.Name)))
                {
                    schema.CreateTable(transaction, type, true, false);
                }
                transaction.Commit();
            }
            var remaining = (await repository.GetTableChangesAsync()).ToArray();
            Assert.IsFalse(remaining.Any(change => (change.TableName == nameof(GXPasswordChangeRecord) || change.TableName == nameof(GXPasswordChangeLock)) && change.Changes.Contains("Table is missing")));
            Assert.IsTrue(schema.TableExist<GXPasswordChangeRecord>(), "Existing installations need the operation table.");
            Assert.IsTrue(schema.TableExist<GXPasswordChangeLock>(), "Existing installations need the per-meter lock table.");
            Assert.IsNotNull(await db.SingleOrDefaultAsync<GXConfiguration>(GXSelectArgs.SelectAll<GXConfiguration>(c => c.Name == retained.Name)));
            // Repeated startup must preserve existing pending operations and their lock.
            var operation = new GXPasswordChangeRecord { Id = Guid.NewGuid(), DeviceId = Guid.NewGuid(), State = "Queued" };
            await db.InsertAsync(GXInsertArgs.Insert(operation));
            await db.InsertAsync(GXInsertArgs.Insert(new GXPasswordChangeLock { Id = operation.DeviceId, OperationId = operation.Id }));
            await (Task)initialize.Invoke(null, [db, NullLogger<GXConfiguration>.Instance])!;
            Assert.IsNotNull(await db.SingleOrDefaultAsync<GXPasswordChangeRecord>(GXSelectArgs.SelectAll<GXPasswordChangeRecord>(r => r.Id == operation.Id)));
            Assert.IsNotNull(await db.SingleOrDefaultAsync<GXPasswordChangeLock>(GXSelectArgs.SelectAll<GXPasswordChangeLock>(r => r.Id == operation.DeviceId)));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
