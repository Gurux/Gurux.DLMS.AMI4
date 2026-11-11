# Gurux.DLMS.AMI.Agent

Gurux.DLMS.AMI.Agent reads DLMS/COSEM meters and works together with the **Gurux.DLMS.AMI4** service. The agent connects to meters, executes tasks received from the service, and sends readings and task results back to it.

The agent can run close to the meters, such as on a local computer or a gateway, while the AMI service provides centralized management. This makes it useful for automated meter reading, collecting consumption data, and monitoring meters across multiple locations.

With the agent and the AMI service, you can:

- Read meter values, such as energy consumption, voltage, and current.
- Collect load profiles and other historical data stored in meters.
- Execute scheduled and on-demand reading tasks.
- Change meter settings and execute supported DLMS/COSEM methods.
- Receive meter notifications and forward the information to the service.
- Monitor communication errors and task results.

Meters can be accessed over network or serial connections, depending on the installation. Available readings and operations depend on the meter, its configuration, and the access rights assigned to the connection.

The agent is intended to be used as part of a Gurux.DLMS.AMI4 installation. Meter configuration and reading tasks are managed through the AMI service.

## Install and start

The portable release requires the .NET 10 runtime. Building from source requires the .NET 10 SDK. The bundled updater installer requires PowerShell 7.2 or newer.

1. Extract the agent release ZIP into a directory writable by the account that will run the agent. It contains:

   ```text
   gx-agent/
     start.sh
     start.bat
     agent/
     gurux-updater/
   ```

2. Start the agent under its intended runtime account from `gx-agent`:

   ```powershell
   .\start.bat -h https://your-ami-server.example
   ```

   On Linux, use `sh ./start.sh -h https://your-ami-server.example`. Both scripts locate `agent/` relative to the script, forward arguments and preserve the agent exit code. The shell launcher supports the repository's CRLF line endings when invoked through `sh`.
3. Automatic updates use the bundled sibling `gurux-updater/Gurux.Updater.Tool.dll` when neither `Updater:Executable` nor `GURUX_UPDATER_PATH` is set. No global tool installation or setup step is needed for release ZIPs. A source publish includes an updater only when `UpdaterPayloadDirectory` is supplied.

On first start, the agent prompts for the AMI address, agent name and a personal access token. The token determines which meters the agent can access. Subsequent starts reuse the saved registration. To register again explicitly, pass `Configure` to the launcher, or run `dotnet ./Gurux.DLMS.AMI.Agent.dll Configure` from `agent/`.

If the AMI server rejects the saved token with HTTP 401, the agent stops its current runtime. An interactive console offers a replacement PAT (press Enter to exit); the agent keeps its identity and saves the replacement only after startup succeeds. Without interactive console input, the agent exits with an error. Stop the service and start the agent interactively under the same account to replace its PAT, then restart the service. Use `Configure` only when registering a new agent. The saved registration is retained. HTTP 403 does not trigger PAT replacement.

If the saved agent ID is missing or inaccessible on the server (HTTP 404 from the agent lookup), startup exits with a registration error instead of retrying. Check the server address and access rights, or run `Configure` to register again. Existing settings and PAT are retained until registration succeeds. This requires a server version that reports missing resources as HTTP 404; an older server returning HTTP 500 is handled as a transient server failure.

Use Ctrl+C or, in an interactive console, Escape to request a graceful stop. Linux services also handle SIGTERM. A service must use the same account and persistent storage paths as registration. Two local processes cannot use the same settings file or registered agent identity concurrently.

Containers are updated by replacing their image. Persist the runtime account's local application data, including both registration and task completions, across container replacement.

## Configuration and persistent data

Configuration is loaded from `appsettings.default.json`, then optional `appsettings.json`, then environment variables. Use `__` in environment variable names, for example `Completions__MinFreeBytes`. Put deployment overrides in `appsettings.json`; keep the shipped defaults separate.

Registration is stored separately in `Gurux/DLMS.AMI/agent/settings.json` below the runtime account's `Environment.SpecialFolder.LocalApplicationData` directory (normally `%LOCALAPPDATA%` on Windows). Override this with `Agent:SettingsPath` or `GURUX_AGENT_SETTINGS`. A legacy installation-directory `settings.json` is imported when using the default path and no registration exists there yet. Registration contains the access token and is restricted to the runtime account; keep it out of release packages and source control.

