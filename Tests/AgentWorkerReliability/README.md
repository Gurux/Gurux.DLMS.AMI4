# Agent worker reliability checks

Run from the repository root:

```powershell
dotnet run --project Tests/AgentWorkerReliability/AgentWorkerReliability.csproj
```

Checks reference the unified Agent project and use its actual runtime code, fake HTTP endpoints, real TCP sockets and a compiled identification script. They cover independent workers, registration ownership, queue backpressure and replacement, cancellation of pending requests, bounded connection admission, gateway replacement and status ordering, repeated start/stop, blocked transport closure, and cleanup of an identification callback after the caller stops waiting for shutdown. Verbose tracing is exercised with an asynchronous identification script that sends through its transport. See the [application and updater documentation](../../Gurux.DLMS.AMI.Agent/README.md) for deployment details.

`GXAgentOptions.MaxIncomingConnections` is a positive limit per auto-connect/gateway listener (default 256). One replacement can wait for an existing configured gateway owner; it starts after its predecessor's complete cleanup. Reader thread settings still control the polled meter queue. The public concrete worker `client` and `Options` are now instance members; HTTP injection uses `new GXAgentWorker(httpClient)`. The worker does not dispose a supplied HTTP client. `IGXAgentWorker` and its default constructor remain available to the host.

Shutdown cancellation limits the caller's wait. Services retain their cleanup task and finish active scripts/callbacks and transport closure before dependencies can be released. Scripts that ignore cancellation must finish before cleanup finishes. Final task acknowledgments and gateway status reporting have independent five-second deadlines.

## Installed transport limitation

Gurux.Net 8.4.2604.201 does not emit a disconnect/state event for a clean peer TCP FIN after `GXNet.Attach` switches the connection into client mode. Its receive callback returns on a zero-byte receive without closing the media; `IsOpen` only reports whether its socket reference exists. There is no safe public peer-EOF probe. This preexisting transport behavior needs a vendor fix; these worker changes do not inspect private socket state or consume protocol data to detect it. Gateway replacement tests use real sockets and explicitly inject the corresponding disconnect event when checking this event contract. Worker-driven close, cancellation, received disconnect/state events and replacement all clean up the owned session.

## Connected meter task draining

After each task, the worker checks `/api/Task/Next` with the current device identifier and zero wait time. Follow-up tasks use the existing transport and DLMS association. A concurrent general poller hands already-claimed work to that active meter session rather than scheduling another connection. On an empty queue, the worker closes/disposes its owned transport; incoming meter/gateway wrappers also exit when their task source is empty. Gateway wrappers can drain other mapped meters before closing the gateway transport.

Completion uploads include the original task `Id`, claim `Start`, `Result`, and `Data`. Reports are written atomically to `%LOCALAPPDATA%/Gurux/DLMS.AMI/task-completions/<server-hash>/<agent-id>/` before upload. Failed uploads remain on disk, are retried by the owning worker, and are recovered by the next worker for that server/agent. Only completion HTTP requests are retried; device writes/actions are not replayed. Background scans run every two seconds, with per-record exponential backoff, Retry-After support and rotating fairness across five-second flush deadlines. Corrupt/permanently rejected records are retained in quarantine. Storage admission limits pause new claims on every transport; already-claimed tasks retain their outcomes. Failed persistence keeps results in memory and delays graceful shutdown/update until storage recovers. See the Agent README for thresholds, metrics and operator recovery.

Protocol receive timeouts are converted from seconds to milliseconds once. Receives use 250 ms slices while preserving their total deadline; a configured zero timeout remains an infinite total wait that cancellation can interrupt. Firmware and retry delays also observe cancellation. Media allocated by the worker is closed/disposed even if setup fails or the operation is canceled; borrowed incoming media remains owned by its incoming-session wrapper.

Focused regressions can also be run with `-- meter-session` and `-- completion-outbox`. The default run includes both groups. They cover real DLMS WRAPPER write responses, same-session continuation, a lost completion response without a duplicate write, poller handoff during execution/setup failure, empty incoming TCP sessions, finite/infinite receive cancellation, and completion recovery from disk.
