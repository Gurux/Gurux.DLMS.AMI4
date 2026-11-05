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
$sourceScript = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../scripts/publish-source-release.ps1'))
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('gurux-source-tests-' + [Guid]::NewGuid().ToString('N'))
$previousRepository = $env:GITHUB_REPOSITORY
$global:guruxSourceReleaseTestState = @{ Release = $null; Uploads = 0; Creates = 0; Root = $testRoot }
function gh {
    $global:LASTEXITCODE = 0
    if ($args[0] -eq 'api') {
        if ($global:guruxSourceReleaseTestState.Release) {
            ConvertTo-Json -InputObject @(@($global:guruxSourceReleaseTestState.Release)) -Depth 8 -Compress
        } else {
            '[]'
        }
    } elseif ($args[0] -eq 'release' -and $args[1] -eq 'create') {
        $global:guruxSourceReleaseTestState.Release = @{ tag_name = $args[2]; assets = @() }
        $global:guruxSourceReleaseTestState.Creates++
    } elseif ($args[0] -eq 'release' -and $args[1] -eq 'upload') {
        $asset = $args[3]
        Copy-Item -LiteralPath $asset -Destination (Join-Path $global:guruxSourceReleaseTestState.Root 'uploaded.zip')
        $global:guruxSourceReleaseTestState.Release.assets = @(@{ name = [IO.Path]::GetFileName($asset) })
        $global:guruxSourceReleaseTestState.Uploads++
    } elseif ($args[0] -eq 'release' -and $args[1] -eq 'download') {
        $destination = $args[[Array]::IndexOf($args, '--dir') + 1]
        $name = $args[[Array]::IndexOf($args, '--pattern') + 1]
        Copy-Item -LiteralPath (Join-Path $global:guruxSourceReleaseTestState.Root 'uploaded.zip') -Destination (Join-Path $destination $name)
    } else {
        throw "Unexpected GitHub command: $args"
    }
}
function Assert-SourceTest {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) {
        throw $Message
    }
}
New-Item -ItemType Directory -Path $testRoot | Out-Null
Push-Location $testRoot
try {
    git init --bare remote.git | Out-Null
    git init work | Out-Null
    Set-Location work
    git config user.name 'Source archive test'
    git config user.email 'source-test@example.invalid'
    git config core.autocrlf false
    git remote add origin (Join-Path $testRoot 'remote.git')
    [IO.File]::WriteAllText((Join-Path (Get-Location) '.gitattributes'), "*.cs export-subst`r`n")
    [IO.File]::WriteAllText((Join-Path (Get-Location) 'sample.cs'), '// Commit: $Format:%H$' + "`r`n" + '// Date: $Format:%cI$' + "`r`n")
    git add .
    git commit -m 'Local archive test' | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not create isolated test commit.'
    }
    $commit = git rev-parse HEAD
    $date = git show -s --format=%cI HEAD
    $env:GITHUB_REPOSITORY = 'example/local-test'
    & $sourceScript -PackageId Gurux.Test -Version 1.0.0.1 -Commit $commit
    & $sourceScript -PackageId Gurux.Test -Version 1.0.0.1 -Commit $commit
    Assert-SourceTest ($global:guruxSourceReleaseTestState.Uploads -eq 1 -and $global:guruxSourceReleaseTestState.Creates -eq 1) 'Rerun replaced or duplicated release assets.'
    $zip = [IO.Compression.ZipFile]::OpenRead((Join-Path $testRoot 'uploaded.zip'))
    try {
        $reader = [IO.StreamReader]::new($zip.GetEntry('sample.cs').Open())
        try {
            $text = $reader.ReadToEnd()
        } finally {
            $reader.Dispose()
        }
    } finally {
        $zip.Dispose()
    }
    Assert-SourceTest ($text.Contains($commit) -and $text.Contains($date) -and -not $text.Contains('$Format:')) 'Archive placeholders were not substituted.'
    Assert-SourceTest ((Get-Content sample.cs -Raw).Contains('$Format:%H$')) 'Archive generation changed repository placeholders.'
    [IO.File]::WriteAllText((Join-Path $testRoot 'uploaded.zip'), 'different existing asset')
    $rejected = $false
    try {
        & $sourceScript -PackageId Gurux.Test -Version 1.0.0.1 -Commit $commit
    } catch {
        $rejected = $_.Exception.Message -like '*differs*'
    }
    Assert-SourceTest $rejected 'A conflicting source asset was not rejected.'
    git commit --allow-empty -m 'Another commit' | Out-Null
    $otherCommit = git rev-parse HEAD
    $rejected = $false
    try {
        & $sourceScript -PackageId Gurux.Test -Version 1.0.0.1 -Commit $otherCommit
    } catch {
        $rejected = $_.Exception.Message -like '*another commit*'
    }
    Assert-SourceTest $rejected 'A version tag pointing to another commit was not rejected.'
    Assert-SourceTest ((git rev-parse 'Gurux.Test/v1.0.0.1^{commit}') -ceq $commit) 'An immutable tag was moved.'
    Write-Host 'Source release checks passed: tag creation, safe rerun, archive substitution, conflicting tag and asset rejection.'
} finally {
    Pop-Location
    $env:GITHUB_REPOSITORY = $previousRepository
    $resolved = [IO.Path]::GetFullPath($testRoot)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if ($resolved.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
