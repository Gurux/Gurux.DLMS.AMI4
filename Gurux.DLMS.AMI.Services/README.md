# Gurux.DLMS.AMI.Services

Gurux.DLMS.AMI.Services defines the shared service contracts used by **Gurux DLMS AMI v4**, an Advanced Metering Infrastructure application for working with DLMS/COSEM meters.

The library allows AMI components and extension modules to use common services through interfaces without depending directly on their implementations. It contains service interfaces and data models; the AMI host or an extension module provides the actual functionality.

## What is it used for?

- **Database access:** read and modify application data, manage tables, and work with transactions through `IGXDatabase`.
- **Query descriptions:** describe selected columns, filters, joins, ordering, and pagination without referencing the host's ORM directly.
- **Notifications:** integrate email, SMS, and MQTT message delivery.
- **HTTP access:** request public content through the host's HTTP proxy and its network and caching policies.
- **Geolocation:** obtain an approximate location for an IP address through an optional provider.
- **QR functionality:** integrate QR image generation and phone approval for login through optional modules.
- **Module integration:** receive SQL command notifications and clean up module-owned user-group references during deletion.

For example, an extension module can send an email through `IGXAmiEmailSender` or query its data through `IGXDatabase`, while the host supplies the configured service implementation.

## Project structure

| Location | Contents |
| --- | --- |
| Project root | Communication, HTTP proxy, geolocation, and QR service contracts |
| `Database/` | Database interface, query descriptions, SQL events, and membership lifecycle contract |

Most contracts use the `Gurux.DLMS.AMI.Services` and `Gurux.DLMS.AMI.Services.Database` namespaces. The QR contracts use `Gurux.DLMS.AMI.Module`.

## Using the library

Reference this project or the `Gurux.DLMS.AMI.Services` NuGet package from an AMI component or extension module. Obtain the required service implementation through the AMI host's integration mechanism. Optional capabilities require a module that provides the corresponding service.

This is a class library, so it does not run as a standalone application. The project targets **.NET 10** and generates XML API documentation when built.

To build from this directory with the .NET 10 SDK:

```sh
dotnet build Gurux.DLMS.AMI.Services.csproj
```

## More information

- [Gurux DLMS AMI](https://www.gurux.fi/Gurux.DLMS.AMI4)
- [Gurux DLMS AMI v4 source repository](https://github.com/gurux/gurux.dlms.ami4)

## License

GNU General Public License v2.0 only (`GPL-2.0-only`). See the license notices in the source files.
