# Verifies launcher working directory, argument forwarding and process exit code.
$ErrorActionPreference = 'Stop'
$root = Join-Path ([IO.Path]::GetTempPath()) ('agent-launchers-' + [Guid]::NewGuid().ToString('N'))
$package = Join-Path $root 'gx agent with spaces'
$agent = Join-Path $package 'agent'
$probe = Join-Path $root 'probe'
$bin = Join-Path $probe 'bin'
$savedPath = $env:PATH
[IO.Directory]::CreateDirectory($agent) | Out-Null
[IO.Directory]::CreateDirectory($probe) | Out-Null
try {
    $project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><AssemblyName>LauncherProbe</AssemblyName></PropertyGroup></Project>'
    $source = 'System.Console.WriteLine(System.IO.Directory.GetCurrentDirectory()); foreach (var arg in args) { System.Console.WriteLine(arg); } return 17;'
    [IO.File]::WriteAllText((Join-Path $probe 'Probe.csproj'), $project + "`r`n")
    [IO.File]::WriteAllText((Join-Path $probe 'Program.cs'), $source + "`r`n")
    dotnet build (Join-Path $probe 'Probe.csproj') -o $bin --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw 'Launcher probe build failed.' }
    $apphost = if ($IsWindows) { 'LauncherProbe.exe' } else { 'LauncherProbe' }
    $command = if ($IsWindows) { 'dotnet.exe' } else { 'dotnet' }
    Copy-Item -LiteralPath (Join-Path $bin $apphost) -Destination (Join-Path $bin $command)
    $env:PATH = $bin + [IO.Path]::PathSeparator + $savedPath
    foreach ($name in @('start.sh', 'start.bat')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot "../../Gurux.DLMS.AMI.Agent/$name") -Destination $package
    }
    function Assert-Probe($output, $code) {
        if ($code -ne 17 -or $output.Count -ne 4 -or $output[0] -ne $agent -or
            $output[1] -cne 'Gurux.DLMS.AMI.Agent.dll' -or $output[2] -cne '-h' -or $output[3] -cne 'argument with spaces') {
            throw "Launcher lost working directory, arguments or exit code ($code): $output"
        }
    }
    if ($IsWindows) {
        $launcher = Join-Path $package 'start.bat'
        $output = @(& $env:ComSpec /d /c "call `"$launcher`" -h `"argument with spaces`"")
        Assert-Probe $output $LASTEXITCODE
        Write-Host 'PASS Windows launcher from another working directory with spaces and exit code forwarding.'
        $bash = Join-Path $env:ProgramFiles 'Git/bin/bash.exe'
        if (Test-Path -LiteralPath $bash) {
            $launcher = (Join-Path $package 'start.sh').Replace('\', '/')
            $output = @(& $bash $launcher -h 'argument with spaces')
            Assert-Probe $output $LASTEXITCODE
            Write-Host 'PASS CRLF shell launcher under Git Bash.'
        }
    } else {
        $output = @(& sh (Join-Path $package 'start.sh') -h 'argument with spaces')
        Assert-Probe $output $LASTEXITCODE
        Write-Host 'PASS Unix launcher from another working directory with spaces and exit code forwarding.'
    }
    $global:LASTEXITCODE = 0
} finally {
    $env:PATH = $savedPath
    $resolved = [IO.Path]::GetFullPath($root)
    $tempPrefix = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($tempPrefix, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolved) -notlike 'agent-launchers-*') { throw 'Unsafe launcher test cleanup path.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}