Task results use `Gurux/DLMS.AMI/task-completions/<server-hash>/<agent-id>` below the same local application data directory. The server hash is the first 16 hexadecimal characters of the SHA-256 hash of the configured AMI address; the agent ID has no hyphens. If local application data is unavailable, this queue falls back to the application directory. Changing the registration settings path does not relocate the completion queue. Preserve the account, address and agent ID when recovering an installation so the agent finds its existing queue.

### Raw notification delivery

When notification pass-through is enabled, transport chunks are saved privately before the receive callback returns. Upload retries preserve each chunk's identifier and stream sequence. The existing AMI raw notification API verifies duplicate identifiers; an acknowledgement lost in transit does not create a second record.

The spool is under `Gurux/DLMS.AMI/raw-notifications/<server-hash>/<agent-id>/<interface>` in the runtime account's local application data directory, falling back to the application directory if local application data is unavailable. The hash uses the first 16 lowercase hexadecimal SHA-256 characters of the HTTP client's absolute server URI. Preserve this storage alongside registration and task completions during upgrades or container replacement. On restart, unfinished streams receive a persisted end marker after their retained chunks.

`RawDelivery` settings control `MaxPendingRecords` (10,000), `MaxPendingBytes` (256 MiB), `MinFreeBytes` (64 MiB), `RequestTimeout` (five seconds), `RetryDelay` (one second) and `ShutdownFlushBudget` (five seconds). Intake waits when storage admission limits are reached; stream end markers may exceed admission thresholds so existing streams can close. UDP senders cannot observe this backpressure and packets may be lost outside the agent when the receiver is blocked. Restore disk capacity or archive resolved quarantine records to resume intake.

HTTP 400, 410, 413 and 422 quarantine the affected stream, including subsequent chunks, while other streams continue. Corrupt or inconsistent local streams are also quarantined. Inspect the retained `reason.txt` and resolve the result with the server before any manual replay; a partial stream may already have reached the parser. Authentication, network and server errors remain retryable, and throttling honours `Retry-After`. Shutdown has a bounded upload flush; unacknowledged records remain on disk for the next start.

### Script execution deadlines

`Scripts:ExecutionTimeout` defaults to `00:00:30` and must be positive and at most one day. The deadline covers the invocation wait, including synchronous entry points and nested `Task`, `ValueTask` and `ValueTask<T>` results. Connection and worker cancellation are passed to the script runner. Long-running scripts should use the runner's cancellation token cooperatively.

An in-process deadline cannot forcibly terminate arbitrary script code. If code continues after cancellation or timeout, the agent retains its context and pauses new script execution until that code and its cancellation callbacks finish. Context unload runs asynchronously; an unload callback exceeding the same deadline also pauses admission until cleanup finishes. The raw notification processor pauses new claims during these periods. Restart the process to recover from a script that never finishes; review possible late script side effects before retrying an operation. Guaranteed forced termination requires process isolation.

### Runtime metrics

The `Gurux.DLMS.AMI.Agent.Runtime` .NET meter exposes `agent.connections.active` (registered meter sessions), `agent.reads.active`, `agent.queue.pending`, `agent.queue.wait`, `agent.reads.duration`, `agent.tasks.duration`, `agent.tasks.completed` and `agent.http.duration`. Durations use seconds. Read/task outcomes are `success`, `error` and `cancelled`; HTTP outcomes distinguish `authorization`, `throttled`, `server`, `rejected`, `network`, `cancelled` and `success`. HTTP cancellation includes request deadlines. Tags contain no meter identifiers or credentials.

The `Gurux.DLMS.AMI.Agent.RawDelivery` meter exposes `agent.raw.pending`, `agent.raw.pending_bytes`, `agent.raw.oldest_seconds`, `agent.raw.quarantined` and `agent.raw.retries`. Raw pending bytes include spool metadata and quarantine storage. Attach a .NET metrics collector to export these measurements.

### Completion storage limits

