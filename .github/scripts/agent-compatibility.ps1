# Publish only the assembly contract shared by the standalone agent and AMI server.
param(
    [Parameter(Mandatory)][string]$PublishDirectory,
    [Parameter(Mandatory)][string]$OutputFile,
    [Parameter(Mandatory)][string]$Version
)

$ErrorActionPreference = 'Stop'
$dll = Join-Path $PublishDirectory 'Gurux.DLMS.AMI.Script.dll'
if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) {
    throw 'The published agent is missing Gurux.DLMS.AMI.Script.dll.'
}
$assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName((Resolve-Path -LiteralPath $dll).Path).Version.ToString()
$metadata = [ordered]@{
    schemaVersion = 1
    id = 'Gurux.DLMS.AMI.Agent'
    version = $Version
    targetFramework = 'net10.0'
    references = @(@{ name = 'Gurux.DLMS.AMI.Script.dll'; version = $assemblyVersion })
}
$json = ($metadata | ConvertTo-Json -Depth 4).Replace("`r`n", "`n").Replace("`n", "`r`n") + "`r`n"
[IO.File]::WriteAllText($OutputFile, $json, [Text.UTF8Encoding]::new($false))
