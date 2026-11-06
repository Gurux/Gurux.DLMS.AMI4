$ErrorActionPreference = 'Stop'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('agent-compatibility-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $testRoot
$dll = Join-Path $testRoot 'Gurux.DLMS.AMI.Script.dll'
$output = Join-Path $testRoot 'agent.compatibility.json'
try {
    Add-Type -TypeDefinition '[assembly: System.Reflection.AssemblyVersion("1.2.3.4")] [assembly: System.Reflection.AssemblyFileVersion("9.8.7.6")] public class AgentScriptCompatibilityFixture {}' -OutputAssembly $dll
    & "$PSScriptRoot/../scripts/agent-compatibility.ps1" -PublishDirectory $testRoot -OutputFile $output -Version '4.1.2611.601'
    $metadata = Get-Content -LiteralPath $output -Raw | ConvertFrom-Json
    if ($metadata.schemaVersion -ne 1 -or $metadata.id -cne 'Gurux.DLMS.AMI.Agent' -or
        $metadata.version -cne '4.1.2611.601' -or $metadata.targetFramework -cne 'net10.0' -or
        @($metadata.references).Count -ne 1 -or
        $metadata.references[0].name -cne 'Gurux.DLMS.AMI.Script.dll' -or
        $metadata.references[0].version -cne '1.2.3.4') {
        throw 'Metadata must contain only the packaged Script assembly version, not its file version.'
    }
    Remove-Item -LiteralPath $dll
    $failed = $false
    try {
        & "$PSScriptRoot/../scripts/agent-compatibility.ps1" -PublishDirectory $testRoot -OutputFile $output -Version '4.1.2611.601'
    } catch {
        $failed = $true
    }
    if (-not $failed) {
        throw 'Missing Script DLL must prevent compatibility metadata generation.'
    }
    Write-Host 'Agent compatibility metadata generation checks passed.'
} finally {
    foreach ($file in @($dll, $output)) {
        if (Test-Path -LiteralPath $file) {
            Remove-Item -LiteralPath $file
        }
    }
    Remove-Item -LiteralPath $testRoot
}
