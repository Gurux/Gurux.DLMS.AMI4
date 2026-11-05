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
    [Parameter(Mandatory)][string]$PackageId,
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$Commit
)

$ErrorActionPreference = 'Stop'
if ($PackageId -cnotmatch '^Gurux\.[A-Za-z0-9.]+$' -or
    $Version -cnotmatch '^\d+\.\d+\.\d+\.\d+$' -or
    $Commit -cnotmatch '^[0-9a-f]{40}$') {
    throw 'Invalid package, version or build commit.'
}
$tag = "$PackageId/v$Version"
$remoteTag = @(git ls-remote --exit-code origin "refs/tags/$tag")
if ($LASTEXITCODE -eq 0) {
    git fetch origin "refs/tags/${tag}:refs/tags/${tag}"
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not fetch the existing version tag without replacing a local tag.'
    }
    $tagCommit = git rev-parse "$tag^{commit}"
    if ($LASTEXITCODE -ne 0 -or $tagCommit -cne $Commit) {
        throw "Version tag $tag belongs to another commit."
    }
} elseif ($LASTEXITCODE -eq 2) {
    $localTag = @(git tag --list $tag)
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not inspect local tags.'
    }
    if ($localTag.Count -gt 0) {
        $tagCommit = git rev-parse "$tag^{commit}"
        if ($LASTEXITCODE -ne 0 -or $tagCommit -cne $Commit) {
            throw "Local version tag $tag belongs to another commit."
        }
    } else {
        git tag $tag $Commit
        if ($LASTEXITCODE -ne 0) {
            throw 'Could not create version tag.'
        }
    }
    git push origin "refs/tags/$tag"
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not push version tag; existing tags are never replaced.'
    }
} else {
    throw 'Could not inspect remote version tags.'
}

$directory = Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $directory | Out-Null
$assetName = "$PackageId.$Version-source.zip"
$archive = Join-Path $directory $assetName
try {
    git archive --format=zip "--output=$archive" $tag
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not archive the published source tag.'
    }
    # Listing distinguishes an absent release from authentication or network failures.
    $json = gh api --paginate --slurp "repos/$env:GITHUB_REPOSITORY/releases"
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not list GitHub releases.'
    }
    $release = @($json | ConvertFrom-Json | ForEach-Object { $_ } |
        Where-Object { $_.tag_name -ceq $tag }) | Select-Object -First 1
    if (-not $release) {
        gh release create $tag --repo $env:GITHUB_REPOSITORY --verify-tag --title "$PackageId $Version" --notes "Source for $PackageId $Version."
        if ($LASTEXITCODE -ne 0) {
            throw 'Could not create GitHub Release.'
        }
    }
    $existingAsset = @($release.assets | Where-Object { $_.name -ceq $assetName })
    if ($existingAsset.Count -gt 0) {
        $download = Join-Path $directory 'existing'
        New-Item -ItemType Directory -Path $download | Out-Null
        gh release download $tag --repo $env:GITHUB_REPOSITORY --pattern $assetName --dir $download
        if ($LASTEXITCODE -ne 0) {
            throw 'Could not verify the existing source archive.'
        }
        $existingHash = (Get-FileHash -LiteralPath (Join-Path $download $assetName) -Algorithm SHA256).Hash
        if ($existingHash -cne (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash) {
            throw 'The existing source archive differs; it will not be overwritten.'
        }
    } else {
        gh release upload $tag $archive --repo $env:GITHUB_REPOSITORY
        if ($LASTEXITCODE -ne 0) {
            throw 'Could not upload source archive; existing assets are never overwritten.'
        }
    }
} finally {
    # Only this invocation's temporary directory is removed.
    $resolved = [IO.Path]::GetFullPath($directory)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if ($resolved.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
