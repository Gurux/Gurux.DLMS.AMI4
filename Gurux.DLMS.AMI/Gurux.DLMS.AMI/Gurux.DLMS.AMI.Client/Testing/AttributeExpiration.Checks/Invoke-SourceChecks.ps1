param(
    [string]$UiSourceProject = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../../../../Gurux.UI.Components/Development/Gurux.UI.Components.csproj')),
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)
$ErrorActionPreference = 'Stop'
$checksProject = Join-Path $PSScriptRoot 'AttributeExpiration.Checks.csproj'
dotnet build $checksProject -c $Configuration -p:GeneratePackageOnBuild=false -m:1 --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
$checksOutput = Join-Path $PSScriptRoot "bin/$Configuration/net10.0"
$checksUiAssembly = Join-Path $checksOutput 'Gurux.UI.Components.dll'
$uiAssemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($checksUiAssembly).Version.ToString()
# Test source changes against the assembly identity used by the Client package graph.
# Only generated test output is replaced; no package or application deployment is changed.
dotnet build $UiSourceProject -c $Configuration -p:GeneratePackageOnBuild=false "-p:Version=$uiAssemblyVersion" -m:1 --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
$sourceUiAssembly = Join-Path (Split-Path -Parent $UiSourceProject) "bin/$Configuration/net10.0/Gurux.UI.Components.dll"
Copy-Item -LiteralPath $sourceUiAssembly -Destination $checksUiAssembly
dotnet (Join-Path $checksOutput 'AttributeExpiration.Checks.dll')
exit $LASTEXITCODE
