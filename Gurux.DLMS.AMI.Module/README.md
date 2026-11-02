# Gurux.DLMS.AMI.Module

Contracts and helpers for extending Gurux DLMS AMI with server functionality and
Blazor UI components. Modules declare their services, HTTP endpoints, views,
pages, dependencies and owned tables; the AMI host manages their activation,
scopes, authorization and shutdown.

The library targets .NET 10 and does not reference the AMI Server or Client
projects. Modules run as trusted extensions inside the host process.

## Contents

- [Getting started](#getting-started)
- [Contracts and namespaces](#contracts-and-namespaces)
- [Lifecycle and services](#lifecycle-and-services)
- [HTTP endpoints](#http-endpoints)
- [Module authentication](#module-authentication)
- [Module views](#module-views)
- [Settings and persistence](#settings-and-persistence)
- [Application logs](#application-logs)
- [Host confirmation dialog](#host-confirmation-dialog)
- [Deployment](#deployment)

## Getting started

Install the .NET 10 SDK and reference the `Gurux.DLMS.AMI.Module` NuGet package
or the `Gurux.DLMS.AMI.Module.csproj` project. Use a version compatible with your
AMI host and its other contract assemblies.

Derive from `GXAmiModuleBase` and give the module a stable, unique identifier:

```csharp
using Gurux.DLMS.AMI.Module;
using Microsoft.AspNetCore.Http;

public sealed class ExampleModule : GXAmiModuleBase
{
    public override string Id => "Example";
    public override string Name => "Example module";
    public override string Description => "Exposes a module status endpoint.";

    public override void Configure(AmiModuleBuilder builder)
    {
        builder.MapGet("status", async (request, cancellationToken) =>
        {
            await request.HttpContext.Response.WriteAsJsonAsync(
                new { Module = request.Module.ModuleId, Status = "Ready" },
                cancellationToken: cancellationToken);
        });
    }
}
```

The AMI host publishes this endpoint at `GET /api/extensions/Example/status`.
It requires authentication and the host's module access checks. Load the module
through the host's module management; this contract library does not run or
install a host application.

## Contracts and namespaces

| Contract | Location |
| --- | --- |
| Module lifecycle, builder, requests, views, authentication and service dispatch | `Gurux.DLMS.AMI.Module` |
| `AmiModuleState`, `AmiModuleDependencyKind`, `AmiViewArea`, `AmiViewDisplay`, `AmiAuthenticationStatus` | `Gurux.DLMS.AMI.Module.Enums` |
| `AmiModuleDependency` | Declared by this assembly in `Gurux.DLMS.AMI.Shared.DTOs.Module` |
| `AmiModuleManifest` | AMI Client assembly; not part of this library |
| `IGXDatabase` and database query abstractions | Separate `Gurux.DLMS.AMI.Services` project, namespace `Gurux.DLMS.AMI.Services.Database` |
| Domain repository interfaces such as `ILogRepository` | Separate Shared assembly, namespace `Gurux.DLMS.AMI.Shared.DIs` |

An assembly's name and a type's namespace are separate: using
`AmiModuleDependency` does not require this project to reference Shared.
Reference Services or Shared explicitly when your module uses their APIs.

## Lifecycle and services

`Configure` declares services and features before activation. Perform asynchronous
initialization in lifecycle methods rather than starting work from `Configure`.
The base class supplies empty hooks and defaults to no dependencies and no
scheduled execution.

| Hook | Purpose |
| --- | --- |
| `InstallAsync` | Create initial module defaults and resources. |
| `UpdateAsync` | Migrate resources from `previousVersion`. |
| `StartAsync` | Start active module operations. |
| `SettingsChangedAsync` | Apply updated configuration. |
| `ExecuteAsync` | Execute scheduled work when `CanSchedule` is `true`. |
| `StopAsync` | Stop module operations and release resources. |
| `UninstallAsync` | Remove resources according to `context.UninstallOptions`. |

`AmiModuleContext` provides the module's `ModuleId`, module-scoped `Services`,
`HostServices`, `Settings`, `Resources`, `UserSettings`, `JsonOptions` and current
`CancellationToken`. Use `PackageDirectory` for package files instead of assuming
that the loaded assembly has an on-disk location. `Own(IDisposable)` registers a
resource for disposal when the module stops. Honor cancellation in ongoing work
and do not retain a request or scoped service after its operation ends.

Register dependencies through `builder.Services`. `AddHostSingleton<TService>()`
shares a host-owned singleton without transferring disposal ownership.
To expose a service to other modules, register its implementation and call
`builder.Export<TService>()`. Exported service contracts must be interfaces from
a shared, non-collectible assembly. Consumers use `IAmiModuleServices.InvokeAsync`
and finish using the service inside the callback; never cache the implementation
or return objects that retain its module scope. `IsAvailable<TService>()` is a
point-in-time check, not a reservation.

Declare mandatory dependencies by overriding `Dependencies` with
`AmiModuleDependency` values. `AmiModuleDependencyKind` distinguishes host
assemblies, other modules and exported services. Minimum versions are inclusive;
maximum versions are exclusive.

## HTTP endpoints

`MapGet`, `MapPost` and `MapMethods` take a route relative to
`/api/extensions/{moduleId}/` and return an `AmiEndpointDefinition`.
Use `RequirePolicy` to add operation-specific access policies:

```csharp
builder.MapPost("refresh", RefreshAsync).RequirePolicy("device.edit");
```

Here `RefreshAsync` is the module's handler with signature
`Task RefreshAsync(AmiModuleRequest request, CancellationToken cancellationToken)`.
Policies must exist in the host. A builder endpoint without an explicit policy
still requires an authenticated user and passes host module access checks.

The handler receives `HttpContext` and its scoped `AmiModuleContext` through
`AmiModuleRequest`. Use `request.Module.Services` for module services and
`request.Module.HostServices` for host services. The supplied token combines
request cancellation and module shutdown.

For framework integrations that supply endpoints, register their source in
`builder.Services` and declare it with `builder.AddEndpointSource<TSource>()`.
The host validates and publishes the module-owned endpoints. `builder.Use(...)`
registers module middleware; middleware is separate from the authentication
extension described below.

## Module authentication

Most modules use the host's existing cookie or bearer authentication. A module
that validates another credential format can register one scoped authenticator:

```csharp
builder.Services.AddScoped<IExampleTokenValidator, ExampleTokenValidator>();
builder.AddAuthenticator<ExampleAuthenticator>();
```

`IExampleTokenValidator` and `ExampleTokenValidator` in this example are
module-specific services; implement them for your protocol. The validator must
return a validated `ClaimsPrincipal`, or `null` for invalid credentials.

```csharp
using System.Net.Http.Headers;
using System.Security.Claims;
using Gurux.DLMS.AMI.Module;

public interface IExampleTokenValidator
{
    Task<ClaimsPrincipal?> ValidateAsync(string token, CancellationToken cancellationToken);
}

public sealed class ExampleAuthenticator(IExampleTokenValidator tokens)
    : IAmiModuleAuthenticator
{
    public async Task<AmiAuthenticationResult> AuthenticateAsync(
        AmiModuleRequest request, CancellationToken cancellationToken)
    {
        var authorization = request.HttpContext.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization))
            return AmiAuthenticationResult.NotHandled;
        if (!AuthenticationHeaderValue.TryParse(authorization, out var header))
            return AmiAuthenticationResult.Rejected;
        if (!string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
            return AmiAuthenticationResult.NotHandled;
        if (string.IsNullOrWhiteSpace(header.Parameter))
            return AmiAuthenticationResult.Rejected;

        var principal = await tokens.ValidateAsync(header.Parameter, cancellationToken);
        return principal?.Identities.Any(identity => identity.IsAuthenticated) == true
            ? AmiAuthenticationResult.Authenticated(principal)
            : AmiAuthenticationResult.Rejected;
    }
}
```

This example takes responsibility for all bearer credentials on the module's
endpoints. If a module also accepts host bearer tokens, its credential validation
must distinguish the supported token formats without falling back after an
applicable token fails validation.

| Result | Host behavior |
| --- | --- |
| `NotHandled` | Use normal host cookie or bearer authentication. Return this only when credentials are absent or do not apply. |
| `Rejected` | Return HTTP 401 without authentication fallback, even for an anonymous endpoint or a request with a valid cookie. |
| `Authenticated(principal)` | Validate the mapped host user and apply normal authorization using the restricted principal. |

Validate credentials on every request: signature, issuer, audience, expiry and
revocation as applicable. Include the host user identifier as
`ClaimTypes.NameIdentifier` or `sub`; all supplied identifier values must agree.
The mapped user must be approved and not removed. The host intersects supplied
roles and `Permission` claims with current host grants; it does not add grants
absent from the credential. Never put credentials in claims, logs or exceptions.
Do not write a response body or retain the request from the authenticator.

Authentication runs only for endpoints owned by the registering module,
including module-owned endpoint sources. Ownership is bound to the selected
endpoint and module generation, not a URL prefix. A stale endpoint cannot use a
replacement module's authenticator or fall back after the module stops. The host
copies returned claims before releasing the authentication scope. Authentication
and endpoint handling use separate module scopes, so pass identity through the
returned principal rather than an authenticator instance.

Use the host's default authentication scheme on these endpoints. Explicitly
selecting a different ASP.NET authentication scheme bypasses default module
selection. Ordinary authorization policies and module access checks still apply.
The extension is protocol-independent: an MCP module can implement OAuth/token
validation without adding MCP-specific contracts to Server. This library does
not supply an OAuth server or an MCP token validator.

## Policy editor

The shared policy selection UI is `Gurux.UI.Components.PolicyEditor`, provided
by the `Gurux.UI.Components` project. It is no longer part of this module library.
Client and module pages bind `List<string>?` policy names with `@bind-Value`.
The host supplies `HttpClient`, `IGXNotificationService`, and `IGXProgress` through
DI. Loading uses `api/Policy/Available`; failures use the notification service
and loading can be cancelled through the progress service.

## Module views

Register UI components in the module's `Configure` method. Each view declares its
area and required policy. Its optional key distinguishes views within that area;
only one view per area may omit a key.

```csharp
public override void Configure(AmiModuleBuilder builder)
{
    builder.AddView<Configuration>(AmiViewArea.Configuration, "module.edit");
    builder.AddView<Subscriptions>(AmiViewArea.User, "notification.view", title: "Notifications");
}
```

`AmiViewArea` supports `Configuration`, `Extension`, `Schedule`, `User`, `Config`,
and entity editor areas. Optional title, icon, order and display values control presentation;
`AmiViewDisplay.Tab` is the default. Use `AddPage` for routed pages.
`CanSchedule` declares whether the module can execute scheduled work and defaults
to false. A module that supports scheduling overrides it with `true` independently
of its optional `Schedule` view. Configuration, extension and schedule components
are registered using `AddView` rather than module properties.

## Application logs

Use `ILogRepository` from `Gurux.DLMS.AMI.Shared.DIs` for authenticated application log operations.
Select a source before accessing its records:

```csharp
var logs = serviceProvider.GetRequiredService<ILogRepository>().ForSource("module");
await logs.AddAsync("Info", [new GXLog { SourceId = moduleId, Message = "Module started." }], cancellationToken);
```

Sources are `system`, `agent`, `gateway`, `module`, `schedule`, `script`,
`workflow`, `key-management`, `device-error`, `device-action`, `user-error`
and `user-action`. Each source retains its own operation policies and ownership
restrictions. `ForSource` returns an independent binding; it does not change the
original repository. A source is required for every operation and must resolve
to an existing, non-removed `GXLogType`. Custom log types can be selected by name
or public identifier. Writing logs does not create a missing log type. Clearing
or deleting logs always stays within the selected type; clearing all types is
not supported.

All HTTP log operations use `api/Log`: for example,
`POST api/Log/List?source=module` and `POST api/Log/Add?source=module`.
The shared contracts are `ListLogs`, `AddLog`, `ClearLog` and `CloseLog`, with
their corresponding responses. Read a record using
`GET api/Log?source=module&id=<public-id>`.
The former source-specific repository interfaces and API controllers have been
removed; modules using them must update their references and rebuild.

## Settings and persistence

Modules must change AMI-owned resources through domain interfaces rather than
writing their tables directly. Use `context.Resources` (`IAmiModuleResources`,
also available from module DI) for module capture defaults, capture logs,
log menu removal and administrator notification defaults. Menu link identifiers
are generated by the menu repository. `RemoveLogMenuAsync(cancellationToken)`
reads the calling module's generated link identifier from protected module settings;
modules do not supply identifiers. The host validates
policy names and references, binds log sources to the module, persists related
records transactionally, and publishes notifications after commit. Existing
user edits and removed resources are preserved. Request-specific notification
edits continue to use `INotificationRepository`.

`GXModuleResources` only coordinates repository calls. Resource-specific
validation, persistence and notification decisions belong to the policy,
log type, menu, enum, log and notification repositories. Their trusted module
lifecycle operations share a transaction and queue `NotificationCache` entries
only after commit. They do not require a fabricated HTTP user, and normal
HTTP repository authorization is unchanged.

```csharp
await context.Resources.EnsureCaptureAsync(new AmiModuleCapture(
    "Example.capture", typeof(ExampleLogTab).AssemblyQualifiedName!, "bi-list"), cancellationToken);
await context.Resources.AddLogsAsync(
    [new AmiModuleLog("device", userId, "Device changed")], [userId], cancellationToken);
```

Direct database writes are reserved for the module's own registered tables when
shared module or user settings cannot represent the data. Do not insert `GXPolicy`, requirements,
resource policy memberships, `GXLogType`, `GXMenuLink`, `GXEnumType`, `GXLog`
or notification defaults directly from a module.

Database abstractions and query descriptions such as `IGXDatabase`, `AmiSelect`
and `AmiSelectQuery<T>` are in the separate Services project under
`Gurux.DLMS.AMI.Services.Database`. Table ownership declarations belong to this
Module library: register them with `builder.OwnTable<T>()` and select an
`AmiModuleTableKind` where needed. Uninstall defaults to preserving module data;
read `context.UninstallOptions` when implementing data removal.

Module settings are exposed as `IAmiModuleSettingsSnapshot` through the context,
builder and module DI scope. `Value` and `Read<T>()` are read-only; the host owns
JSON deserialization and updates. The concrete snapshot implementation is
internal to the server and is not part of the module assembly.

Use `ReadStateAsync<T>()` and `UpdateStateAsync<T>()` for durable server-managed
singleton state. This state is stored with module settings separately from
editable configuration; use one state model per module.


### User data without module tables

Use `context.UserSettings` (`IAmiModuleUserSettings`, also available in module DI)
for module-managed user data. The host binds the module identifier and the
UserSettingRepository owns JSON persistence, relations, transactions and
owner-targeted notifications after commit. Operations also work in background
services without an HTTP request. Names are limited to 64 characters.

```csharp
await context.UserSettings.UpsertAsync(userId, "preferences", preferences,
    cancellationToken: cancellationToken);
var savedPreferences = await context.UserSettings.ReadAsync<Preferences>(
    userId, "preferences", cancellationToken);
await context.UserSettings.DeleteAsync(userId, "preferences", cancellationToken);
```

`ListAsync<T>` returns this module's named values for one user; use a common
value type for values listed together. For identifiers that must belong to only
one user in a module, use `exclusiveToUser: true` consistently for that name.
WebPush uses this for endpoint hashes. Exclusive primary keys exclude the user,
so competing hosts cannot insert the same endpoint for different users.
Ordinary user settings APIs and user details exclude module-managed values;
module endpoints validate payloads and derive the owner from authentication.
Modules are trusted host extensions: this interface does not sandbox arbitrary
module code. WebPush subscriptions now use GXUserSetting and own no tables.

Use the `Version` returned by `ListAsync<T>` with `DeleteIfUnchangedAsync` when
removing stale data after an external operation. WebPush uses this to prevent a
late 410 response from deleting a browser subscription refreshed during sending.

## Host confirmation dialog

Browser module views can inject `IAmiConfirmation` from the Module contract:

```razor
@inject IAmiConfirmation Confirmation
```

```csharp
if (await Confirmation.ConfirmAsync(localizedTitle, localizedMessage))
{
    await ClearAsync();
}
```

The layout owns a single confirmation dialog with localized cancel/OK labels.
Titles and messages are plain text.
Only explicit approval returns true; cancel, close, missing host and host disposal
return false. Concurrent requests are queued, and stale answers cannot approve
another request. This is a UI service, separate from background notifications.
Module code must recheck that its view/source still exists before performing the
action after awaiting confirmation. Server-side action policies still apply.

Use the options overload to display selected item names, customize the action
button, or let the user choose disabling versus permanent deletion:

```csharp
var answer = await Confirmation.ConfirmAsync(title, message, new AmiConfirmationOptions
{
    ConfirmText = localizedRemove,
    Items = selectedNames,
    AllowDelete = true
});
if (answer.Confirm)
{
    await RemoveAsync(deletePermanently: answer.Delete);
}
```

Disabling is selected initially for every dialog. `Delete` is true only when the
user approves permanent deletion in a dialog that offers that choice. Selected
item names are copied when queued and displayed as encoded text.

## Deployment

Build and publish the module project using a .NET 10 SDK, for example:

```shell
dotnet publish MyModule.csproj -c Release
```

Deployment packaging belongs to the module project or its repository's build
tooling. This library does not create a module ZIP or install the module.
Package the module assembly, module-private managed and native dependencies,
and required static resources. Keep host-provided shared contracts compatible
with the target AMI installation; avoid shipping conflicting copies of those
assemblies. Follow the target host's module installation and update workflow.

## Contributing

Follow the repository's coding guidelines and `.editorconfig` where provided.
Keep changes focused, document public contracts and verify examples against the
current API. Include tests for changes to registration, authorization or lifecycle
behavior.

## License and support

Licensed under the GNU General Public License v2 (GPL-2.0).
See [the license text](https://www.gnu.org/licenses/gpl-2.0.txt).

Source and issues: [Gurux.DLMS.AMI4](https://github.com/Gurux/Gurux.DLMS.AMI4).
