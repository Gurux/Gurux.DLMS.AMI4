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

param([ValidateSet('Inspect', 'Latest')][string]$Action,
      [string]$Version, [string]$Commit,
      [ValidateSet('guruxorg/dlms-ami-agent', 'guruxorg/dlms-ami')][string]$Image = 'guruxorg/dlms-ami-agent')

$ErrorActionPreference = 'Stop'

function Get-AgentDockerImage {
    param([string]$Reference)
    $result = (& docker manifest inspect $Reference 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) {
        # Only an explicit missing manifest permits creating a new version tag.
        # Authentication, network and registry errors must never be treated as absence.
        if ($result -cin @("no such manifest: $Reference", "no such manifest: docker.io/$Reference")) { return $null }
        throw "Cannot inspect Docker tag ${Reference}: $result"
    }
    & docker pull $Reference | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Cannot pull Docker tag $Reference" }
    $labels = & docker image inspect $Reference --format '{{json .Config.Labels}}'
    if ($LASTEXITCODE -ne 0) { throw "Cannot inspect Docker labels for $Reference" }
    $parsed = $labels | ConvertFrom-Json
    if ($null -eq $parsed) { return [pscustomobject]@{} }
    return $parsed
}

function Assert-AgentDockerVersion {
    param($Labels, [string]$Version, [string]$Commit)
    if ($Labels.'org.opencontainers.image.version' -cne $Version -or
        $Labels.'org.opencontainers.image.revision' -cne $Commit) {
        throw 'The existing Docker version tag belongs to different agent binaries; refusing to replace it.'
    }
}

function Publish-AgentDockerLatest {
    param([string]$Version, [string]$Commit,
          [string]$Image = 'guruxorg/dlms-ami-agent')
    $reference = "${Image}:$Version"
    $labels = Get-AgentDockerImage -Reference $reference
    if (-not $labels) { throw 'The versioned Docker image must be published before latest.' }
    Assert-AgentDockerVersion -Labels $labels -Version $Version -Commit $Commit
    $latest = Get-AgentDockerImage -Reference "${Image}:latest"
    if ($latest) {
        $currentVersion = $null
        if (-not [version]::TryParse($latest.'org.opencontainers.image.version', [ref]$currentVersion)) {
            throw 'The latest Docker image has no valid version label; refusing to replace it automatically.'
        }
        if ($currentVersion -gt [version]$Version) {
            Write-Host "Keeping newer latest image $currentVersion when retrying $Version."
            return
        }
    }
    & docker tag $reference "${Image}:latest"
    if ($LASTEXITCODE -ne 0) { throw 'Could not tag latest agent image.' }
    & docker push "${Image}:latest"
    if ($LASTEXITCODE -ne 0) { throw 'Could not publish latest agent image.' }
}

if ($MyInvocation.InvocationName -ne '.') {
    if ($Version -cnotmatch '^\d+\.\d+\.\d+\.\d+$' -or $Commit -cnotmatch '^[0-9a-f]{40}$') { throw 'A release version and full source commit are required.' }
    switch ($Action) {
        'Inspect' {
            $labels = Get-AgentDockerImage -Reference "${Image}:$Version"
            $exists = $null -ne $labels
            if ($exists) { Assert-AgentDockerVersion -Labels $labels -Version $Version -Commit $Commit }
            "exists=$($exists.ToString().ToLowerInvariant())" >> $env:GITHUB_OUTPUT
        }
        'Latest' { Publish-AgentDockerLatest -Version $Version -Commit $Commit -Image $Image }
        default { throw 'Choose Inspect or Latest.' }
    }
    # A missing manifest is a handled result. Do not leak Docker's nonzero
    # LASTEXITCODE into GitHub's pwsh wrapper after this script succeeds.
    exit 0
}