| Setting | Default | Effect |
| --- | --- | --- |
| `Completions:MaxPendingRecords` | `10000` | Pause new task claims at this many queued and quarantined records. |
| `Completions:MaxPendingBytes` | `268435456` (256 MiB) | Pause claims at this total size, including quarantine and results awaiting persistence. |
| `Completions:MinFreeBytes` | `67108864` (64 MiB) | Pause claims below this free disk space. |
| `Completions:RequestTimeout` | `00:00:05` | Limit an individual acknowledgement request. |
| `Completions:FlushBudget` | `00:00:05` | Limit a pass through acknowledgement retries. |

Admission reads a cached status snapshot. Storage is checked every two seconds independently of HTTP requests, and after report/flush operations. Detection of an external disk failure can therefore lag until the next sample; results that cannot be persisted immediately stop new claims. Previously admitted tasks may still finish. These limits control admission rather than imposing a hard cap on already-running tasks' results.

The `Gurux.DLMS.AMI.Agent.Completions` .NET meter exposes `agent.completions.pending`, `pending_bytes`, `oldest_seconds`, `quarantined`, `free_bytes`, `unpersisted`, `claims_paused` and `shutdown_blocked` (each with the `agent.completions.` prefix). Attach a .NET metrics collector to observe these instruments. Queue status is also logged periodically, with admission transitions logged immediately when detected.

### Recover from storage and network failures

- **AMI connection unavailable:** completed results remain in JSON files and acknowledgements retry with backoff. A runtime restart reloads these files. Retrying an acknowledgement does not execute the meter operation again.
- **Full or unwritable disk:** new claims pause. Restore free space, directory permissions and access to the original queue path. Results that could not be written remain in memory. Keep the process running until `unpersisted` reaches zero.
- **Shutdown blocked by storage:** a critical log names the affected directory and `shutdown_blocked` becomes 1. Shutdown and update handoff wait until every in-memory result has been persisted; shutdown resumes automatically after recovery. A forced process termination or service-manager kill can lose those in-memory results. Allow time for recovery when configuring service stop deadlines.
- **Quarantined results:** malformed local JSON and HTTP 400, 410, 413 or 422 responses move a result into the queue's `quarantine` subdirectory. Quarantined records count toward admission limits and are not retried automatically. Inspect the logged reason and retain a copy. Correct the cause before considering replay; verify server handling of duplicate completions first. With the agent stopped and the record validated, an operator can move its JSON back to the parent queue for retry, or archive it outside the queue after resolving the result manually.
- **Authentication or throttling:** HTTP 401 remains retryable; restore the AMI credentials or permissions. HTTP 429 honours `Retry-After`. Neither response automatically discards a result.

## Automatic application updates

Build and release outputs include `agent-release-public.pem`. The same public key is embedded in the agent and used by default for release signature verification. Set `Updater:PublicKeyPem` only to override this default with an explicitly trusted public key; existing nonblank configuration is preserved. The private signing key is never included in the agent or release. The release signing step rejects a private signing key that does not match the bundled public key.

Automatic updates also require an external Gurux.Updater installation. The selection order is `Updater:Executable`, `GURUX_UPDATER_PATH`, then the bundled sibling `gurux-updater/Gurux.Updater.Tool.dll` resolved from the application directory. For a Windows or Linux service installation, also set `Updater:ServiceName` and `Updater:ApplicationPath` to the installed agent application. Standalone updates support both a published executable and the portable DLL. Gurux.Updater starts DLL applications through the .NET host; no service name is required for a standalone DLL. If ApplicationPath is omitted, the agent prefers its apphost executable when present and otherwise uses Gurux.DLMS.AMI.Agent.dll. This requires a Gurux.Updater.Tool release containing portable-DLL restart support; use that version for the release workflow's GURUX_UPDATER_TOOL_VERSION variable before publishing the agent.

The updater executable and `Updater:StagingDirectory` must be outside the live installation directory. Linux service handoff requires `/usr/bin/systemd-run`. Optional `Updater:HealthUrl` checks the installed application; `VersionUrl` is not supported in local package mode.

Update downloads require HTTPS and a signed release manifest. The agent verifies signature, hash, requested version, archive paths and size limits before handing the package to the external updater. Tasks and result persistence drain before handoff. A launcher failure restores the installed runtime and delays another handoff for five minutes. Check update logs and configuration when automatic updating is unavailable; do not replace the live directory while the agent is still draining.

