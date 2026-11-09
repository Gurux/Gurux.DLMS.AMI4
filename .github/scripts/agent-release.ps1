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

param(
    [ValidateSet('Prepare', 'Publish', 'Notify')][string]$Action,
    [string]$Directory = 'artifacts/agent',
    [string]$RequestedVersion = '',
    [ValidateSet('Gurux.DLMS.AMI.Agent', 'Gurux.DLMS.AMI')][string]$ProductId = 'Gurux.DLMS.AMI.Agent'
)

$ErrorActionPreference = 'Stop'

function Get-AgentReleaseVersion {
    param([string[]]$Tags = @(), [string[]]$CommitTags = @(),
          [string]$RequestedVersion = '', [DateTimeOffset]$Now = [DateTimeOffset]::UtcNow,
          [ValidateSet('Gurux.DLMS.AMI.Agent', 'Gurux.DLMS.AMI')][string]$ProductId = 'Gurux.DLMS.AMI.Agent')
    $prefix = "$ProductId/v"
    $pattern = '^' + [regex]::Escape($prefix) + '(\d+\.\d+\.\d+\.\d+)$'
    $versions = @($Tags | Where-Object { $_ -cmatch $pattern } |
        ForEach-Object { [version]$_.Substring($prefix.Length) } | Sort-Object -Descending)
    $sameCommit = @($CommitTags | Where-Object { $_ -cmatch $pattern })
    if ($RequestedVersion) {
        $version = $RequestedVersion
    } elseif ($sameCommit.Count -eq 1) {
        $version = $sameCommit[0].Substring($prefix.Length)
    } elseif ($sameCommit.Count -gt 1) {
        throw 'Multiple agent versions identify this commit; select an explicit version.'
    } else {
        $month = $Now.UtcDateTime.ToString('yyMM')
        $revision = $Now.UtcDateTime.Day * 100 + 1
        $candidate = [version]"4.1.$month.$revision"
        if ($versions.Count -gt 0) {
            $latest = $versions[0]
            if ($latest.Major -gt 4 -or ($latest.Major -eq 4 -and $latest.Minor -gt 1) -or
                ($latest.Major -eq 4 -and $latest.Minor -eq 1 -and $latest.Build -gt $candidate.Build)) {
                throw "Latest agent version is $latest; select a newer explicit version."
            }
            if ($latest.Major -eq 4 -and $latest.Minor -eq 1 -and $latest.Build -eq $candidate.Build) {
                $revision = [Math]::Max($revision, $latest.Revision + 1)
            }
        }
        $version = "4.1.$month.$revision"
    }
    if ($version -cnotmatch '^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$' -or
        @($version.Split('.') | Where-Object { $_.Length -gt 5 -or [int]$_ -gt 65534 }).Count -gt 0) {
        throw 'Use four numeric version components between 0 and 65534.'
    }
    $tag = "$prefix$version"
    if ($tag -cin $Tags -and $tag -cnotin $CommitTags) { throw 'This version belongs to another commit.' }
    return $version
}

function Invoke-AgentGh {
    param([string[]]$Arguments)
    $output = & gh @Arguments
    if ($LASTEXITCODE -ne 0) { throw "GitHub command failed: $($Arguments[0])" }
    return $output
}

function Get-AgentGitHubRelease {
    param([string]$Repository, [string]$Tag)
    # Listing distinguishes an absent release from authentication/network failures.
    $pages = (Invoke-AgentGh -Arguments @('api', '--paginate', '--slurp', "repos/$Repository/releases")) |
        ConvertFrom-Json
    foreach ($page in $pages) {
        foreach ($release in $page) {
            if ($release.tag_name -ceq $Tag) { return $release }
        }
    }
    return $null
}

function Assert-AgentPublishedAssets {
    param($Release, $Manifest)
    $names = @($Manifest.package, $Manifest.checksum)
    if ($Manifest.releaseManifest) { $names += $Manifest.releaseManifest }
    if ($Manifest.wasmPackage) { $names += @($Manifest.wasmPackage, $Manifest.wasmChecksum) }
    foreach ($name in $names) {
        if (@($Release.assets | Where-Object { $_.name -ceq $name -and $_.state -eq 'uploaded' -and $_.size -gt 0 }).Count -ne 1) {
            throw "Published agent release is missing a complete asset: $name"
        }
    }
}

