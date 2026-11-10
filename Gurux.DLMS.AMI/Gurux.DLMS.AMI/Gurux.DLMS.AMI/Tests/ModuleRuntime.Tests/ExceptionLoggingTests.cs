using System.Text.Json;
using Gurux.DLMS.AMI.Db;
using Gurux.DLMS.AMI.Server.Midlewares;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Log;
using Gurux.Service.Orm;
using Gurux.Service.Orm.Common.Enums;
using Gurux.Service.Orm.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ModuleRuntime.Tests;

[TestClass]
public sealed class ExceptionLoggingTests
{
    [TestMethod]
    public async Task UnexpectedErrorIsPersistedWithoutAnAdministratorOrRequestLogPermissions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ami-exception-log-{Guid.NewGuid():N}.db");
        try
        {
            var factory = new SqlConnectionFactory(DatabaseType.SqLite, $"Data Source={path};Foreign Keys=False;Pooling=False");
            await using var connection = await factory.GetConnectionAsync();
            var schema = new GXSchemaManager(connection);
            foreach (var type in new[] { typeof(GXUser), typeof(GXLogType), typeof(GXPolicy), typeof(GXLogTypePolicy), typeof(GXLog) })
            {
                schema.CreateTable(type, false, false);
            }
            // No administrator, authorization service, or log repository is available.
            using var services = new ServiceCollection().BuildServiceProvider();
            var context = new DefaultHttpContext { RequestServices = services, TraceIdentifier = "device-save-trace" };
            context.Request.Path = "/api/Device/Update";
            context.Response.Body = new MemoryStream();
            Exception failure;
            try
            {
                throw new InvalidOperationException("device-save-failure", new Exception("database-inner-failure"));
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            var handler = new GXExceptionHandler(NullLogger<GXExceptionHandler>.Instance, factory);
            Assert.IsTrue(await handler.TryHandleAsync(context, failure, CancellationToken.None));
            Assert.AreEqual(500, context.Response.StatusCode);

            var logs = await connection.SelectAsync<GXLog>(GXSelectArgs.SelectAll<GXLog>());
            Assert.HasCount(1, logs, "Server failures must survive missing administrator accounts and request permissions.");
            var entry = logs.Single();
            Assert.AreEqual("device-save-failure", entry.Message);
            StringAssert.Contains(entry.StackTrace!, "database-inner-failure");
            StringAssert.Contains(entry.StackTrace!, nameof(UnexpectedErrorIsPersistedWithoutAnAdministratorOrRequestLogPermissions));
            StringAssert.Contains(entry.Data!, "/api/Device/Update");
            StringAssert.Contains(entry.Data!, "device-save-trace");
            var typeRow = await connection.SingleOrDefaultAsync<GXLogType>(GXSelectArgs.SelectAll<GXLogType>());
            Assert.AreEqual("BUILTIN:SYSTEM", typeRow!.NormalizedName);

            context.Response.Body.Position = 0;
            using var problem = await JsonDocument.ParseAsync(context.Response.Body);
            Assert.AreEqual("Internal server error.", problem.RootElement.GetProperty("detail").GetString());
            Assert.AreEqual("device-save-trace", problem.RootElement.GetProperty("traceId").GetString());
            Assert.IsFalse(problem.RootElement.ToString().Contains("database-inner-failure"));
        }
        finally
        {
            File.Delete(path);
        }
    }
}