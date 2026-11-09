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
$root = Join-Path ([IO.Path]::GetTempPath()) ('agent-signing-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($root) | Out-Null
$package = Join-Path $root 'agent.zip'
$publicKey = Join-Path $root 'release-public.pem'
$savedKey = $env:AGENT_RELEASE_SIGNING_KEY_PEM
$rsa = [Security.Cryptography.RSA]::Create(2048)
try {
    [IO.File]::WriteAllText($package, 'package bytes')
    $env:AGENT_RELEASE_SIGNING_KEY_PEM = ''
    $rejected = $false
    try { & "$PSScriptRoot/../scripts/sign-agent-package.ps1" -Package $package -Version '1.2.3' } catch { $rejected = $true }
    if (-not $rejected -or (Test-Path "$package.manifest.json")) { throw 'Missing signing key did not fail closed.' }
    $env:AGENT_RELEASE_SIGNING_KEY_PEM = $rsa.ExportPkcs8PrivateKeyPem()
    $rejected = $false
    try { & "$PSScriptRoot/../scripts/sign-agent-package.ps1" -Package $package -Version '1.2.3' -PublicKeyPath $publicKey } catch { $rejected = $true }
    if (-not $rejected -or (Test-Path "$package.manifest.json")) { throw 'Missing bundled public-key file did not fail closed.' }
    [IO.File]::WriteAllText($publicKey, $rsa.ExportPkcs8PrivateKeyPem())
    $rejected = $false
    try { & "$PSScriptRoot/../scripts/sign-agent-package.ps1" -Package $package -Version '1.2.3' -PublicKeyPath $publicKey } catch { $rejected = $true }
    if (-not $rejected -or (Test-Path "$package.manifest.json")) { throw 'Private PEM was accepted as a bundled public key.' }
    $other = [Security.Cryptography.RSA]::Create(2048)
    try { [IO.File]::WriteAllText($publicKey, $other.ExportSubjectPublicKeyInfoPem()) } finally { $other.Dispose() }
    $rejected = $false
    try { & "$PSScriptRoot/../scripts/sign-agent-package.ps1" -Package $package -Version '1.2.3' -PublicKeyPath $publicKey } catch { $rejected = $true }
    if (-not $rejected -or (Test-Path "$package.manifest.json")) { throw 'Mismatched bundled public key did not fail closed.' }
    [IO.File]::WriteAllText($publicKey, $rsa.ExportSubjectPublicKeyInfoPem())
    & "$PSScriptRoot/../scripts/sign-agent-package.ps1" -Package $package -Version '1.2.3' -PublicKeyPath $publicKey
    $manifest = Get-Content "$package.manifest.json" -Raw | ConvertFrom-Json
    $payload = [Text.Encoding]::UTF8.GetBytes("$($manifest.version)`n$($manifest.sha256)`n")
    if (-not $rsa.VerifyData($payload, [Convert]::FromBase64String($manifest.signature),
        [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pss)) { throw 'Signature invalid.' }
    if ($manifest.sha256 -cne (Get-FileHash $package -Algorithm SHA256).Hash.ToLowerInvariant()) { throw 'Digest invalid.' }
    [IO.File]::WriteAllText($package, 'tampered')
    if ($manifest.sha256 -ceq (Get-FileHash $package -Algorithm SHA256).Hash.ToLowerInvariant()) { throw 'Tampering was not detected.' }
    Write-Host 'PASS release signing, missing/mismatched/public-only key validation and tamper detection.'
} finally {
    $rsa.Dispose()
    $env:AGENT_RELEASE_SIGNING_KEY_PEM = $savedKey
    foreach ($file in @($package, "$package.manifest.json", $publicKey)) { if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file } }
    Remove-Item -LiteralPath $root
}
