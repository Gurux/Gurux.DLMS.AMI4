# Gurux.DLMS.AMI.Agent.Shared

Shared settings and worker interfaces for agents in Gurux DLMS AMI v4.
This .NET class library provides contracts used by the agent host and worker implementation.

## Contents

- `GXAgentOptions`: validated server connection, authentication, version, selected serial port, and worker settings.
- `GXAgentOptionsSnapshot`: a deep validated snapshot; `ToOptions()` returns a detached editable copy.
- `GXAgentCapabilities`: an immutable snapshot of discovered serial ports, separate from configuration.
- `IGXAgentWorker`: service registration, initialization, cancellable registration/start/stop and asynchronous disposal.
- `IGXAgentHostNotifications` and `GXAgentHostNotifications`: asynchronous, cancellable version/settings change notifications.
- `GXAgentContract`: validates worker assembly contract metadata before constructing the worker.
- `GXAgentWorkerState` and `GXAgentConnectionState`: independent lifecycle and notification connection state.
- `GXAgentValidationError`: a stable error code, property path and safe diagnostic message.

Worker settings cover meter reading, incoming meter connections, notifications,
stored raw notification processing, and gateway connections.
The settings types are provided by `Gurux.DLMS.AMI.Shared`.

## Build

XML API documentation is generated during the build.
This library is used by other projects; it is not a standalone application.

## Configuration

Use `GXAgentOptions` to supply the AMI server address and a personal access token,
then configure the workers needed by the agent. Keep access tokens out of source control.
The agent host and worker implementation handle startup and communication.

Call `options.Validate(requireAgentId: false)` before registering a new agent and
`options.Validate()` before starting an existing agent. Invalid enabled worker settings
produce errors with property paths. Disabled workers do not require transport settings.
`GXAgentOptions.ToString()` provides a safe summary without tokens, URL credentials or
nested transport credentials. JSON serialization still includes the token because it is
required to restore authentication; protect the settings file.

Validation throws `GXAgentOptionsValidationException`, whose `Errors` preserve the same
structured errors returned by `GetValidationErrors()`. Use `Code` and `PropertyPath` for
UI localization and tests, for example `InvalidThreadCount` at `ReaderSettings.Threads`.
Do not parse the English `Message` to identify a validation rule.

The host edits/deserializes `GXAgentOptions`; the worker captures its own deep validated
copy on initialization. Published configuration is available as `worker.Configuration`.
Nested settings and script byte arrays are copied too. `ToOptions()` and the legacy-style
`GXAgentWorker.Options` getter return independent copies, so modifying them cannot change
active configuration. Inside the worker, writers serialize updates and atomically publish
a whole new snapshot. Readers can retain an old snapshot safely while an update is applied.
Persist `worker.Configuration.ToOptions()` after a settings/version notification instead of
persisting the original object passed to `Init`.

## Worker lifecycle and ownership

1. Create one worker instance and call `ConfigureServices(IServiceCollection)` once.
2. The host builds the service provider and calls `Init(provider, options, notifications)` once.
3. Register if necessary, then call `StartAsync(hostCancellationToken)`.
4. Await `notifications.WaitForChangeAsync(hostCancellationToken)`; a result can contain
   both `VersionAvailable` and `SettingsChanged`. Persist settings before reloading a worker.
5. Call `StopAsync(shutdownDeadlineToken)`, then `DisposeAsync()` on the worker, then
   dispose the provider. Dispose notifications after all workers have stopped.

Repeated/concurrent starts share one startup operation. Stop and disposal are idempotent.
A stopped worker is terminal: create a new instance for a restart or version change.
Cancellation of `StopAsync` limits the caller's wait while cleanup continues; do not unload
the worker assembly or dispose its provider until cleanup finishes. The worker borrows the
provider and notifications; it never disposes them. The current implementation supports
one active worker per process because existing meter integrations share runtime state.

The host handles Ctrl+C through cancellation and awaits shutdown. Worker version changes
use a new instance and release the old provider before unloading its assembly context.

## Compatibility

Deploy Agent.Shared, Worker and Agent together: the worker interface has changed and is not
binary-compatible with previously published workers. No server DTO change is required.
Public contracts now consistently use `GX` prefixes (`AgentOptions`, `AgentCapabilities`
and `AgentHostChange` were renamed); JSON configuration property names remain unchanged.

Worker assemblies declare `[assembly: GXAgentContractVersion(GXAgentContract.Major,
GXAgentContract.Minor)]`. The host checks this metadata before constructing any worker.
Missing metadata, a different major version or a minimum required host minor version above
the host's version causes an actionable compatibility error. A major increment represents
a binary-breaking interface change; minor increments must preserve binary compatibility.

`State` reports `Created`, `Initialized`, `Starting`, `Running`, `RestartPending`, `Stopping`,
`Stopped`, `Faulted` or `Disposed`. `ConnectionState` separately reports `Disconnected`,
`Connecting`, `Connected` or `Reconnecting`. Losing the SignalR connection keeps a running
worker in `Running` while reconnection proceeds. Rejected invalid settings preserve the
previous snapshot; a failure applying already published service settings reports `Faulted`.
Older settings JSON containing `SerialPorts` is accepted, but that discovered list is no
longer persisted. `SerialPort` remains the selected port. Server status messages retain
their existing semicolon-delimited serial-port format.

## License

GNU General Public License v2.0 only (`GPL-2.0-only`).
See the license in the repository and the source file headers.
