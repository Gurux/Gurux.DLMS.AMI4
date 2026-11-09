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

param([string]$Version, [string]$Commit, [string]$RollbackCompatibleFrom,
      [string]$Directory = 'artifacts/compose')
$ErrorActionPreference = 'Stop'

function New-AmiComposeMetadata {
    param([string]$Version, [string]$Digest, [string]$RollbackCompatibleFrom)
    if ($Version -cnotmatch '^\d+\.\d+\.\d+\.\d+$' -or $Digest -cnotmatch '^sha256:[0-9a-f]{64}$') { throw 'A release version and immutable registry digest are required.' }
    $rollback = $null
    if (-not [string]::IsNullOrWhiteSpace($RollbackCompatibleFrom)) {
        # Deliberately require an explicit bounded four-part NuGet interval for the audited workflow input.
        $match = [regex]::Match($RollbackCompatibleFrom, '^([\[(])(\d+\.\d+\.\d+\.\d+),(\d+\.\d+\.\d+\.\d+)([\])])$')
        if (-not $match.Success) { throw 'Audited rollback compatibility must be a bounded four-part NuGet version range.' }
        $lower=[version]$match.Groups[2].Value; $upper=[version]$match.Groups[3].Value
        if ($lower -gt $upper -or ($lower -eq $upper -and ($match.Groups[1].Value -ne '[' -or $match.Groups[4].Value -ne ']'))) { throw 'The rollback compatibility interval is empty.' }
        $rollback = $RollbackCompatibleFrom
    }
    return [ordered]@{schemaVersion=1;version=$Version;compose=[ordered]@{
        channel='stable';platforms=@('linux/amd64');images=@{ami="guruxorg/dlms-ami@$Digest"};containsWasm=$true
        rollbackCompatibleFrom=$rollback;maintenanceRequired=($null -eq $rollback)
    }}
}

function Get-AmiPublishedDigest {
    param([string]$Version, [string]$Commit)
    $reference="guruxorg/dlms-ami:$Version"
    & docker pull --platform linux/amd64 $reference | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Published version must be downloadable before catalog publication.' }
    $raw = & docker image inspect $reference --format '{{json .}}'
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect published AMI image.' }
    $image=$raw | ConvertFrom-Json
    if ($image.Os -cne 'linux' -or $image.Architecture -cne 'amd64' -or
        $image.Config.Labels.'org.opencontainers.image.version' -cne $Version -or
        $image.Config.Labels.'org.opencontainers.image.revision' -cne $Commit) { throw 'Published image platform or release identity mismatch.' }
    $digests=@($image.RepoDigests | Where-Object { $_ -cmatch '^guruxorg/dlms-ami@sha256:[0-9a-f]{64}$' })
    if ($digests.Count -ne 1) { throw 'Expected exactly one AMI registry digest.' }
    & docker manifest inspect $digests[0] | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Immutable AMI manifest is not downloadable.' }
    return $digests[0].Split('@')[1]
}

if ($MyInvocation.InvocationName -ne '.') {
    if ($Commit -cnotmatch '^[0-9a-f]{40}$') { throw 'A full source commit is required.' }
    $metadata=New-AmiComposeMetadata -Version $Version -Digest (Get-AmiPublishedDigest -Version $Version -Commit $Commit) -RollbackCompatibleFrom $RollbackCompatibleFrom
    New-Item -ItemType Directory -Force $Directory | Out-Null
    $name="Gurux.DLMS.AMI-$Version.compose.json"
    $path=Join-Path $Directory $name
    $json=($metadata | ConvertTo-Json -Depth 6)+"`n"
    [IO.File]::WriteAllText([IO.Path]::GetFullPath($path),$json,[Text.UTF8Encoding]::new($false))
    $tag="Gurux.DLMS.AMI/v$Version"
    $releaseRaw=& gh release view $tag --repo $env:GITHUB_REPOSITORY --json assets
    if ($LASTEXITCODE -ne 0) { throw 'Cannot read AMI release assets.' }
    $existing=@(($releaseRaw | ConvertFrom-Json).assets | Where-Object { $_.name -ceq $name })
    if ($existing.Count -gt 1) { throw 'Duplicate compose sidecars.' }
    if ($existing.Count -eq 1) {
        $published=Join-Path $Directory 'published'
        New-Item -ItemType Directory -Force $published | Out-Null
        & gh release download $tag --repo $env:GITHUB_REPOSITORY --pattern $name --dir $published --clobber
        if ($LASTEXITCODE -ne 0) { throw 'Cannot verify existing compose sidecar.' }
        if ([IO.File]::ReadAllText((Join-Path $published $name)) -cne $json) { throw 'Existing compose sidecar differs; refusing to replace immutable release metadata.' }
    } else {
        & gh release upload $tag $path --repo $env:GITHUB_REPOSITORY
        if ($LASTEXITCODE -ne 0) { throw 'Cannot publish compose sidecar.' }
    }
}
