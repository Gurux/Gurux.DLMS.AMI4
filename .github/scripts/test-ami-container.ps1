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

param([Parameter(Mandatory)][string]$Image, [string]$ExpectedVersion)
$ErrorActionPreference = 'Stop'
$name = 'ami-http-check-' + [Guid]::NewGuid().ToString('N')
& docker run --detach --name $name --publish '127.0.0.1::8080' $Image | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Could not start AMI test container.' }
try {
    $binding = & docker port $name '8080/tcp'
    if ($LASTEXITCODE -ne 0 -or $binding -notmatch ':(\d+)$') { throw 'Could not find AMI test HTTP port.' }
    $url = "http://127.0.0.1:$($Matches[1])/Account/Login"
    $ready = $false
    for ($attempt = 0; $attempt -lt 60; ++$attempt) {
        try {
            $response = Invoke-WebRequest -Uri $url -TimeoutSec 3
            if ($response.StatusCode -eq 200) { $ready = $true; break }
        } catch { }
        $state = & docker inspect $name --format '{{.State.Running}}'
        if ($state -ne 'true') { break }
        Start-Sleep -Seconds 1
    }
    if (-not $ready) { throw 'AMI container did not serve its login page.' }
    $healthy = $false
    for ($attempt = 0; $attempt -lt 90; ++$attempt) {
        $health = & docker inspect $name --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}missing{{end}}'
        if ($health -eq 'healthy') { $healthy = $true; break }
        if ($health -in @('unhealthy', 'missing')) { break }
        Start-Sleep -Seconds 1
    }
    if (-not $healthy) { throw 'AMI image did not pass its built-in HTTP/version health probe.' }
    $baseUrl = $url -replace '/Account/Login$', ''
    $apiVersion = (Invoke-WebRequest -Uri "$baseUrl/api/Configuration/Version" -TimeoutSec 5).Content.Trim()
    $marker = (Invoke-WebRequest -Uri "$baseUrl/version.json" -TimeoutSec 5).Content | ConvertFrom-Json
    if ($marker.version -cne $apiVersion -or ($ExpectedVersion -and $apiVersion -cne $ExpectedVersion)) { throw 'API/client image version mismatch.' }
    $runtime = (Invoke-WebRequest -Uri "$baseUrl/_framework/dotnet.js" -TimeoutSec 10).Content
    $embedded = [regex]::Match($runtime, '(?s)/\*json-start\*/(.*?)/\*json-end\*/')
    if (-not $embedded.Success) { throw 'Published image has no embedded WASM boot manifest.' }
    $boot = $embedded.Groups[1].Value | ConvertFrom-Json
    if ($boot.mainAssemblyName -cne 'Gurux.DLMS.AMI.Client') { throw 'Unexpected image WASM application.' }
    foreach ($group in @('jsModuleNative', 'jsModuleRuntime', 'wasmNative', 'icu', 'coreAssembly', 'assembly')) {
        foreach ($resource in $boot.resources.$group) {
            $startup = Invoke-WebRequest -Uri "$baseUrl/_framework/$([Uri]::EscapeDataString($resource.name))" -TimeoutSec 15
            if ($startup.StatusCode -ne 200 -or $startup.RawContentLength -eq 0) { throw "Missing image startup resource: $($resource.name)" }
        }
    }
    $framework = Invoke-WebRequest -Uri ($url -replace '/Account/Login$', '/_framework/blazor.web.js') -TimeoutSec 5
    if ($framework.StatusCode -ne 200 -or $framework.RawContentLength -eq 0) { throw 'AMI container did not serve the Blazor framework.' }
    & docker exec $name sh -c 'test "$(id -u)" != 0 && test -w /data && test -d /app/wwwroot/_framework'
    if ($LASTEXITCODE -ne 0) { throw 'AMI container runtime permissions or client assets are invalid.' }
    Write-Host 'AMI image health, matching API/WASM versions, boot resources, database startup and non-root data directory checks passed.'
} finally {
    & docker logs $name 2>&1 | Out-Host
    & docker rm --force --volumes $name | Out-Host
}
