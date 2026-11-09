# --------------------------------------------------------------------------
# Gurux Ltd
#
# Copyright (c) Gurux Ltd.
#
# This file is part of Gurux Device Framework.
#
# Commit: $Format:%H$
# Date:   $Format:%cI$
#
# This software is available under either of the following licenses:
#
# 1. GNU General Public License, version 2 only (GPL-2.0-only).
#    See the LICENSE file included with this distribution.
#
# 2. A commercial license granted by Gurux Ltd under a separate written
#    agreement. Use under this option requires a valid commercial license
#    and is governed by the terms of that agreement.
#
# If you have not obtained a commercial license from Gurux Ltd,
# use of this software is governed by GPL-2.0-only.
#
# Commercial licensing: https://www.gurux.fi/
#
# SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
# --------------------------------------------------------------------------

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/../scripts/agent-release.ps1"
$now = [DateTimeOffset]'2026-11-03T12:00:00Z'
function Check($actual, $expected) {
    if ($actual -cne $expected) { throw "Expected '$expected', got '$actual'." }
}
function Reject([scriptblock]$action) {
    $failed = $false
    try { & $action } catch { $failed = $true }
    if (-not $failed) { throw 'Expected invalid release selection to fail.' }
}
Check (Get-AgentReleaseVersion -Now $now) '4.1.2611.301'
Check (Get-AgentReleaseVersion -Now $now -Tags @('Gurux.DLMS.AMI.Agent/v4.1.2611.301', 'Gurux.DLMS.AMI.Agent/v4.1.2611.305')) '4.1.2611.306'
Check (Get-AgentReleaseVersion -Now $now -Tags @('Gurux.DLMS.AMI.Shared/v4.1.2611.999', 'Gurux.DLMS.AMI.Agent.Worker/v4.1.2611.900')) '4.1.2611.301'
Check (Get-AgentReleaseVersion -Now $now -CommitTags @('Gurux.DLMS.AMI.Agent/v4.1.2610.3101') -Tags @('Gurux.DLMS.AMI.Agent/v4.1.2611.999')) '4.1.2610.3101'
Check (Get-AgentReleaseVersion -Now $now -Tags @('Gurux.DLMS.AMI.Agent/v4.1.2610.3101')) '4.1.2611.301'
Check (Get-AgentReleaseVersion -Now $now -Tags @('Gurux.DLMS.AMI.Agent/v4.0.2610.3101')) '4.1.2611.301'
Check (Get-AgentReleaseVersion -Now $now -RequestedVersion '4.1.2611.307') '4.1.2611.307'
Reject { Get-AgentReleaseVersion -Now $now -RequestedVersion '4.1.2611.301' -Tags @('Gurux.DLMS.AMI.Agent/v4.1.2611.301') }
Check (Get-AgentReleaseVersion -Now $now -RequestedVersion '4.1.2611.301' -Tags @('Gurux.DLMS.AMI.Agent/v4.1.2611.301') -CommitTags @('Gurux.DLMS.AMI.Agent/v4.1.2611.301')) '4.1.2611.301'
Reject { Get-AgentReleaseVersion -Now $now -RequestedVersion '../escape' }
Reject { Get-AgentReleaseVersion -Now $now -RequestedVersion '4.1.2611.65535' }
Reject { Get-AgentReleaseVersion -Now $now -Tags @('Gurux.DLMS.AMI.Agent/v4.1.2611.65534') }
Reject { Get-AgentReleaseVersion -Now $now -Tags @('Gurux.DLMS.AMI.Agent/v4.2.2611.301') }
Write-Host 'Agent release version checks passed.'
Check (Get-AgentReleaseVersion -ProductId 'Gurux.DLMS.AMI' -Now $now -Tags @('Gurux.DLMS.AMI.Agent/v4.1.2611.999', 'Gurux.DLMS.AMI/v4.1.2611.301')) '4.1.2611.302'
Check (Get-AgentReleaseVersion -ProductId 'Gurux.DLMS.AMI' -Now $now -CommitTags @('Gurux.DLMS.AMI/v4.1.2610.3101')) '4.1.2610.3101'

