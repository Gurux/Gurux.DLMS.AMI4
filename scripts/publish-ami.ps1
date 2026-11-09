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
    [string]$Version,
    [string]$OutputDirectory,
    [string]$UiSourceProject
)

$ErrorActionPreference = 'Stop'

function Copy-AmiStandaloneStyles {
    param([string]$HostedWebRoot, [string]$StandaloneWebRoot)
    foreach ($name in @('app.css', 'lib')) {
        $source = Join-Path $HostedWebRoot $name
        if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination (Join-Path $StandaloneWebRoot $name) -Recurse -Force }
    }
    # Hosted scoped CSS can contain component imports required outside the server shell.
    # Preserve the standalone client's own bundles and framework files.
    foreach ($source in Get-ChildItem -LiteralPath $HostedWebRoot -File | Where-Object Name -Match '\.styles\.css(\.(br|gz))?$') {
        $target = Join-Path $StandaloneWebRoot $source.Name
        if (!(Test-Path -LiteralPath $target)) { Copy-Item -LiteralPath $source.FullName -Destination $target }
    }
}

function Complete-AmiPublicationLayout {
    param([string]$PublishDirectory, [string]$StandaloneWebRoot, [string]$Version)
    # MapStaticAssets embeds lengths, hashes and compressed representations at host publish time.
    # A second client publish must never replace files described by that manifest.
    $standalone = Join-Path $PublishDirectory 'standalone'
    New-Item -ItemType Directory -Path $standalone -Force | Out-Null
    Copy-Item -Path (Join-Path $StandaloneWebRoot '*') -Destination $standalone -Recurse -Force
    @{ version = $Version } | ConvertTo-Json -Compress | Set-Content -LiteralPath (Join-Path $PublishDirectory 'wwwroot/version.json') -Encoding utf8NoBOM
}

