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
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('ami-wasm-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $fixture 'wwwroot/_framework') -Force | Out-Null
try {
    '<html><head></head><body></body></html>' | Set-Content (Join-Path $fixture 'wwwroot/index.html')
    'boot' | Set-Content (Join-Path $fixture 'wwwroot/_framework/blazor.webassembly.js')
    '{}' | Set-Content (Join-Path $fixture 'wwwroot/_framework/blazor.boot.json')
    '{"OtherSetting":true}' | Set-Content (Join-Path $fixture 'wwwroot/appsettings.json')
    $archive = Join-Path $fixture 'wasm.zip'
    New-AmiWasmPackage -WebRoot (Join-Path $fixture 'wwwroot') -Version '4.1.2611.301' -Archive $archive
    $zip = [IO.Compression.ZipFile]::OpenRead($archive)
    try {
        if (-not $zip.GetEntry('index.html') -or $zip.GetEntry('wwwroot/index.html')) { throw 'WASM archive root is incorrect.' }
        if (-not $zip.GetEntry('version.json')) { throw 'Version marker missing.' }
    } finally { $zip.Dispose() }
    $settings = Get-Content (Join-Path $fixture 'wwwroot/appsettings.json') -Raw | ConvertFrom-Json
    if (-not $settings.OtherSetting) { throw 'Standalone configuration did not preserve existing defaults.' }
    if ((Get-Content (Join-Path $fixture 'wwwroot/index.html') -Raw) -notmatch 'gx-wasm-version.*4.1.2611.301') { throw 'Loaded-page version marker missing.' }
    $inputStream = [IO.File]::OpenRead((Join-Path $fixture 'wwwroot/index.html.gz'))
    $gzip = [IO.Compression.GZipStream]::new($inputStream, [IO.Compression.CompressionMode]::Decompress)
    $reader = [IO.StreamReader]::new($gzip)
    try { if ($reader.ReadToEnd() -notmatch 'gx-wasm-version.*4.1.2611.301') { throw 'Precompressed HTML has stale version metadata.' } }
    finally { $reader.Dispose(); $gzip.Dispose(); $inputStream.Dispose() }
    Write-Host 'WASM ZIP root, version identity and standalone configuration verified.'
} finally {
    $resolved = [IO.Path]::GetFullPath($fixture)
    if (-not $resolved.StartsWith([IO.Path]::GetTempPath(), [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe test cleanup.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