## Build and verify

From the repository root:

```powershell
dotnet build Gurux.DLMS.AMI.Agent/Gurux.DLMS.AMI.Agent.csproj -c Release
dotnet run --project Tests/AgentApplication/AgentApplication.Checks.csproj -c Release
dotnet run --project Tests/AgentWorkerReliability/AgentWorkerReliability.csproj -c Release
dotnet run --project Tests/AgentMaintenance/AgentMaintenance.csproj -c Release
dotnet run --project Tests/AgentProfileHistory/AgentProfileHistory.csproj -c Release
dotnet run --project Tests/AgentRawDelivery/AgentRawDelivery.csproj -c Release
dotnet run --project Tests/AgentScriptExecution/AgentScriptExecution.csproj -c Release
pwsh .github/tests/agent-package.tests.ps1
```

The checks use temporary storage and controlled HTTP responses; they do not require live meters or an AMI server. They cover completion retry/restart, storage admission, corrupt records, storage-blocked shutdown/recovery and update validation. Run only the new monitoring checks with `dotnet run --project Tests/AgentApplication/AgentApplication.Checks.csproj -c Release -- completion-monitoring`.

Run the basic unit checks alone with `dotnet run --project Tests/AgentApplication/AgentApplication.Checks.csproj -c Release -- unit`. They exercise configuration validation, snapshot isolation, diagnostic redaction, serial capabilities, host notifications, retry delays and completion limit validation without network or file-system access. They also run as part of the full application checks and the existing release workflow.

Run the supplied IPv4 gateway identification script checks with `dotnet run --project Tests/AgentApplication/AgentApplication.Checks.csproj -c Release -- identify-by-ip`. These compile and execute `IdentifyByIp` with the AMI script runner and agent gateway repository, replacing only the HTTP server boundary. They verify lookup by IP without the source port, creation of an unknown gateway, and reuse across different source ports. The script expects an IPv4 `address:port` sender string. These checks also run in the full application suite.

Run the received-string gateway identification script checks with `dotnet run --project Tests/AgentApplication/AgentApplication.Checks.csproj -c Release -- identify-by-data`. These compile and execute the supplied `Identify` method through the same runner and repository. They verify lookup using ASCII-decoded data, preservation of existing gateways, creation and reuse of unknown gateways, and identification independently of sender IP. Two different identifiers are tested from the same sender, and each identifier is repeated from a different IP. These checks also run in the full application suite.

Publishing without a runtime identifier preserves the portable release name `Gurux.DLMS.AMI.Agent-<version>.zip`. A publish with `-r win-x64` or `-r linux-arm64` produces `Gurux.DLMS.AMI.Agent-<version>-win-x64.zip` or `Gurux.DLMS.AMI.Agent-<version>-linux-arm64.zip` under `Gurux.DLMS.AMI.Agent/bin/<configuration>`. All ZIP contents are inside `gx-agent/`; after extraction, run `start.bat` on Windows or `sh ./start.sh` on Linux from that directory. Use separate publish directories for separate runtime identifiers. Portable release automation continues to use the portable filename.

The agent accepts both the `gx-agent/` layout and older flat update ZIPs. After signature and package validation, it prepares a flat installation ZIP for the external updater and passes its SHA-256. Agents released before this layout support must be upgraded manually once before they can accept the new ZIP layout through automatic updates.

## License

This software is available under either of the following licenses:

- **Open-source license:** GNU General Public License, version 2 only
  (GPL-2.0-only). See [LICENSE](../LICENSE) for the full license text.
- **Commercial license:** Available from Gurux Ltd under a separate
  written agreement. This option allows use under the terms of that
  agreement instead of GPL-2.0-only.

A valid commercial license is required to use the commercial licensing
option. See [Gurux Commercial License Notice](../LICENSES/LicenseRef-Gurux-Commercial.txt).

For commercial licensing inquiries, [contact Gurux Ltd](https://www.gurux.fi/).

## Docker with automatic updates

Use the [agent Compose installation](../deployment/docker/README.md#agent) to start the agent and its independent updater together. Configure the normal server address and PAT in `.env`; first registration is automatic and saved in the persistent data volume. No host updater/systemd installation is required. Existing standalone and systemd installation methods remain available.
