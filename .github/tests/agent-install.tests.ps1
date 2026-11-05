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
$root = Join-Path ([IO.Path]::GetTempPath()) ('agent-setup-tests-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($root) | Out-Null
$installer = [IO.Path]::GetFullPath("$PSScriptRoot/../../Gurux.DLMS.AMI.Agent/Install-Agent.ps1")
$savedContainer = $env:DOTNET_RUNNING_IN_CONTAINER
function Check($value, $message) { if (-not $value) { throw $message } }
function Reject([scriptblock] $action, $message) {
    $rejected = $false
    try { & $action } catch { $rejected = $true }
    Check $rejected $message
}
function Fixture($name) {
    $agent = Join-Path $root $name
    $payload = Join-Path $agent 'setup/updater'
    [IO.Directory]::CreateDirectory($payload) | Out-Null
    [IO.File]::WriteAllText((Join-Path $agent 'Gurux.DLMS.AMI.Agent.dll'), 'agent')
    $files = [ordered]@{}
    foreach ($file in @('Gurux.Updater.Tool.dll','Gurux.Updater.Tool.deps.json','Gurux.Updater.Tool.runtimeconfig.json')) {
        [IO.File]::WriteAllText((Join-Path $payload $file), 'payload-' + $file)
        $files[$file] = (Get-FileHash -LiteralPath (Join-Path $payload $file) -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    @{ schemaVersion = 1; version = '1.2.3'; entryPoint = 'Gurux.Updater.Tool.dll'; files = $files } |
        ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $payload 'updater-payload.json')
    return $agent
}
try {
    $env:DOTNET_RUNNING_IN_CONTAINER = ''
    $agent = Fixture 'agent with spaces'
    [IO.File]::WriteAllText((Join-Path $agent 'settings.json'), 'registration unchanged')
    @{ Other = @{ Keep = 'yes' }; Updater = @{ ServiceName = 'existing-service'; PublicKeyPem = 'keep-key' } } |
        ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $agent 'appsettings.json')
    $configFile = Join-Path $agent 'appsettings.json'
    if ($IsWindows) {
        $acl = Get-Acl -LiteralPath $configFile
        $acl.SetAccessRuleProtection($true, $true)
        Set-Acl -LiteralPath $configFile -AclObject $acl
        $permissions = (Get-Acl -LiteralPath $configFile).GetSecurityDescriptorSddlForm([Security.AccessControl.AccessControlSections]::Access)
    } else {
        [IO.File]::SetUnixFileMode($configFile, [IO.UnixFileMode]::UserRead -bor [IO.UnixFileMode]::UserWrite)
        if ((& id '-u') -eq '0') {
            & chown '1001:1001' $configFile
            if ($LASTEXITCODE -ne 0) { throw 'Could not prepare ownership regression fixture.' }
        }
        $permissions = [IO.File]::GetUnixFileMode($configFile)
        $ownership = & stat '-c' '%u:%g' '--' $configFile
    }
    & $installer -AgentDirectory $agent
    if ($IsWindows) {
        Check ((Get-Acl -LiteralPath $configFile).GetSecurityDescriptorSddlForm([Security.AccessControl.AccessControlSections]::Access) -eq $permissions) 'Existing configuration ACL was changed.'
    } else {
        Check ([IO.File]::GetUnixFileMode($configFile) -eq $permissions) 'Existing configuration file mode was changed.'
        Check ((& stat '-c' '%u:%g' '--' $configFile) -eq $ownership) 'Existing configuration owner/group was changed.'
    }
    $config = Get-Content (Join-Path $agent 'appsettings.json') -Raw | ConvertFrom-Json
    $executable = $config.Updater.Executable
    Check (Test-Path -LiteralPath $executable -PathType Leaf) 'Updater was not installed.'
    Check (-not $executable.StartsWith($agent + [IO.Path]::DirectorySeparatorChar)) 'Updater is inside live agent directory.'
    Check ($config.Other.Keep -eq 'yes' -and $config.Updater.ServiceName -eq 'existing-service' -and $config.Updater.PublicKeyPem -eq 'keep-key') 'Existing settings were overwritten.'
    Check ((Get-Content (Join-Path $agent 'settings.json') -Raw) -eq 'registration unchanged') 'Registration was changed.'
    & $installer -AgentDirectory $agent
    $repeat = Get-Content (Join-Path $agent 'appsettings.json') -Raw | ConvertFrom-Json
    Check ($repeat.Updater.Executable -eq $executable) 'Repeated installation changed updater location.'
    Write-Host 'PASS installation, external path, spaces, repeated setup and settings preservation'

    $clean = Fixture 'clean'
    & $installer -AgentDirectory $clean
    Check (Test-Path (Join-Path $clean 'appsettings.json')) 'Fresh setup did not create configuration.'
    Write-Host 'PASS fresh configuration'

    $lower = Fixture 'lowercase'
    '{"updater":{"executable":"old","ServiceName":"preserved"}}' | Set-Content (Join-Path $lower 'appsettings.json')
    & $installer -AgentDirectory $lower
    $lowerConfig = Get-Content (Join-Path $lower 'appsettings.json') -Raw | ConvertFrom-Json -AsHashtable
    Check (@($lowerConfig.Keys | Where-Object { $_ -ieq 'updater' }).Count -eq 1) 'Installer created a duplicate case-insensitive Updater section.'
    $section = $lowerConfig[@($lowerConfig.Keys | Where-Object { $_ -ieq 'updater' })[0]]
    Check (@($section.Keys | Where-Object { $_ -ieq 'executable' }).Count -eq 1) 'Installer created a duplicate case-insensitive Executable value.'
    Check ($section.ServiceName -eq 'preserved') 'Lowercase updater settings were lost.'
    Write-Host 'PASS case-insensitive existing configuration'

    $corrupt = Fixture 'corrupt'
    [IO.File]::WriteAllText((Join-Path $corrupt 'setup/updater/Gurux.Updater.Tool.dll'), 'tampered')
    Reject { & $installer -AgentDirectory $corrupt } 'Corrupt payload accepted.'
    Check (-not (Test-Path (Join-Path $corrupt 'appsettings.json'))) 'Failed setup modified configuration.'
    Write-Host 'PASS corrupt payload rejected before configuration writes'

    $unsafe = Fixture 'unsafe'
    $manifestPath = Join-Path $unsafe 'setup/updater/updater-payload.json'
    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json -AsHashtable
    $manifest.files['../escape.dll'] = '0' * 64
    $manifest | ConvertTo-Json -Depth 10 | Set-Content $manifestPath
    Reject { & $installer -AgentDirectory $unsafe } 'Escaping manifest path accepted.'
    Reject { & $installer -AgentDirectory $agent -UpdaterDirectory (Join-Path $agent 'updater') } 'Live-directory updater accepted.'
    Write-Host 'PASS unsafe paths rejected'

    $container = Fixture 'container'
    $env:DOTNET_RUNNING_IN_CONTAINER = 'true'
    Reject { & $installer -AgentDirectory $container } 'Container self-updater installation accepted.'
    Check (-not (Test-Path (Join-Path $container 'appsettings.json'))) 'Container setup modified configuration.'
    Write-Host 'PASS container uses image replacement'
} finally {
    $env:DOTNET_RUNNING_IN_CONTAINER = $savedContainer
    $resolved = [IO.Path]::GetFullPath($root)
    if (-not $resolved.StartsWith([IO.Path]::GetFullPath([IO.Path]::GetTempPath()), [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolved) -notlike 'agent-setup-tests-*') { throw 'Unsafe test cleanup path.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
