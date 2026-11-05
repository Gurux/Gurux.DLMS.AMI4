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
$root = Join-Path ([IO.Path]::GetTempPath()) ('agent-payload-tests-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($root) | Out-Null
$prepare = [IO.Path]::GetFullPath("$PSScriptRoot/../scripts/prepare-agent-updater.ps1")
$global:agentPayloadToolArguments = @()
$global:agentPayloadFailTool = $false
function global:dotnet {
    $global:agentPayloadToolArguments = @($args)
    if ($global:agentPayloadFailTool) { $global:LASTEXITCODE = 1; return }
    $index = [Array]::IndexOf($args, '--tool-path')
    if ($index -lt 0) { throw 'The tool must be installed in an isolated tool path.' }
    $payload = Join-Path $args[$index + 1] '.store/gurux.updater.tool/1.2.3/tools/net10.0/any'
    [IO.Directory]::CreateDirectory($payload) | Out-Null
    Copy-Item -LiteralPath ([object].Assembly.Location) -Destination (Join-Path $payload 'Gurux.Updater.Tool.dll')
    foreach ($file in @('Gurux.Updater.Tool.deps.json','Gurux.Updater.Tool.runtimeconfig.json')) {
        [IO.File]::WriteAllText((Join-Path $payload $file), '{}')
    }
    $global:LASTEXITCODE = 0
}
try {
    $output = Join-Path $root 'payload'
    & $prepare -OutputDirectory $output -Version '1.2.3' -PackageSource 'https://packages.test/v3/index.json'
    $manifest = Get-Content (Join-Path $output 'updater-payload.json') -Raw | ConvertFrom-Json -AsHashtable
    if ($manifest.files.Count -ne 3 -or $manifest.entryPoint -cne 'Gurux.Updater.Tool.dll') { throw 'Payload manifest is incomplete.' }
    foreach ($entry in $manifest.files.GetEnumerator()) {
        if ((Get-FileHash -LiteralPath (Join-Path $output $entry.Key) -Algorithm SHA256).Hash -ine $entry.Value) { throw 'Payload digest mismatch.' }
    }
    if ($global:agentPayloadToolArguments -notcontains '1.2.3' -or $global:agentPayloadToolArguments -notcontains '--version' -or $global:agentPayloadToolArguments -notcontains 'https://packages.test/v3/index.json') { throw 'Tool version or source was not passed to dotnet.' }
    if (@(Get-ChildItem -LiteralPath $root -Directory -Filter '.agent-updater-build-*' -Force).Count -ne 0) { throw 'Tool installation workspace leaked.' }
    Write-Host 'PASS isolated NuGet tool installation, version/source arguments, portable payload and checksums'
    $global:agentPayloadFailTool = $true
    $rejected = $false
    try { & $prepare -OutputDirectory (Join-Path $root 'failure') } catch { $rejected = $true }
    if (-not $rejected -or (Test-Path (Join-Path $root 'failure'))) { throw 'Failed tool installation produced a usable payload.' }
    Write-Host 'PASS failed tool installation leaves no release payload'
} finally {
    Remove-Item Function:\dotnet -ErrorAction SilentlyContinue
    $resolved=[IO.Path]::GetFullPath($root)
    if (-not $resolved.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()),[StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolved) -notlike 'agent-payload-tests-*') { throw 'Unsafe payload test cleanup.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
