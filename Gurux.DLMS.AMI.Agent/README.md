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

1. Extract the agent release ZIP into a directory writable by the account that will run the agent.
2. If using automatic application updates, run `pwsh ./Install-Agent.ps1`. This installs the bundled updater into a separate sibling directory and configures `Updater:Executable` in `appsettings.json`. It does not register a Windows or systemd service. Release packages contain the updater payload; a normal source publish does not unless `UpdaterPayloadDirectory` is supplied.
3. Start the agent under its intended runtime account:

   ```powershell
   dotnet ./Gurux.DLMS.AMI.Agent.dll -h https://your-ami-server.example
   ```

On first start, the agent prompts for the AMI address, agent name and a personal access token. The token determines which meters the agent can access. Subsequent starts reuse the saved registration. To register again explicitly, use `dotnet ./Gurux.DLMS.AMI.Agent.dll Configure`.

If the AMI server rejects the saved token with HTTP 401, the agent stops its current runtime. An interactive console offers a replacement PAT (press Enter to exit); the agent keeps its identity and saves the replacement only after startup succeeds. Without interactive console input, the agent exits with an error. Stop the service and start the agent interactively under the same account to replace its PAT, then restart the service. Use `Configure` only when registering a new agent. The saved registration is retained. HTTP 403 does not trigger PAT replacement.

If the saved agent ID is missing or inaccessible on the server (HTTP 404 from the agent lookup), startup exits with a registration error instead of retrying. Check the server address and access rights, or run `Configure` to register again. Existing settings and PAT are retained until registration succeeds. This requires a server version that reports missing resources as HTTP 404; an older server returning HTTP 500 is handled as a transient server failure.

Use Ctrl+C or, in an interactive console, Escape to request a graceful stop. Linux services also handle SIGTERM. A service must use the same account and persistent storage paths as registration. Two local processes cannot use the same settings file or registered agent identity concurrently.

Containers are updated by replacing their image. Skip `Install-Agent.ps1` in containers and persist the runtime account's local application data, including both registration and task completions, across container replacement.

## Configuration and persistent data

Configuration is loaded from `appsettings.default.json`, then optional `appsettings.json`, then environment variables. Use `__` in environment variable names, for example `Completions__MinFreeBytes`. Put deployment overrides in `appsettings.json`; keep the shipped defaults separate.

Registration is stored separately in `Gurux/DLMS.AMI/agent/settings.json` below the runtime account's `Environment.SpecialFolder.LocalApplicationData` directory (normally `%LOCALAPPDATA%` on Windows). Override this with `Agent:SettingsPath` or `GURUX_AGENT_SETTINGS`. A legacy installation-directory `settings.json` is imported when using the default path and no registration exists there yet. Registration contains the access token and is restricted to the runtime account; keep it out of release packages and source control.

Task results use `Gurux/DLMS.AMI/task-completions/<server-hash>/<agent-id>` below the same local application data directory. The server hash is the first 16 hexadecimal characters of the SHA-256 hash of the configured AMI address; the agent ID has no hyphens. If local application data is unavailable, this queue falls back to the application directory. Changing the registration settings path does not relocate the completion queue. Preserve the account, address and agent ID when recovering an installation so the agent finds its existing queue.

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

Automatic updates require an external Gurux.Updater installation and a trusted RSA release public key in `Updater:PublicKeyPem`. Set the absolute `Updater:Executable` path if not configured by `Install-Agent.ps1`. For a Windows or Linux service installation, also set `Updater:ServiceName` and `Updater:ApplicationPath` to the installed agent application. Standalone automatic restart requires a published executable; a portable DLL installation requires a service name.

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
pwsh .github/tests/agent-package.tests.ps1
```

The checks use temporary storage and controlled HTTP responses; they do not require live meters or an AMI server. They cover completion retry/restart, storage admission, corrupt records, storage-blocked shutdown/recovery and update validation. Run only the new monitoring checks with `dotnet run --project Tests/AgentApplication/AgentApplication.Checks.csproj -c Release -- completion-monitoring`.

Run the basic unit checks alone with `dotnet run --project Tests/AgentApplication/AgentApplication.Checks.csproj -c Release -- unit`. They exercise configuration validation, snapshot isolation, diagnostic redaction, serial capabilities, host notifications, retry delays and completion limit validation without network or file-system access. They also run as part of the full application checks and the existing release workflow.

Run the supplied IPv4 gateway identification script checks with `dotnet run --project Tests/AgentApplication/AgentApplication.Checks.csproj -c Release -- identify-by-ip`. These compile and execute `IdentifyByIp` with the AMI script runner and agent gateway repository, replacing only the HTTP server boundary. They verify lookup by IP without the source port, creation of an unknown gateway, and reuse across different source ports. The script expects an IPv4 `address:port` sender string. These checks also run in the full application suite.

Run the received-string gateway identification script checks with `dotnet run --project Tests/AgentApplication/AgentApplication.Checks.csproj -c Release -- identify-by-data`. These compile and execute the supplied `Identify` method through the same runner and repository. They verify lookup using ASCII-decoded data, preservation of existing gateways, creation and reuse of unknown gateways, and identification independently of sender IP. Two different identifiers are tested from the same sender, and each identifier is repeated from a different IP. These checks also run in the full application suite.

Publishing without a runtime identifier preserves the portable release name `Gurux.DLMS.AMI.Agent-<version>.zip`. A publish with `-r win-x64` or `-r linux-arm64` produces `Gurux.DLMS.AMI.Agent-<version>-win-x64.zip` or `Gurux.DLMS.AMI.Agent-<version>-linux-arm64.zip` under `Gurux.DLMS.AMI.Agent/bin/<configuration>`. Use separate publish directories for separate runtime identifiers. Portable release automation continues to use the portable filename.

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
