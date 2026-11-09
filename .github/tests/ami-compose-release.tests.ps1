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
. "$PSScriptRoot/../scripts/ami-compose-release.ps1"
function Reject([scriptblock]$action) { $failed=$false; try { & $action } catch { $failed=$true }; if (-not $failed) { throw 'Expected rejection' } }
$version='4.1.2611.301'
$digest='sha256:'+('a'*64)
$value=New-AmiComposeMetadata -Version $version -Digest $digest
if (-not $value.compose.maintenanceRequired -or $null -ne $value.compose.rollbackCompatibleFrom -or -not $value.compose.containsWasm) { throw 'Unknown rollback safety must require maintenance' }
if ($value.compose.images.ami -cne "guruxorg/dlms-ami@$digest") { throw 'Expected immutable full image' }
Reject { New-AmiComposeMetadata -Version $version -Digest 'latest' }
Reject { New-AmiComposeMetadata -Version $version -Digest $digest -RollbackCompatibleFrom 'unknown' }
$value=New-AmiComposeMetadata -Version $version -Digest $digest -RollbackCompatibleFrom '[4.1.2611.1,4.1.2611.301]'
if ($value.compose.maintenanceRequired) { throw 'Audited rollback should not require maintenance' }
$script:commands=[Collections.Generic.List[string]]::new()
function docker {
    $script:commands.Add(($args -join ' ')); $global:LASTEXITCODE=0
    if ($args[0] -eq 'pull') { return }
    if ($args[0] -eq 'image') { return (@{Os='linux';Architecture='amd64';RepoDigests=@("guruxorg/dlms-ami@$digest");Config=@{Labels=@{'org.opencontainers.image.version'=$version;'org.opencontainers.image.revision'=('1'*40)}}} | ConvertTo-Json -Depth 6 -Compress) }
    if ($args[0] -eq 'manifest') { return '{}' }
    throw "Unexpected docker $args"
}
try {
    $found=Get-AmiPublishedDigest -Version $version -Commit ('1'*40)
    if ($found -cne $digest) { throw 'Registry digest must be preserved' }
    if (-not ($script:commands -contains "manifest inspect guruxorg/dlms-ami@$digest")) { throw 'Digest must be downloadable from registry' }
    Reject { Get-AmiPublishedDigest -Version $version -Commit ('2'*40) }
} finally { Remove-Item Function:\docker }
Write-Host 'Compose identity, digest registry verification and rollback policy checks passed.'
