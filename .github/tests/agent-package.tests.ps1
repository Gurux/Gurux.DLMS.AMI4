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

# Exercise the real ZIP target: RID publications must coexist without replacing the portable release.
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../Gurux.DLMS.AMI.Agent/Gurux.DLMS.AMI.Agent.csproj'))
$projectRoot = Split-Path $project -Parent
$configuration = 'PackageChecks-' + [Guid]::NewGuid().ToString('N')
$output = Join-Path $projectRoot "bin/$configuration"
$scratch = Join-Path ([IO.Path]::GetTempPath()) ('agent-package-' + [Guid]::NewGuid().ToString('N'))
$version = '0.0.1-package-check'
$cases = @(
    @{ Rid = ''; Name = 'Gurux.DLMS.AMI.Agent-0.0.1-package-check.zip'; Payload = 'portable' },
    @{ Rid = 'win-x64'; Name = 'Gurux.DLMS.AMI.Agent-0.0.1-package-check-win-x64.zip'; Payload = 'windows' },
    @{ Rid = 'linux-arm64'; Name = 'Gurux.DLMS.AMI.Agent-0.0.1-package-check-linux-arm64.zip'; Payload = 'linux' }
)
try {
    New-Item -ItemType Directory -Path $output -Force | Out-Null
    foreach ($case in $cases) {
        $publish = Join-Path $scratch $case.Payload
        New-Item -ItemType Directory -Path $publish -Force | Out-Null
        [IO.File]::WriteAllText((Join-Path $publish 'payload.txt'), $case.Payload)
        dotnet msbuild $project -nologo -verbosity:quiet -target:ZipPublish `
            "-property:Configuration=$configuration" "-property:Version=$version" `
            "-property:RuntimeIdentifier=$($case.Rid)" "-property:PublishDir=$publish/"
        if ($LASTEXITCODE -ne 0) { throw "ZIP target failed for '$($case.Rid)'." }
    }
    foreach ($case in $cases) {
        $package = Join-Path $output $case.Name
        if (-not (Test-Path -LiteralPath $package -PathType Leaf)) { throw "Missing distinct package: $($case.Name)" }
        $zip = [IO.Compression.ZipFile]::OpenRead($package)
        try {
            $reader = [IO.StreamReader]::new($zip.GetEntry('payload.txt').Open())
            try {
                if ($reader.ReadToEnd() -cne $case.Payload) { throw "Package was overwritten by another platform: $($case.Name)" }
            } finally { $reader.Dispose() }
        } finally { $zip.Dispose() }
    }
    Write-Host 'PASS portable and RID-specific ZIP names preserve all three package payloads.'
} finally {
    $binRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'bin')) + [IO.Path]::DirectorySeparatorChar
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not [IO.Path]::GetFullPath($output).StartsWith($binRoot, [StringComparison]::OrdinalIgnoreCase) -or
        -not [IO.Path]::GetFullPath($scratch).StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Package test cleanup paths escaped their roots.'
    }
    if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output -Recurse -Force }
    if (Test-Path -LiteralPath $scratch) { Remove-Item -LiteralPath $scratch -Recurse -Force }
}
