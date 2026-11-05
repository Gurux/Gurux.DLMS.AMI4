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
    [Parameter(Mandatory)][string]$Package,
    [Parameter(Mandatory)][string]$Version
)

$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^[A-Za-z0-9._+-]{1,128}$') { throw 'Invalid release version.' }
if ([string]::IsNullOrWhiteSpace($env:AGENT_RELEASE_SIGNING_KEY_PEM)) {
    throw 'Configure AGENT_RELEASE_SIGNING_KEY_PEM with the release RSA private key.'
}
$key = [Security.Cryptography.RSA]::Create()
try {
    $key.ImportFromPem($env:AGENT_RELEASE_SIGNING_KEY_PEM)
    if ($key.KeySize -lt 2048) { throw 'Release signing key must be at least 2048 bits.' }
    $hash = (Get-FileHash -LiteralPath $Package -Algorithm SHA256).Hash.ToLowerInvariant()
    $payload = [Text.Encoding]::UTF8.GetBytes("$Version`n$hash`n")
    $signature = $key.SignData($payload, [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pss)
    [ordered]@{ version = $Version; sha256 = $hash; signature = [Convert]::ToBase64String($signature) } |
        ConvertTo-Json | Set-Content -LiteralPath "$Package.manifest.json" -Encoding utf8NoBOM
} finally { $key.Dispose() }
