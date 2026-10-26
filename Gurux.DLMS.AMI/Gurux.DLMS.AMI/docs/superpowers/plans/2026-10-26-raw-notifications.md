# Raw notifications implementation plan

**Goal:** Implement the approved raw notification contracts in the server repository and controller.
**Architecture:** Store transport chunks using the existing ORM. Serialize mutations within the process and use serializable database transactions for cross-process safety. Scope every operation to accessible receiving agents and validate processor access. Keep connection chunks together across leases and retries.
**Spec:** Shared/DIs/IRawNotificationRepository.cs and Shared/Rest/GXRawNotification.cs in the sibling Gurux.DLMS.AMI.Shared project.

- [x] Add SQLite integration checks for idempotent ingestion, conflicting delivery, ordering, concurrent claims, stale lease rejection, additional chunks, completion, retry and retention.
- [x] Implement RawNotificationRepository with server-owned lease fields and connection-level transitions, including validation and authorization.
- [x] Implement RawNotificationController endpoints matching REST contracts; validate inputs and return useful status codes for invalid leases.
- [x] Register the repository and raw notification policies; confirm schema discovery includes the DTO.
- [x] Build the server and run integration checks; inspect the final changes.

Keep existing unrelated working-tree edits. Do not commit or change the agent receiver/parser in this task.

Validation: `dotnet run --project Tests/RawNotifications/RawNotifications.Checks.csproj --no-restore -v quiet` passed. `dotnet build Gurux.DLMS.AMI/Gurux.DLMS.AMI.csproj --no-restore -v quiet` succeeded with 0 errors (162 warnings in the wider application). Independent review completed; expired connection restart regression fixed and verified.

Rulings: Ingestion requires consecutive chunks starting at zero, so parsing cannot consume bytes out of transport order. Lease timestamps use the ORM's persisted millisecond precision; renewal never shortens an existing lease. Raw notifications use dedicated permissions, with all soft-deleted group membership links excluded. Existing RestartAsync is implemented as contracted; no new restart HTTP contract was introduced.