function New-AmiWasmPackage {
    param([string]$WebRoot, [string]$Version, [string]$Archive)
    if ($Version -cnotmatch '^\d+\.\d+\.\d+\.\d+$') { throw 'Invalid WASM version.' }
    foreach ($file in @('index.html', '_framework/blazor.webassembly.js')) {
        if (-not (Test-Path -LiteralPath (Join-Path $WebRoot $file) -PathType Leaf)) { throw "Missing standalone WASM file: $file" }
    }
    if (-not (Test-Path -LiteralPath (Join-Path $WebRoot '_framework/blazor.boot.json')) -and
        @(Get-ChildItem -LiteralPath (Join-Path $WebRoot '_framework') -Filter 'dotnet*.js' -File).Count -eq 0) {
        throw 'Missing WASM boot manifest/runtime.'
    }
    @{ version = $Version } | ConvertTo-Json -Compress | Set-Content -LiteralPath (Join-Path $WebRoot 'version.json') -Encoding utf8NoBOM
    $indexPath = Join-Path $WebRoot 'index.html'
    $html = Get-Content -LiteralPath $indexPath -Raw
    $html = $html -replace '<meta name="gx-wasm-version" content="[^"]*"\s*/?>', ''
    $html = $html.Replace('<head>', "<head>`n    <meta name=`"gx-wasm-version`" content=`"$Version`" />")
    Set-Content -LiteralPath $indexPath -Value $html -Encoding utf8NoBOM
    $settingsPath = Join-Path $WebRoot 'appsettings.json'
    $settings = if (Test-Path -LiteralPath $settingsPath) { Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json -AsHashtable } else { @{} }
    $settings | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $settingsPath -Encoding utf8NoBOM
    # dotnet publish already compressed the unmodified bootstrap documents.
    # Regenerate these siblings so gzip_static/brotli_static cannot serve stale configuration or HTML.
    foreach ($name in @('index.html', 'appsettings.json', 'version.json')) {
        $file = Join-Path $WebRoot $name
        foreach ($extension in @('gz', 'br')) {
            $inputStream = [IO.File]::OpenRead($file)
            $outputStream = [IO.File]::Create("$file.$extension")
            $compressor = if ($extension -eq 'gz') { [IO.Compression.GZipStream]::new($outputStream, [IO.Compression.CompressionLevel]::SmallestSize) }
                else { [IO.Compression.BrotliStream]::new($outputStream, [IO.Compression.CompressionLevel]::SmallestSize) }
            try { $inputStream.CopyTo($compressor) } finally { $compressor.Dispose(); $inputStream.Dispose(); $outputStream.Dispose() }
        }
    }
    [IO.Compression.ZipFile]::CreateFromDirectory($WebRoot, $Archive)
    $digest = (Get-FileHash -LiteralPath $Archive -Algorithm SHA256).Hash.ToLowerInvariant()
    "$digest  $([IO.Path]::GetFileName($Archive))" | Set-Content -LiteralPath "$Archive.sha256" -Encoding ascii
}

function Assert-AmiPackageLayout {
    param([string]$Directory)
    foreach ($name in @('Gurux.DLMS.AMI.dll', 'Gurux.DLMS.AMI.Client.dll', 'Gurux.DLMS.AMI.deps.json', 'Gurux.DLMS.AMI.runtimeconfig.json')) {
        if (-not (Test-Path -LiteralPath (Join-Path $Directory $name) -PathType Leaf)) { throw "Missing AMI release file: $name" }
    }
    if (-not (Test-Path -LiteralPath (Join-Path $Directory 'wwwroot/_framework') -PathType Container) -or
        @(Get-ChildItem -LiteralPath (Join-Path $Directory 'wwwroot/_framework') -File).Count -eq 0) {
        throw 'AMI release is missing the Blazor client framework assets.'
    }
    if (@(Get-ChildItem -LiteralPath $Directory -Recurse -File -Filter 'appsettings.Development*.json').Count) {
        throw 'Development settings must not be included in an AMI release.'
    }
}

function New-AmiReleasePackage {
    param([string]$Version, [string]$OutputDirectory, [string]$UiSourceProject)
    if ($Version -cnotmatch '^\d+\.\d+\.\d+\.\d+$' -or
        @($Version.Split('.') | Where-Object { $_.Length -gt 5 -or [int]$_ -gt 65534 }).Count) {
        throw 'Specify a four-component release version, each part between 0 and 65534.'
    }
    $root = Split-Path $PSScriptRoot -Parent
    $project = Join-Path $root 'Gurux.DLMS.AMI/Gurux.DLMS.AMI/Gurux.DLMS.AMI/Gurux.DLMS.AMI.csproj'
    if (-not $OutputDirectory) { $OutputDirectory = Join-Path (Split-Path $project) 'bin/Release/release' }
    $output = [IO.Path]::GetFullPath($OutputDirectory)
    New-Item -ItemType Directory -Path $output -Force | Out-Null
    $archive = Join-Path $output "Gurux.DLMS.AMI-$Version.zip"
    if (Test-Path -LiteralPath $archive) { throw "Release package already exists: $archive. Use a new version or a fresh output directory." }
    $publishDirectory = Join-Path $output ('publish-' + $Version + '-' + [Guid]::NewGuid().ToString('N'))
    # Each release owns its build state; shared obj/bin can retain development boot resources.
    $hostArtifacts = Join-Path $output ('host-artifacts-' + [Guid]::NewGuid().ToString('N'))
    $arguments = @('publish', $project, '--configuration', 'Release', '--output', $publishDirectory,
        '--artifacts-path', $hostArtifacts,
        '--self-contained', 'false', '-p:UseAppHost=false', '-p:GeneratePackageOnBuild=false',
        '-p:UseGuruxServiceSource=false', '-p:AmiReleaseBuild=true', "-p:Version=$Version",
        '-p:IncludeSourceRevisionInInformationalVersion=false')
    if ($UiSourceProject) {
        $arguments += @('-p:UseGuruxUiComponentsSource=true', "-p:GuruxUiComponentsSourceProject=$([IO.Path]::GetFullPath($UiSourceProject))")
    }
    & dotnet @arguments | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'AMI release publish failed.' }
    Assert-AmiPackageLayout -Directory $publishDirectory
    $actual = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $publishDirectory 'Gurux.DLMS.AMI.dll')).ProductVersion
    if ($actual -cne $Version) { throw "Runtime version '$actual' does not match release '$Version'." }
    # Publish the client itself so the ZIP boots independently of server-rendered Blazor HTML.
    $clientProject = Join-Path $root 'Gurux.DLMS.AMI/Gurux.DLMS.AMI/Gurux.DLMS.AMI.Client/Gurux.DLMS.AMI.Client.csproj'
    $clientOutput = Join-Path $output ('wasm-' + $Version + '-' + [Guid]::NewGuid().ToString('N'))
    $clientArtifacts = Join-Path $output ('wasm-artifacts-' + [Guid]::NewGuid().ToString('N'))
    $clientArguments = @('publish', $clientProject, '--configuration', 'Release', '--output', $clientOutput,
        '--artifacts-path', $clientArtifacts,
        '-p:AmiReleaseBuild=true', '-p:GeneratePackageOnBuild=false', "-p:Version=$Version", '-p:IncludeSourceRevisionInInformationalVersion=false')
    if ($UiSourceProject) { $clientArguments += @('-p:UseGuruxUiComponentsSource=true', "-p:GuruxUiComponentsSourceProject=$([IO.Path]::GetFullPath($UiSourceProject))") }
    & dotnet @clientArguments | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'Standalone WASM publish failed.' }
    $webRoot = Join-Path $clientOutput 'wwwroot'
    Copy-AmiStandaloneStyles -HostedWebRoot (Join-Path $publishDirectory 'wwwroot') -StandaloneWebRoot $webRoot
    $wasmArchive = Join-Path $output "Gurux.DLMS.AMI.Wasm-$Version.zip"
    New-AmiWasmPackage -WebRoot $webRoot -Version $Version -Archive $wasmArchive
    Complete-AmiPublicationLayout -PublishDirectory $publishDirectory -StandaloneWebRoot $webRoot -Version $Version
    # Clean production defaults; deployments supply Crypto:Key and external DB configuration.
    Copy-Item -LiteralPath (Join-Path $root 'scripts/ami-appsettings.json') -Destination (Join-Path $publishDirectory 'appsettings.json')
    [IO.Compression.ZipFile]::CreateFromDirectory($publishDirectory, $archive)
    $digest = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    "$digest  $([IO.Path]::GetFileName($archive))" | Set-Content -LiteralPath "$archive.sha256" -Encoding ascii
    return [pscustomobject]@{ Version = $Version; Archive = $archive; Checksum = "$archive.sha256"; WasmArchive = $wasmArchive; PublishDirectory = $publishDirectory }
}

if ($MyInvocation.InvocationName -ne '.') {
    New-AmiReleasePackage -Version $Version -OutputDirectory $OutputDirectory -UiSourceProject $UiSourceProject
}
