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
. "$PSScriptRoot/../../scripts/publish-ami.ps1"
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('ami-package-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixture | Out-Null
function Reject([scriptblock]$action) {
    $failed = $false
    try { & $action } catch { $failed = $true }
    if (-not $failed) { throw 'Expected incomplete or unsafe server package to fail.' }
}
try {
    Reject { Assert-AmiPackageLayout -Directory $fixture }
    foreach ($name in @('Gurux.DLMS.AMI.dll', 'Gurux.DLMS.AMI.Client.dll', 'Gurux.DLMS.AMI.deps.json', 'Gurux.DLMS.AMI.runtimeconfig.json')) {
        'fixture' | Set-Content -LiteralPath (Join-Path $fixture $name)
    }
    Reject { Assert-AmiPackageLayout -Directory $fixture }
    New-Item -ItemType Directory -Path (Join-Path $fixture 'wwwroot/_framework') -Force | Out-Null
    'fixture' | Set-Content -LiteralPath (Join-Path $fixture 'wwwroot/_framework/client.wasm')
    Reject { Assert-AmiPackageLayout -Directory $fixture }
    New-Item -ItemType Directory -Path (Join-Path $fixture 'wwwroot/lib/bootstrap/dist/css') -Force | Out-Null
    '.nav{display:flex;list-style:none}' | Set-Content (Join-Path $fixture 'wwwroot/lib/bootstrap/dist/css/bootstrap.min.css')
    Assert-AmiPackageLayout -Directory $fixture
    'private development config' | Set-Content -LiteralPath (Join-Path $fixture 'appsettings.Development.json')
    Reject { Assert-AmiPackageLayout -Directory $fixture }
    Remove-Item -LiteralPath (Join-Path $fixture 'appsettings.Development.json')
    $standalone = Join-Path $fixture 'client-publication'
    New-Item -ItemType Directory -Path (Join-Path $standalone '_framework') -Force | Out-Null
    'host-compressed-runtime' | Set-Content (Join-Path $fixture 'wwwroot/_framework/dotnet.js.br')
    'host-manifest' | Set-Content (Join-Path $fixture 'Gurux.DLMS.AMI.staticwebassets.endpoints.json')
    'host-css' | Set-Content (Join-Path $fixture 'wwwroot/app.css')
    'host-scoped-css' | Set-Content (Join-Path $fixture 'wwwroot/Gurux.DLMS.AMI.styles.css')
    'host-client-css' | Set-Content (Join-Path $fixture 'wwwroot/Gurux.DLMS.AMI.Client.styles.css')
    'standalone-compressed-runtime' | Set-Content (Join-Path $standalone '_framework/dotnet.js.br')
    'standalone-index' | Set-Content (Join-Path $standalone 'index.html')
    'standalone-client-css' | Set-Content (Join-Path $standalone 'Gurux.DLMS.AMI.Client.styles.css')
    $before = @{}
    Get-ChildItem (Join-Path $fixture 'wwwroot') -Recurse -File | ForEach-Object { $before[$_.FullName] = (Get-FileHash $_.FullName).Hash }
    $manifestHash = (Get-FileHash (Join-Path $fixture 'Gurux.DLMS.AMI.staticwebassets.endpoints.json')).Hash
    Copy-AmiStandaloneStyles -HostedWebRoot (Join-Path $fixture 'wwwroot') -StandaloneWebRoot $standalone
    if ((Get-Content (Join-Path $standalone 'Gurux.DLMS.AMI.Client.styles.css') -Raw).Trim() -cne 'standalone-client-css') { throw 'Standalone client scoped CSS was overwritten.' }
    Complete-AmiPublicationLayout -PublishDirectory $fixture -StandaloneWebRoot $standalone -Version '99.0.1.0'
    foreach ($path in $before.Keys) {
        if ((Get-FileHash $path).Hash -cne $before[$path]) { throw "Hosted asset changed after standalone publication: $path" }
    }
    if ((Get-FileHash (Join-Path $fixture 'Gurux.DLMS.AMI.staticwebassets.endpoints.json')).Hash -cne $manifestHash) { throw 'Hosted manifest changed.' }
    foreach ($path in @('_framework/dotnet.js.br', 'index.html', 'Gurux.DLMS.AMI.Client.styles.css', 'Gurux.DLMS.AMI.styles.css', 'app.css')) {
        if (!(Test-Path (Join-Path $fixture "standalone/$path"))) { throw "Standalone publication is missing $path" }
        if ((Get-FileHash (Join-Path $fixture "standalone/$path")).Hash -cne (Get-FileHash (Join-Path $standalone $path)).Hash) { throw "Standalone asset changed: $path" }
    }
    if ((Get-Content (Join-Path $fixture 'wwwroot/version.json') -Raw | ConvertFrom-Json).version -cne '99.0.1.0') { throw 'Missing hosted release marker.' }
    Write-Host 'AMI package requires server/client assets and excludes development settings.'
    Write-Host 'Standalone publication preserves hosted asset bytes and manifest, including compressed runtime assets.'
} finally {
    $resolvedFixture = [IO.Path]::GetFullPath($fixture)
    if ([IO.Path]::GetDirectoryName($resolvedFixture) -ne [IO.Path]::GetTempPath().TrimEnd([IO.Path]::DirectorySeparatorChar) -or
        [IO.Path]::GetFileName($resolvedFixture) -notmatch '^ami-package-[a-f0-9]{32}$') { throw 'Unexpected package test cleanup path.' }
    Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
}
