$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/../scripts/agent-release.ps1"

# Only the Git/GitHub boundary is simulated; execute the real release preparation.
function git {
    $global:LASTEXITCODE = 0
    if ($args[0] -eq 'log') {
        if ($args -contains 'Gurux.DLMS.AMI.Script') {
            return '- Compiler fix'
        }
        return '- Agent fix'
    }
}
function gh {
    $global:LASTEXITCODE = 0
    return '[[]]'
}
$directory = Join-Path ([IO.Path]::GetTempPath()) ('agent-no-dependencies-' + [Guid]::NewGuid().ToString('N'))
$names = @('GITHUB_SHA', 'GITHUB_REPOSITORY', 'CATALOG_TOKEN', 'GITHUB_OUTPUT', 'GITHUB_ENV')
$saved = @{}
foreach ($name in $names) {
    $saved[$name] = [Environment]::GetEnvironmentVariable($name)
}
try {
    New-Item -ItemType Directory -Path $directory | Out-Null
    $env:GITHUB_SHA = '1234567890123456789012345678901234567890'
    $env:GITHUB_REPOSITORY = 'Gurux/Gurux.DLMS.AMI4'
    $env:CATALOG_TOKEN = 'catalog-test-token'
    $env:GITHUB_OUTPUT = Join-Path $directory 'output'
    $env:GITHUB_ENV = Join-Path $directory 'env'
    Initialize-AgentRelease -Directory $directory -RequestedVersion '4.1.2611.901'
    $manifest = Get-Content (Join-Path $directory 'release.json') -Raw | ConvertFrom-Json
    if ($manifest.PSObject.Properties.Name -contains 'compatibilityManifest') {
        throw 'Agent release preparation must not require a Script/Shared compatibility manifest.'
    }
    if (-not $manifest.releaseManifest) {
        throw 'Signed package manifests must still be required.'
    }
    if ((Get-Content (Join-Path $directory 'notes.md') -Raw) -notmatch 'Compiler fix') {
        throw 'Agent release notes must include changes to its Script source project.'
    }
    Write-Host 'PASS agent release preparation without dependency metadata and with signed-package metadata.'
} finally {
    foreach ($name in $names) {
        [Environment]::SetEnvironmentVariable($name, $saved[$name])
    }
    foreach ($name in @('output', 'env', 'release.json', 'notes.md')) {
        $file = Join-Path $directory $name
        if (Test-Path -LiteralPath $file) {
            Remove-Item -LiteralPath $file
        }
    }
    if (Test-Path -LiteralPath $directory) {
        Remove-Item -LiteralPath $directory
    }
}
