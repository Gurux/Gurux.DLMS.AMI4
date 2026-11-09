# Module test support

This source-only, non-packable project references the AMI server assembly and requires the matching Gurux.DLMS.AMI4 source checkout. It uses the real GXModuleRuntime; it does not launch Program, HTTP listeners, hosted application services or a database.

Create an AmiModuleTestHost with an optional IServiceCollection callback and IConfiguration, activate an IGXAmiModule, invoke its Dispatcher, publish typed Events, inspect GetManifest, then StopAsync or ReloadAsync. Dispose the host with await using to drain and dispose module generations and host services. Registrations supplied by the callback override defaults through the usual DI rules.

RepositoryProxy.Create<T> creates configurable interface stubs. DatabaseTestContext is opt-in: construction selects a private SQLite file by default, and CreateTablesAsync explicitly creates its test tables. External database providers require the disposable database runner environment variables. No database fixture is constructed by AmiModuleTestHost.
