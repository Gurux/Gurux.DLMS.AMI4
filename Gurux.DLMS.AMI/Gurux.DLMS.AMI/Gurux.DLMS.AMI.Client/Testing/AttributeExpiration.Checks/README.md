# Attribute expiration UI checks

Run from the AMI repository root:

`pwsh -File Gurux.DLMS.AMI/Gurux.DLMS.AMI/Gurux.DLMS.AMI.Client/Testing/AttributeExpiration.Checks/Invoke-SourceChecks.ps1`

The runner builds Client checks, builds the sibling UI source using the referenced assembly identity, and replaces only the generated test-output UI DLL before executing the checks. This verifies local UI changes while the Client dependency graph still uses its published UI package. It does not publish a package or change application deployment.

The bUnit checks exercise the actual GXInputTimeSpan-backed editor: nullable and zero durations, static mode, DTO edit tracking, compatible stored dates/offsets, disabled editing and invalid-input recovery. Shared DTO storage remains DateTimeOffset?; UI binding uses TimeSpan?. This clock-based DTO supports durations below one day; larger durations are explicitly rejected instead of silently losing the day component.