function Publish-AgentRelease {
    param($Manifest, [string]$Directory)
    $existing = Get-AgentGitHubRelease -Repository $Manifest.repository -Tag $Manifest.tag
    if ($existing -and -not $existing.draft) {
        Assert-AgentPublishedAssets -Release $existing -Manifest $Manifest
        Write-Host 'Agent release is already published; preserving its original assets.'
        return
    }
    $package = Join-Path $Directory $Manifest.package
    $checksum = Join-Path $Directory $Manifest.checksum
    $additional = @()
    if ($Manifest.wasmPackage) {
        $wasm = Join-Path $Directory $Manifest.wasmPackage
        $wasmChecksum = Join-Path $Directory $Manifest.wasmChecksum
        foreach ($file in @($wasm, $wasmChecksum)) {
            if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Missing WASM asset: $file" }
        }
        $wasmDigest = (Get-FileHash -LiteralPath $wasm -Algorithm SHA256).Hash.ToLowerInvariant()
        if ((Get-Content -LiteralPath $wasmChecksum -Raw).Trim() -cne "$wasmDigest  $($Manifest.wasmPackage)") { throw 'WASM ZIP checksum does not match.' }
        $additional = @($wasm, $wasmChecksum)
    }
    if ($Manifest.releaseManifest) {
        $signedManifest = Join-Path $Directory $Manifest.releaseManifest
        if (-not (Test-Path -LiteralPath $signedManifest -PathType Leaf)) { throw 'Missing signed agent release manifest.' }
        $metadata = Get-Content -LiteralPath $signedManifest -Raw | ConvertFrom-Json
        if ($metadata.version -cne $Manifest.version -or
            $metadata.sha256 -cne (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash.ToLowerInvariant() -or
            [string]::IsNullOrWhiteSpace($metadata.signature)) { throw 'Signed release manifest does not describe this package.' }
        $additional += $signedManifest
    }
    foreach ($file in @($package, $checksum, (Join-Path $Directory 'notes.md'))) {
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Missing release asset: $file" }
    }
    $digest = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash.ToLowerInvariant()
    if ((Get-Content -LiteralPath $checksum -Raw).Trim() -cne "$digest  $($Manifest.package)") {
        throw 'Agent ZIP checksum does not match.'
    }
    if (-not $existing) {
        $title = if ($Manifest.product) { $Manifest.product } else { 'Gurux.DLMS.AMI.Agent' }
        Invoke-AgentGh -Arguments @('release', 'create', $Manifest.tag, '--repo', $Manifest.repository,
            '--verify-tag', '--draft', '--title', "$title $($Manifest.version)",
            '--notes-file', (Join-Path $Directory 'notes.md')) | Out-Host
    }
    Invoke-AgentGh -Arguments (@('release', 'upload', $Manifest.tag, $package, $checksum) + $additional +
        @('--repo', $Manifest.repository, '--clobber')) | Out-Host
    Assert-AgentPublishedAssets -Release (Get-AgentGitHubRelease -Repository $Manifest.repository -Tag $Manifest.tag) -Manifest $Manifest
    Invoke-AgentGh -Arguments @('release', 'edit', $Manifest.tag, '--repo', $Manifest.repository,
        '--notes-file', (Join-Path $Directory 'notes.md'), '--draft=false', '--latest=false') | Out-Host
}

function Get-AgentCatalogToken {
    $catalogToken = ([string]$env:CATALOG_TOKEN).Trim()
    if ([string]::IsNullOrEmpty($catalogToken)) {
        throw 'Configure CATALOG_DISPATCH_TOKEN for Gurux.AMI.Catalog Contents: write.'
    }
    if ($catalogToken -match '[^\x21-\x7E]') {
        throw 'CATALOG_DISPATCH_TOKEN must contain a single-line token without internal whitespace, invisible characters, or a Bearer prefix. Update the GitHub Actions secret.'
    }
    return $catalogToken
}

function Send-AgentCatalogNotification {
    param($Manifest)
    $catalogToken = Get-AgentCatalogToken
    $token = $env:GH_TOKEN
    try {
        $env:GH_TOKEN = $catalogToken
        $product = if ($Manifest.product) { $Manifest.product } else { 'Gurux.DLMS.AMI.Agent' }
        $eventType = if ($product -eq 'Gurux.DLMS.AMI') { 'application-release' } else { 'agent-release' }
        $payload = @{ event_type = $eventType; client_payload = @{
            product = $product; tag = $Manifest.tag; repository = $Manifest.repository
        }} | ConvertTo-Json -Depth 3 -Compress
        $payload | & gh api --method POST repos/Gurux/Gurux.AMI.Catalog/dispatches --input -
        if ($LASTEXITCODE -ne 0) { throw 'Agent release published, but catalog dispatch failed. Rerun this workflow to retry without replacing the release.' }
    } finally { $env:GH_TOKEN = $token }
}

function Initialize-AgentRelease {
    param([string]$Directory, [string]$RequestedVersion,
          [string]$ProductId = 'Gurux.DLMS.AMI.Agent')
    $sha = $env:GITHUB_SHA
    $repository = $env:GITHUB_REPOSITORY
    if ($sha -notmatch '^[0-9a-f]{40}$' -or $repository -cne 'Gurux/Gurux.DLMS.AMI4') { throw 'Unexpected release repository or commit.' }
    $null = Get-AgentCatalogToken
    git fetch --tags origin
    if ($LASTEXITCODE -ne 0) { throw 'Could not refresh release tags.' }
    $prefix = "$ProductId/v"
    $pattern = '^' + [regex]::Escape($prefix) + '\d+\.\d+\.\d+\.\d+$'
    $tags = @(git tag --list "$prefix*")
    if ($LASTEXITCODE -ne 0) { throw 'Could not read release tags.' }
    $commitTags = @(git tag --points-at $sha)
    if ($LASTEXITCODE -ne 0) { throw 'Could not read commit tags.' }
    $version = Get-AgentReleaseVersion -Tags $tags -CommitTags $commitTags -RequestedVersion $RequestedVersion -ProductId $ProductId
    $tag = "$prefix$version"
    New-Item -ItemType Directory -Force -Path $Directory | Out-Null
    $manifest = [ordered]@{
        product = $ProductId; version = $version; tag = $tag; commit = $sha; repository = $repository
        package = "$ProductId-$version.zip"
        checksum = "$ProductId-$version.zip.sha256"
    }
    $existing = Get-AgentGitHubRelease -Repository $repository -Tag $tag
    $published = $existing -and -not $existing.draft
    if ($ProductId -eq 'Gurux.DLMS.AMI.Agent' -and (-not $published -or @($existing.assets | Where-Object { $_.name -ceq "$ProductId-$version.zip.manifest.json" }).Count -gt 0)) {
        $manifest.releaseManifest = "$ProductId-$version.zip.manifest.json"
    }
    if ($ProductId -eq 'Gurux.DLMS.AMI' -and (-not $published -or @($existing.assets | Where-Object { $_.name -ceq "Gurux.DLMS.AMI.Wasm-$version.zip" }).Count -gt 0)) {
        $manifest.wasmPackage = "Gurux.DLMS.AMI.Wasm-$version.zip"
        $manifest.wasmChecksum = "Gurux.DLMS.AMI.Wasm-$version.zip.sha256"
    }
    if ($published) { Assert-AgentPublishedAssets -Release $existing -Manifest $manifest }

    # Reserve before building so a failed run reuses the same version and commit.
    if ($tag -cnotin $tags) {
        git tag $tag $sha
        if ($LASTEXITCODE -ne 0) { throw 'Could not reserve agent release tag.' }
        git push origin "refs/tags/$tag"
        if ($LASTEXITCODE -ne 0) { throw 'Could not push agent release tag.' }
    }
    $previous = @($tags | Where-Object { $_ -cne $tag -and $_ -cmatch $pattern } |
        Where-Object { [version]$_.Substring($prefix.Length) -lt [version]$version } |
        Sort-Object { [version]$_.Substring($prefix.Length) } -Descending | Select-Object -First 1)
    $range = if ($previous.Count) { "$($previous[0])..$sha" } else { $sha }
    $paths = if ($ProductId -eq 'Gurux.DLMS.AMI') { @('Gurux.DLMS.AMI', 'Gurux.DLMS.AMI.Shared', 'Gurux.DLMS.AMI.Http') }
             else { @('Gurux.DLMS.AMI.Agent', 'Gurux.DLMS.AMI.Script', 'Gurux.DLMS.AMI.Shared', 'Gurux.DLMS.AMI.Http') }
    $changes = @(git log '--format=- %s (%h)' $range -- @paths)
    if ($LASTEXITCODE -ne 0) { throw 'Could not create agent changelog.' }
    @("$ProductId $version", '', "Requires .NET 10. Run with: dotnet $ProductId.dll", '', "Commit: $sha", '', 'Changes:', $changes) |
        Set-Content -LiteralPath (Join-Path $Directory 'notes.md') -Encoding utf8
    $manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Directory 'release.json') -Encoding utf8
    "version=$version" >> $env:GITHUB_OUTPUT
    "published=$($published.ToString().ToLowerInvariant())" >> $env:GITHUB_OUTPUT
    "BUILD_BUILDNUMBER=$version" >> $env:GITHUB_ENV
}

if ($MyInvocation.InvocationName -ne '.') {
    switch ($Action) {
        'Prepare' { Initialize-AgentRelease -Directory $Directory -RequestedVersion $RequestedVersion -ProductId $ProductId }
        'Publish' { Publish-AgentRelease -Manifest (Get-Content (Join-Path $Directory 'release.json') -Raw | ConvertFrom-Json) -Directory $Directory }
        'Notify' { Send-AgentCatalogNotification -Manifest (Get-Content (Join-Path $Directory 'release.json') -Raw | ConvertFrom-Json) }
        default { throw 'Choose Prepare, Publish or Notify.' }
    }
}
