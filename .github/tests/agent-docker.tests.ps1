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
. "$PSScriptRoot/../scripts/agent-docker.ps1"
function Check($actual, $expected) { if ($actual -cne $expected) { throw "Expected '$expected', got '$actual'." } }
function Reject([scriptblock]$action) {
    $failed = $false
    try { & $action } catch { $failed = $true }
    if (-not $failed) { throw 'Expected Docker publication to fail.' }
}
$version = '4.1.2611.301'
$commit = '1234567890123456789012345678901234567890'
$script:images = @{}
$script:calls = [Collections.Generic.List[string]]::new()
$script:registryFailure = $false
function docker {
    $script:calls.Add(($args -join ' '))
    $global:LASTEXITCODE = 0
    $reference = $args[2]
    if ($args[0] -eq 'manifest') {
        if ($script:registryFailure) { $global:LASTEXITCODE = 1; return 'unauthorized: authentication required' }
        if (-not $script:images.ContainsKey($reference)) { $global:LASTEXITCODE = 1; return "no such manifest: docker.io/$reference" }
        return '{}'
    }
    if ($args[0] -eq 'pull') { return }
    if ($args[0] -eq 'image') { return $script:images[$reference] | ConvertTo-Json -Compress }
    if ($args[0] -eq 'tag') { $script:images[$args[2]] = $script:images[$args[1]]; return }
    if ($args[0] -eq 'push') { return }
    throw "Unexpected Docker command: $args"
}
try {
    Check (Get-AgentDockerImage -Reference "guruxorg/dlms-ami-agent:$version") $null
    $script:registryFailure = $true
    Reject { Get-AgentDockerImage -Reference "guruxorg/dlms-ami-agent:$version" }
    $script:registryFailure = $false
    $script:images["guruxorg/dlms-ami-agent:$version"] = $null
    $unlabelled = Get-AgentDockerImage -Reference "guruxorg/dlms-ami-agent:$version"
    if ($null -eq $unlabelled) { throw 'An existing unlabelled image must not be mistaken for a missing tag.' }
    Reject { Assert-AgentDockerVersion -Labels $unlabelled -Version $version -Commit $commit }
    $script:images["guruxorg/dlms-ami-agent:$version"] = @{
        'org.opencontainers.image.version' = $version; 'org.opencontainers.image.revision' = $commit
    }
    $labels = Get-AgentDockerImage -Reference "guruxorg/dlms-ami-agent:$version"
    Assert-AgentDockerVersion -Labels $labels -Version $version -Commit $commit
    Reject { Assert-AgentDockerVersion -Labels $labels -Version $version -Commit ('0' * 40) }
    Publish-AgentDockerLatest -Version $version -Commit $commit
    Check $script:images['guruxorg/dlms-ami-agent:latest']['org.opencontainers.image.version'] $version
    $script:images['guruxorg/dlms-ami-agent:latest'] = @{ 'org.opencontainers.image.version' = '4.1.2611.302' }
    $script:calls.Clear()
    Publish-AgentDockerLatest -Version $version -Commit $commit
    if (@($script:calls | Where-Object { $_ -like 'push *' -or $_ -like 'tag *' }).Count) { throw 'An old rerun must not roll latest back.' }
    $script:images["guruxorg/dlms-ami:$version"] = @{
        'org.opencontainers.image.version' = $version; 'org.opencontainers.image.revision' = $commit
    }
    $script:calls.Clear()
    Publish-AgentDockerLatest -Image guruxorg/dlms-ami -Version $version -Commit $commit
    if (@($script:calls | Where-Object { $_ -match 'dlms-ami-agent' }).Count) { throw 'AMI publication must not touch the agent repository.' }
    Check $script:images['guruxorg/dlms-ami:latest']['org.opencontainers.image.version'] $version
    Write-Host 'Docker registry failure, version ownership, initial latest and old-rerun checks passed.'
} finally { Remove-Item Function:\docker }

# GitHub's pwsh wrapper exits with LASTEXITCODE after the script returns.
# Run the actual entry point in a child process so a handled missing manifest
# cannot leak Docker's exit code into the otherwise successful workflow step.
$processTestRoot = Join-Path ([IO.Path]::GetTempPath()) ('gurux-docker-exit-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $processTestRoot | Out-Null
$wrapperPath = Join-Path $processTestRoot 'github-wrapper.ps1'
$wrapper = @'
param([string]$ScriptPath, [string]$OutputPath, [string]$Image, [switch]$RegistryFailure)
$ErrorActionPreference = 'Stop'
$env:GITHUB_OUTPUT = $OutputPath
function docker {
    $global:LASTEXITCODE = 1
    if ($RegistryFailure) {
        return 'unauthorized: authentication required'
    }
    return "no such manifest: docker.io/$($args[2])"
}
& $ScriptPath -Action Inspect -Image $Image -Version '4.1.2611.301' -Commit '1234567890123456789012345678901234567890'
if (Test-Path variable:LASTEXITCODE) {
    exit $LASTEXITCODE
}
'@
[IO.File]::WriteAllText($wrapperPath, ($wrapper -replace '\r?\n', "`r`n"), [Text.UTF8Encoding]::new($false))
$pwshPath = (Get-Process -Id $PID).Path
$scriptPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../scripts/agent-docker.ps1'))
try {
    foreach ($image in @('guruxorg/dlms-ami-agent', 'guruxorg/dlms-ami')) {
        $outputPath = Join-Path $processTestRoot (($image.Split('/')[-1]) + '.output')
        $childOutput = @(& $pwshPath -NoProfile -File $wrapperPath -ScriptPath $scriptPath -OutputPath $outputPath -Image $image 2>&1)
        Check $LASTEXITCODE 0
        Check ([IO.File]::ReadAllText($outputPath).Trim()) 'exists=false'
    }
    $outputPath = Join-Path $processTestRoot 'unauthorized.output'
    $childOutput = @(& $pwshPath -NoProfile -File $wrapperPath -ScriptPath $scriptPath -OutputPath $outputPath -Image 'guruxorg/dlms-ami-agent' -RegistryFailure 2>&1)
    if ($LASTEXITCODE -eq 0 -or (Test-Path -LiteralPath $outputPath)) {
        throw 'Registry authentication failures must fail without reporting a missing image.'
    }
    Write-Host 'GitHub pwsh exit-code checks passed for both image repositories and authentication failures.'
}
finally {
    Remove-Item -LiteralPath $wrapperPath
    foreach ($file in Get-ChildItem -LiteralPath $processTestRoot -File -Filter '*.output') {
        Remove-Item -LiteralPath $file.FullName
    }
    Remove-Item -LiteralPath $processTestRoot
}
