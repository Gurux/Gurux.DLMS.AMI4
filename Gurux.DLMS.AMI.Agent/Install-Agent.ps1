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
<#
.SYNOPSIS
Installs the bundled updater outside the extracted agent directory and configures its path.
.EXAMPLE
pwsh ./Install-Agent.ps1
.EXAMPLE
pwsh ./Install-Agent.ps1 -AgentDirectory /opt/gurux/agent -UpdaterDirectory /opt/gurux/agent-updater
#>
[CmdletBinding()]
param(
    [string] $AgentDirectory = $PSScriptRoot,
    [string] $UpdaterDirectory
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ($env:DOTNET_RUNNING_IN_CONTAINER -in @('true','1')) {
    throw 'Container agents are updated by replacing their image; an external updater is not installed.'
}
$comparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
function Under([string] $path, [string] $directory) {
    $prefix = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetFullPath($directory)) + [IO.Path]::DirectorySeparatorChar
    return [IO.Path]::GetFullPath($path).StartsWith($prefix, $comparison)
}
function NoLinks([string] $path) {
    $node = [IO.DirectoryInfo]::new([IO.Path]::GetFullPath($path))
    while ($null -ne $node) {
        if ($node.Exists -and ($node.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw "Linked installation path is not supported: $($node.FullName)" }
        $node = $node.Parent
    }
}
$agent = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetFullPath($AgentDirectory))
if (-not (Test-Path -LiteralPath (Join-Path $agent 'Gurux.DLMS.AMI.Agent.dll') -PathType Leaf)) {
    throw 'Run setup against an extracted agent release containing Gurux.DLMS.AMI.Agent.dll.'
}
if ([string]::IsNullOrWhiteSpace($UpdaterDirectory)) { $UpdaterDirectory = $agent + '.updater' }
$updaterRoot = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetFullPath($UpdaterDirectory))
if ($updaterRoot.Equals($agent, $comparison) -or (Under $updaterRoot $agent) -or (Under $agent $updaterRoot)) {
    throw 'The updater directory must be separate from the live agent directory.'
}
NoLinks $agent
NoLinks $updaterRoot
$payload = Join-Path $agent 'setup/updater'
$manifestPath = Join-Path $payload 'updater-payload.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { throw 'Bundled updater is missing. Use a release ZIP containing setup/updater.' }
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json -AsHashtable
if ($manifest.schemaVersion -ne 1 -or $manifest.entryPoint -cne 'Gurux.Updater.Tool.dll' -or
    $manifest.version -notmatch '^[0-9A-Za-z][0-9A-Za-z.+_-]*$' -or $manifest.files -isnot [Collections.IDictionary]) {
    throw 'Invalid updater payload manifest.'
}
foreach ($required in @('Gurux.Updater.Tool.dll','Gurux.Updater.Tool.deps.json','Gurux.Updater.Tool.runtimeconfig.json')) {
    if (-not $manifest.files.Contains($required)) { throw "Updater payload is missing $required" }
}
function VerifyPayload([string] $directory) {
    NoLinks $directory
    foreach ($entry in Get-ChildItem -LiteralPath $directory -Recurse -Force) {
        if ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Updater payload contains a link.' }
    }
    foreach ($entry in $manifest.files.GetEnumerator()) {
        $relative = [string]$entry.Key
        if ([IO.Path]::IsPathRooted($relative) -or $relative.Contains('\') -or $relative.Contains(':') -or
            @($relative.Split('/') | Where-Object { $_ -in @('', '.', '..') }).Count -gt 0 -or $entry.Value -notmatch '^[0-9a-fA-F]{64}$') {
            throw "Unsafe updater payload entry: $relative"
        }
        $file = [IO.Path]::GetFullPath((Join-Path $directory $relative))
        if (-not (Under $file $directory) -or -not (Test-Path -LiteralPath $file -PathType Leaf) -or
            (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ine $entry.Value) { throw "Updater payload verification failed: $relative" }
    }
    $files = @(Get-ChildItem -LiteralPath $directory -Recurse -File -Force)
    if ($files.Count -ne $manifest.files.Count + 1) { throw 'Updater payload contains unexpected files.' }
}
VerifyPayload $payload
$configPath = Join-Path $agent 'appsettings.json'
if ((Test-Path -LiteralPath $configPath) -and ((Get-Item -LiteralPath $configPath -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
    throw 'Linked appsettings.json is not supported; update its target configuration explicitly.'
}
$config = if (Test-Path -LiteralPath $configPath -PathType Leaf) { Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json -AsHashtable } else { @{} }
if ($config -isnot [Collections.IDictionary]) { throw 'Existing appsettings.json must contain a JSON object.' }
function ExistingKey([Collections.IDictionary] $dictionary, [string] $name) {
    $matches = @($dictionary.Keys | Where-Object { $_ -ieq $name })
    if ($matches.Count -gt 1) { throw "Ambiguous existing configuration keys: $name" }
    if ($matches.Count -eq 1) { return [string]$matches[0] }
    return $name
}
$sectionKey = ExistingKey $config 'Updater'
if (-not $config.Contains($sectionKey) -or $null -eq $config[$sectionKey]) { $config[$sectionKey] = @{} }
$updaterSettings = $config[$sectionKey]
if ($updaterSettings -isnot [Collections.IDictionary]) { throw 'Existing Updater settings must contain a JSON object.' }
$executableKey = ExistingKey $updaterSettings 'Executable'
$identity = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash.ToLowerInvariant().Substring(0,16)
$destination = [IO.Path]::GetFullPath((Join-Path $updaterRoot ($manifest.version + '-' + $identity)))
if (-not (Under $destination $updaterRoot)) { throw 'Invalid updater destination.' }
$lock = [IO.File]::Open((Join-Path $agent '.install-agent.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
$staging = $null
$configTemp = $null
try {
    if (Test-Path -LiteralPath $destination) {
        VerifyPayload $destination
    } else {
        [IO.Directory]::CreateDirectory($updaterRoot) | Out-Null
        $staging = [IO.Path]::GetFullPath((Join-Path $updaterRoot ('.staging-' + [Guid]::NewGuid().ToString('N'))))
        if (-not (Under $staging $updaterRoot)) { throw 'Invalid updater staging path.' }
        [IO.Directory]::CreateDirectory($staging) | Out-Null
        Get-ChildItem -LiteralPath $payload -Force | Copy-Item -Destination $staging -Recurse -Force
        VerifyPayload $staging
        # Both absolute move targets have been checked against the separate updater root.
        [IO.Directory]::Move($staging, $destination)
        $staging = $null
    }
    $updaterExecutable = Join-Path $destination 'Gurux.Updater.Tool.dll'
    $updaterSettings[$executableKey] = $updaterExecutable
    $configTemp = Join-Path $agent ('.appsettings-' + [Guid]::NewGuid().ToString('N') + '.tmp')
    [IO.File]::WriteAllText($configTemp, ($config | ConvertTo-Json -Depth 100) + [Environment]::NewLine)
    if (Test-Path -LiteralPath $configPath -PathType Leaf) {
        if ($IsWindows) {
            Set-Acl -LiteralPath $configTemp -AclObject (Get-Acl -LiteralPath $configPath)
        } elseif ($IsLinux) {
            $ownership = & stat '-c' '%u:%g' '--' $configPath
            if ($LASTEXITCODE -ne 0 -or $ownership -notmatch '^\d+:\d+$') { throw 'Could not read existing configuration ownership.' }
            & chown '--' $ownership $configTemp
            if ($LASTEXITCODE -ne 0) { throw 'Could not preserve configuration ownership.' }
            [IO.File]::SetUnixFileMode($configTemp, [IO.File]::GetUnixFileMode($configPath))
        } else {
            throw 'Configuration permission preservation is supported on Windows and Linux.'
        }
    }
    if ($IsWindows -and (Test-Path -LiteralPath $configPath -PathType Leaf)) {
        # ReplaceFile preserves the existing Windows file's security metadata.
        [IO.File]::Replace($configTemp, $configPath, [NullString]::Value)
    } else {
        [IO.File]::Move($configTemp, $configPath, $true)
    }
    $configTemp = $null
    Write-Host "Updater installed: $updaterExecutable"
    Write-Host 'Updater.Executable configured; existing agent and updater settings retained.'
} finally {
    if ($null -ne $configTemp -and (Test-Path -LiteralPath $configTemp)) { Remove-Item -LiteralPath $configTemp -Force }
    if ($null -ne $staging -and (Test-Path -LiteralPath $staging)) {
        if (-not (Under $staging $updaterRoot)) { throw 'Refusing unsafe updater staging cleanup.' }
        Remove-Item -LiteralPath $staging -Recurse -Force
    }
    $lock.Dispose()
}
