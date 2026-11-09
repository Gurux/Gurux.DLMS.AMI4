$ErrorActionPreference = 'Stop'
$check = Join-Path $PSScriptRoot '../scripts/check-ami-release-version.ps1'
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('ami-version-check-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixture | Out-Null
$version = '4.1.2611.907'
function Write-Fixture([string]$hostVersion, [string]$standaloneVersion, [string]$wasmVersion, [bool]$omitHost = $false) {
    $archives = @(
        @{ Name = "Gurux.DLMS.AMI-$version.zip"; Entries = @{ 'standalone/version.json' = $standaloneVersion } },
        @{ Name = "Gurux.DLMS.AMI.Wasm-$version.zip"; Entries = @{ 'version.json' = $wasmVersion } }
    )
    if (-not $omitHost) {
        $archives[0].Entries['wwwroot/version.json'] = $hostVersion
    }
    foreach ($archive in $archives) {
        $path = Join-Path $fixture $archive.Name
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path
        }
        $zip = [IO.Compression.ZipFile]::Open($path, [IO.Compression.ZipArchiveMode]::Create)
        try {
            foreach ($entry in $archive.Entries.GetEnumerator()) {
                $writer = [IO.StreamWriter]::new($zip.CreateEntry($entry.Key).Open(), [Text.UTF8Encoding]::new($false))
                try {
                    $writer.Write((@{ version = $entry.Value } | ConvertTo-Json -Compress))
                } finally {
                    $writer.Dispose()
                }
            }
        } finally {
            $zip.Dispose()
        }
    }
}
function Reject-Marker {
    $rejected = $false
    try {
        & $check -Directory $fixture -Version $version
    } catch {
        $rejected = $true
    }
    if (-not $rejected) {
        throw 'Expected the release version check to reject an inconsistent package.'
    }
}
try {
    Write-Fixture $version $version $version
    & $check -Directory $fixture -Version $version
    Write-Fixture '4.1.2611.906' $version $version
    Reject-Marker
    Write-Fixture $version '4.1.2611.906' $version
    Reject-Marker
    Write-Fixture $version $version '4.1.2611.906'
    Reject-Marker
    Write-Fixture $version $version $version $true
    Reject-Marker
    Write-Host 'Release version checks passed: matching markers, stale server/standalone/WASM markers and missing marker.'
} finally {
    foreach ($name in @("Gurux.DLMS.AMI-$version.zip", "Gurux.DLMS.AMI.Wasm-$version.zip")) {
        $path = Join-Path $fixture $name
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path
        }
    }
    Remove-Item -LiteralPath $fixture
}
