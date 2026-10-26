# Gurux.DLMS.AMI4

Lightweight .NET library containing AMI (Advanced Metering Infrastructure) extensions for the Gurux DLMS/COSEM framework.

## Features

- AMI-specific module interfaces and helpers under the `Gurux.DLMS.AMI.Module` namespace
- Designed to integrate with the Gurux Device Framework
- Open source (GPLv2)

## Requirements

- .NET 9 SDK
- C# 13
- __Visual Studio 2022__ or later (or `dotnet` CLI)

## Quick start

1. Clone the repository:


   git clone https://github.com/Gurux/Gurux.DLMS.AMI4.git
   cd Gurux.DLMS.AMI4


2. Open the solution in __Visual Studio 2022__ or use the CLI:

- In Visual Studio: File > Open > Project/Solution, open the `.sln` file, then use __Build > Build Solution__.
- CLI: `dotnet restore` then `dotnet build`.

## Usage

Reference the assembly or project and use the types in the `Gurux.DLMS.AMI.Module` namespace. Example:


using Gurux.DLMS.AMI.Module;

public class MyModule : GXAmiModuleBase
{
    public override string Id => "MyModule";
    public override string Name => "My module";
    public override string Description => "Adds attribute settings.";
    public override void Configure(AmiModuleBuilder builder)
    {
        builder.AddView<MyAttributeSettings>(AmiViewArea.AttributeSettings, "module.edit");
    }
}


## Policy editor

The shared policy selection UI is `Gurux.UI.Components.PolicyEditor`, provided
by the `Gurux.UI.Components` project. It is no longer part of this module library.
Client and module pages bind `List<string>?` policy names with `@bind-Value`.
The host supplies `HttpClient`, `IGXNotificationService`, and `IGXProgress` through
DI. Loading uses `api/Policy/Available`; failures use the notification service
and loading can be cancelled through the progress service.

## Deployment packages

Module projects in `Gurux.DLMS.AMI.Modules` share `Directory.Build.targets`, which
excludes assemblies supplied by the AMI host from publish output and ZIP files.
This covers Microsoft/System references present in the host and the shared Gurux
contracts. Unknown Microsoft/System packages and module-private libraries remain
included, along with native dependencies and static resources. Development builds
keep CopyLocal dependencies. Reused publish folders are cleaned before ZIP creation.

Use `AmiModuleHostProject` to select another host project, or
`AmiModuleExcludeHostAssemblies=false` to publish without filtering.
`AmiModuleArchiveDirectory` selects the ZIP output directory. The verification
script `scripts/Test-AmiModulePublish.ps1` checks ZIP contents and loads the module
against host assemblies without initializing its application services.

## Contributing

Please follow the repository's coding guidelines and the project's `__CONTRIBUTING.md__` and `.editorconfig` files. Open issues and pull requests against the `main` branch. Keep changes small and focused, and include unit tests where appropriate.

## License

This code is licensed under the GNU General Public License v2 (GPL-2.0). See http://www.gnu.org/licenses/gpl-2.0.txt for details.

## Contact

Repository: [Gurux.DLMS.AMI4 GitHub](https://github.com/Gurux/Gurux.DLMS.AMI4)

For questions or support, open an issue on the repository.

## Module metadata

AmiModuleManifest is defined in Gurux.DLMS.AMI.Client and used by the Blazor client
and its host. AmiModuleView and AmiModulePage are defined in Gurux.DLMS.AMI.Module
for module builders. AmiModuleDependency stays in Gurux.DLMS.AMI.Shared.DTOs.Module;
AmiModuleState and AmiModuleDependencyKind stay in Gurux.DLMS.AMI.Shared.Enums.
Module and Shared do not reference Client. Runtime exceptions and integration
interfaces in AmiModuleIntegrations.cs remain in Gurux.DLMS.AMI.Module.

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

### Database contracts

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

Database abstractions, query descriptions and table ownership declarations are
in the `Database` folder and `Gurux.DLMS.AMI.Services.Database` namespace.
Use `using Gurux.DLMS.AMI.Services.Database;` for `IGXDatabase`, `AmiSelect`,
`AmiSelectQuery<T>`, and module table registrations. The Module project has no
project dependency on Shared.

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

### Host confirmation dialog

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

The host renders the same Confirm component used by SystemLog, with its standard
buttons and localized cancel/OK labels. Titles and messages are plain text.
Only explicit approval returns true; cancel, close, missing host and host disposal
return false. Concurrent requests are queued, and stale answers cannot approve
another request. This is a UI service, separate from background notifications.
Module code must recheck that its view/source still exists before performing the
action after awaiting confirmation. Server-side action policies still apply.
