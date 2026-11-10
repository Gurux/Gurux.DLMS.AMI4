using System.Reflection;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Hubs;
using Gurux.DLMS.AMI.Server.Controllers;
using Gurux.DLMS.AMI.Server.Repository;
using Gurux.DLMS.AMI.Server.Services.KeyManagement;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Rest;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Common.Enums;
using Gurux.Service.Orm.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ModuleRuntime.Tests;

[TestClass]
public sealed class SqlServerSchemaUpdateTests
{
    [TestMethod]
    [TestCategory("SqlServer")]
    public async Task UpdateTablesEndpointUpgradesExistingDatabaseAndPreservesDataOnRepeatedUpdate()
    {
        var connectionString = Environment.GetEnvironmentVariable("AMI_SQLSERVER_SCHEMA_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Inconclusive("Set AMI_SQLSERVER_SCHEMA_TEST_CONNECTION to test schema upgrades in an isolated SQL Server database.");
        }
        var options = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master", ConnectTimeout = 5 };
        const string prefix = "AmiSchemaCheck_";
        var databaseName = prefix + Guid.NewGuid().ToString("N");
        await using var master = new SqlConnection(options.ConnectionString);
        await master.OpenAsync();
        var created = false;
        try
        {
            using (var command = master.CreateCommand())
            {
                command.CommandText = "CREATE DATABASE [" + databaseName + "]";
                await command.ExecuteNonQueryAsync();
                created = true;
            }
            options.InitialCatalog = databaseName;
            var factory = new SqlConnectionFactory(DatabaseType.MSSQL, options.ConnectionString);
            await using var db = await factory.GetConnectionAsync();
            var settings = typeof(ConfigurationRepository).Assembly.GetType("Gurux.DLMS.AMI.Server.ServerSettings")!;
            var initialize = settings.GetMethod("InitializeDatabaseAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
            await (Task)initialize.Invoke(null, [db, NullLogger<GXConfiguration>.Instance])!;
            var schema = new GXSchemaManager(db);
            using (var transaction = db.BeginTransaction())
            {
                schema.ForceDropTable<GXPasswordChangeRecord>(transaction);
                schema.ForceDropTable<GXPasswordChangeLock>(transaction);
                transaction.Commit();
            }
            var sentinel = new GXConfiguration { Name = "Preserved during schema update", Settings = "retained" };
            await db.InsertAsync(GXInsertArgs.Insert(sentinel));
            using var services = new ServiceCollection().AddSingleton<NotificationCache>()
                .AddSingleton(DispatchProxy.Create<IUserRepository, NoRecipients>()).BuildServiceProvider();
            var repository = new ConfigurationRepository(factory, services, Options.Create(new IdentityOptions()), services.GetRequiredService<NotificationCache>());
            var pending = (await repository.GetTableChangesAsync()).ToArray();
            Assert.IsTrue(pending.Any(t => t.TableName == nameof(GXPasswordChangeRecord)));
            Assert.IsTrue(pending.Any(t => t.TableName == nameof(GXPasswordChangeLock)));
            Assert.IsFalse(schema.TableExist<GXPasswordChangeRecord>(), "Checking for updates must not apply them.");
            var controller = new ConfigurationController(repository, null!, null!, services);
            var first = await controller.UpdateTables(factory, NullLogger<GXConfiguration>.Instance);
            Assert.IsInstanceOfType<OkObjectResult>(first.Result);
            Assert.IsEmpty((IEnumerable<GXConfigurationTableChange>)((OkObjectResult)first.Result!).Value!);
            Assert.IsTrue(schema.TableExist<GXPasswordChangeRecord>());
            Assert.IsTrue(schema.TableExist<GXPasswordChangeLock>());
            var retained = await db.SingleOrDefaultAsync<GXConfiguration>(GXSelectArgs.SelectAll<GXConfiguration>(c => c.Name == sentinel.Name));
            Assert.AreEqual("retained", retained!.Settings);
            var operation = new GXPasswordChangeRecord { Id = Guid.NewGuid(), DeviceId = Guid.NewGuid(), State = "Queued" };
            await db.InsertAsync(GXInsertArgs.Insert(operation));
            await db.InsertAsync(GXInsertArgs.Insert(new GXPasswordChangeLock { Id = operation.DeviceId, OperationId = operation.Id }));
            var repeated = await controller.UpdateTables(factory, NullLogger<GXConfiguration>.Instance);
            Assert.IsEmpty((IEnumerable<GXConfigurationTableChange>)((OkObjectResult)repeated.Result!).Value!);
            Assert.IsNotNull(await db.SingleOrDefaultAsync<GXPasswordChangeRecord>(GXSelectArgs.SelectAll<GXPasswordChangeRecord>(r => r.Id == operation.Id)));
            Assert.IsNotNull(await db.SingleOrDefaultAsync<GXPasswordChangeLock>(GXSelectArgs.SelectAll<GXPasswordChangeLock>(r => r.Id == operation.DeviceId)));
        }
        finally
        {
            // Only this test's randomly generated database can be removed.
            if (created && databaseName.StartsWith(prefix, StringComparison.Ordinal) && databaseName.Length == prefix.Length + 32)
            {
                using var command = master.CreateCommand();
                command.CommandText = "ALTER DATABASE [" + databaseName + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [" + databaseName + "]";
                await command.ExecuteNonQueryAsync();
            }
        }
    }

    public class NoRecipients : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == nameof(IUserRepository.GetUserIdsInRoleAsync))
            {
                return Task.FromResult(new List<string>());
            }
            throw new NotSupportedException(method.Name);
        }
    }
}
