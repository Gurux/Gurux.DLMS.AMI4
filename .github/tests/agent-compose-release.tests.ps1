$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/../scripts/agent-compose-release.ps1"
$version = '4.1.2611.1001'
$digest = 'sha256:' + ('a' * 64)
$value = New-AgentComposeMetadata -Version $version -Digest $digest
if (-not $value.compose.maintenanceRequired -or $value.compose.containsWasm -or $null -ne $value.compose.rollbackCompatibleFrom)
{
    throw 'An unaudited agent release must require maintenance and contain no AMI browser.'
}
if ($value.compose.images.agent -cne "guruxorg/dlms-ami-agent@$digest")
{
    throw 'The agent image must use an immutable official registry digest.'
}
$value = New-AgentComposeMetadata -Version $version -Digest $digest -RollbackCompatibleFrom '[4.1.2611.1,4.1.2611.1001]'
if ($value.compose.maintenanceRequired)
{
    throw 'An audited bounded range must enable compatible agent updates.'
}
foreach ($bad in @('latest', '*', 'sha256:abc'))
{
    $rejected = $false
    try { New-AgentComposeMetadata -Version $version -Digest $bad | Out-Null } catch { $rejected = $true }
    if (-not $rejected)
    {
        throw 'Mutable or malformed image references must be rejected.'
    }
}
Write-Host 'PASS immutable agent Compose metadata and audited rollback policy.'