# Stub only the remote GitHub boundary; exercise the real publisher and checksum validation.
$testDirectory = Join-Path ([IO.Path]::GetTempPath()) ('ami-agent-release-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDirectory | Out-Null
$manifest = [pscustomobject]@{ repository = 'Gurux/Gurux.DLMS.AMI4'; version = '4.1.2611.301';
    tag = 'Gurux.DLMS.AMI.Agent/v4.1.2611.301'; package = 'agent.zip'; checksum = 'agent.zip.sha256' }
$package = Join-Path $testDirectory $manifest.package
$checksum = Join-Path $testDirectory $manifest.checksum
'original package' | Set-Content -LiteralPath $package
'release notes' | Set-Content -LiteralPath (Join-Path $testDirectory 'notes.md')
$digest = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash.ToLowerInvariant()
"$digest  agent.zip" | Set-Content -LiteralPath $checksum
$script:remoteRelease = $null
$script:calls = [Collections.Generic.List[string]]::new()
$script:failUpload = $false
$script:failDispatch = $false
function gh {
    $script:calls.Add(($args -join ' '))
    $global:LASTEXITCODE = 0
    if ($args[0] -eq 'api' -and $args -contains '--slurp') {
        if ($script:remoteRelease) { return ConvertTo-Json -InputObject @(@($script:remoteRelease)) -Depth 8 -Compress }
        return '[[]]'
    }
    if ($args[0] -eq 'release' -and $args[1] -eq 'create') {
        $script:remoteRelease = @{ tag_name = $manifest.tag; draft = $true; assets = @() }
    } elseif ($args[0] -eq 'release' -and $args[1] -eq 'upload') {
        if ($script:failUpload) { $global:LASTEXITCODE = 1; return }
        $script:remoteRelease.assets = @(
            @{ name = 'agent.zip'; state = 'uploaded'; size = 17 },
            @{ name = 'agent.zip.sha256'; state = 'uploaded'; size = 76 })
        if ($args -contains (Join-Path $testDirectory 'agent.zip.manifest.json')) {
            $script:remoteRelease.assets += @{ name = 'agent.zip.manifest.json'; state = 'uploaded'; size = 500 }
        }
    } elseif ($args[0] -eq 'release' -and $args[1] -eq 'edit') {
        $script:remoteRelease.draft = $false
    } elseif ($args[0] -eq 'api' -and $args -contains 'POST') {
        $script:dispatchToken = $env:GH_TOKEN
        if ($script:failDispatch) { $global:LASTEXITCODE = 1 }
    } else { throw "Unexpected GitHub operation: $args" }
}

$savedToken = $env:GH_TOKEN
$savedCatalogToken = $env:CATALOG_TOKEN
try {
    'wrong checksum' | Set-Content -LiteralPath $checksum
    Reject { Publish-AgentRelease -Manifest $manifest -Directory $testDirectory }
    if (@($script:calls | Where-Object { $_ -like 'release *' }).Count) { throw 'Invalid ZIP must never create a release.' }
    "$digest  agent.zip" | Set-Content -LiteralPath $checksum
    $script:calls.Clear()
    $script:failUpload = $true
    Reject { Publish-AgentRelease -Manifest $manifest -Directory $testDirectory }
    if (-not $script:remoteRelease.draft) { throw 'A failed upload must leave the release unpublished.' }
    if (@($script:calls | Where-Object { $_ -like 'release edit *' }).Count) { throw 'A failed upload must not publish or notify.' }
    $script:failUpload = $false
    $script:calls.Clear()
    Publish-AgentRelease -Manifest $manifest -Directory $testDirectory
    if ($script:remoteRelease.draft) { throw 'A complete release must be published.' }
    if (@($script:calls | Where-Object { $_ -like 'release create *' }).Count) { throw 'Retry must reuse the draft.' }
    $script:calls.Clear()
    'different local build' | Set-Content -LiteralPath $package
    Publish-AgentRelease -Manifest $manifest -Directory $testDirectory
    if (@($script:calls | Where-Object { $_ -like 'release *' }).Count) { throw 'A published release must never be changed on rerun.' }
    $env:GH_TOKEN = 'source-repository-token'
    $env:CATALOG_TOKEN = ''
    Reject { Send-AgentCatalogNotification -Manifest $manifest }
    $env:CATALOG_TOKEN = 'catalog-test-token'
    $script:failDispatch = $true
    Reject { Send-AgentCatalogNotification -Manifest $manifest }
    Check $env:GH_TOKEN 'source-repository-token'
    $script:failDispatch = $false
    Send-AgentCatalogNotification -Manifest $manifest
    Check $env:GH_TOKEN 'source-repository-token'
    $env:CATALOG_TOKEN = " `t`r`ncatalog-test-token`r`n "
    Send-AgentCatalogNotification -Manifest $manifest
    Check $script:dispatchToken 'catalog-test-token'
    Check $env:GH_TOKEN 'source-repository-token'
    foreach ($invalidToken in @("catalog-canary`ninvalid", 'Bearer catalog-canary', ('catalog-canary' + [char]1 + 'invalid'), ('catalog-canary' + [char]0xFEFF))) {
        $env:CATALOG_TOKEN = $invalidToken
        $before = $script:calls.Count
        $failure = $null
        try { Send-AgentCatalogNotification -Manifest $manifest } catch { $failure = $_ }
        if ($null -eq $failure -or $script:calls.Count -ne $before) { throw 'Malformed catalog token reached GitHub CLI.' }
        if ($failure.Exception.Message.Contains('catalog-canary')) { throw 'Invalid token validation exposed credentials.' }
        Check $env:GH_TOKEN 'source-repository-token'
    }
    $env:CATALOG_TOKEN = 'catalog-test-token'
    $script:remoteRelease.assets = @()
    Reject { Publish-AgentRelease -Manifest $manifest -Directory $testDirectory }
    $manifest | Add-Member NoteProperty releaseManifest 'agent.zip.manifest.json'
    $script:remoteRelease = $null
    Reject { Publish-AgentRelease -Manifest $manifest -Directory $testDirectory }
    $signedPath = Join-Path $testDirectory $manifest.releaseManifest
    $newDigest = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash.ToLowerInvariant()
    "$newDigest  agent.zip" | Set-Content -LiteralPath $checksum
    @{ version = $manifest.version; sha256 = $newDigest; signature = 'test-signature' } | ConvertTo-Json | Set-Content $signedPath
    $script:calls.Clear()
    Publish-AgentRelease -Manifest $manifest -Directory $testDirectory
    if ($script:remoteRelease.draft -or -not @($script:calls | Where-Object { $_ -like '*release upload*agent.zip.manifest.json*' }).Count) {
        throw 'Signed metadata must be uploaded before publishing.'
    }
    $script:remoteRelease.assets = @($script:remoteRelease.assets | Where-Object name -ne 'agent.zip.manifest.json')
    Reject { Publish-AgentRelease -Manifest $manifest -Directory $testDirectory }
    Write-Host 'Agent draft/upload/publish, immutable rerun and dispatch retry checks passed.'
} finally {
    $env:GH_TOKEN = $savedToken
    $env:CATALOG_TOKEN = $savedCatalogToken
    Remove-Item Function:\gh
    foreach ($file in @($package, $checksum, (Join-Path $testDirectory 'notes.md'), (Join-Path $testDirectory 'agent.zip.manifest.json'))) {
        if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file }
    }
    Remove-Item -LiteralPath $testDirectory
}
