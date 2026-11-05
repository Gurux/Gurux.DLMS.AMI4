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

#Requires -Version 7.2
[CmdletBinding(DefaultParameterSetName = 'NuGet')]
param(
    [Parameter(Mandatory)] [string] $OutputDirectory,
    [Parameter(ParameterSetName = 'NuGet')] [string] $Version,
    [Parameter(ParameterSetName = 'NuGet')] [string] $PackageSource,
    [Parameter(Mandatory, ParameterSetName = 'Published')] [string] $PublishedDirectory
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw 'Updater payload output must not already exist.' }
$parent = [IO.Path]::GetDirectoryName($output)
if ([string]::IsNullOrEmpty($parent)) { throw 'Updater output cannot be a filesystem root.' }
[IO.Directory]::CreateDirectory($parent) | Out-Null
$work = [IO.Path]::GetFullPath((Join-Path $parent ('.agent-updater-build-' + [Guid]::NewGuid().ToString('N'))))
[IO.Directory]::CreateDirectory($work) | Out-Null
try {
    if ($PSCmdlet.ParameterSetName -eq 'NuGet') {
        $toolPath = Join-Path $work 'tool'
        $arguments = @('tool','install','Gurux.Updater.Tool','--tool-path',$toolPath)
        if (-not [string]::IsNullOrWhiteSpace($Version)) { $arguments += @('--version', $Version) }
        if (-not [string]::IsNullOrWhiteSpace($PackageSource)) { $arguments += @('--source', $PackageSource) }
        & dotnet @arguments
        if ($LASTEXITCODE -ne 0) { throw 'Installing Gurux.Updater.Tool for release packaging failed.' }
        $main = @(Get-ChildItem -LiteralPath (Join-Path $toolPath '.store') -Recurse -File -Filter 'Gurux.Updater.Tool.dll')
        if ($main.Count -ne 1) { throw 'The NuGet tool must contain exactly one portable updater payload.' }
        $source = $main[0].Directory.FullName
    } else {
        $source = [IO.Path]::GetFullPath($PublishedDirectory)
    }
    foreach ($file in @('Gurux.Updater.Tool.dll','Gurux.Updater.Tool.deps.json','Gurux.Updater.Tool.runtimeconfig.json')) {
        if (-not (Test-Path -LiteralPath (Join-Path $source $file) -PathType Leaf)) { throw "Updater publication is missing $file" }
    }
    $payload = Join-Path $work 'payload'
    [IO.Directory]::CreateDirectory($payload) | Out-Null
    foreach ($entry in Get-ChildItem -LiteralPath $source -Recurse -Force) {
        if ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Updater payload must not contain linked files.' }
    }
    Get-ChildItem -LiteralPath $source -Force | Copy-Item -Destination $payload -Recurse -Force
    $productVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $payload 'Gurux.Updater.Tool.dll')).ProductVersion
    if ([string]::IsNullOrWhiteSpace($productVersion)) { throw 'Updater assembly has no product version.' }
    $payloadVersion = $productVersion.Split('+')[0]
    if ($payloadVersion -notmatch '^[0-9A-Za-z][0-9A-Za-z.+_-]*$') { throw 'Invalid updater payload version.' }
    $files = [ordered]@{}
    foreach ($file in Get-ChildItem -LiteralPath $payload -Recurse -File -Force | Sort-Object FullName) {
        $relative = [IO.Path]::GetRelativePath($payload, $file.FullName).Replace('\','/')
        $files[$relative] = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    @{ schemaVersion = 1; version = $payloadVersion; entryPoint = 'Gurux.Updater.Tool.dll'; files = $files } |
        ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $payload 'updater-payload.json') -Encoding utf8NoBOM
    # Move only this generated payload into the explicitly requested, absent output directory.
    if (-not [IO.Path]::GetFullPath($payload).StartsWith($work + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Invalid generated updater payload path.'
    }
    [IO.Directory]::Move($payload, $output)
    Write-Host "Prepared portable Gurux.Updater.Tool $payloadVersion"
} finally {
    $resolved = [IO.Path]::GetFullPath($work)
    if (-not $resolved.StartsWith($parent + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolved) -notlike '.agent-updater-build-*') { throw 'Unsafe updater packaging cleanup path.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
