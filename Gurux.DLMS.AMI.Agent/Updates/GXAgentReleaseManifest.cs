// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Gurux.DLMS.AMI.Agent;

/// <summary>Carries the release identity, package digest and trusted signature.</summary>
/// <param name="Version">Application version published by this record.</param>
/// <param name="Sha256">SHA-256 digest of the signed application package.</param>
/// <param name="Signature">Signature authenticating the release version and package hash.</param>
internal sealed record GXAgentReleaseManifest(string Version, string Sha256, string Signature)
{
    /// <summary>Verifies the signed release identity and package hash against the trusted public key.</summary>
    /// <param name="json">Serialized release manifest containing the signed package identity.</param>
    /// <param name="version">Application release version being verified or published.</param>
    /// <param name="publicKey">Trusted public key used to verify the manifest signature.</param>
    /// <returns>The release manifest whose identity, hash and signature passed verification.</returns>
    internal static GXAgentReleaseManifest Verify(byte[] json, string version, string publicKey)
    {
        try
        {
            var manifest = JsonSerializer.Deserialize<GXAgentReleaseManifest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidDataException("Missing release manifest.");
            if (manifest.Version != version || manifest.Sha256?.Length != 64 || !manifest.Sha256.All(Uri.IsHexDigit) ||
                manifest.Sha256 != manifest.Sha256.ToLowerInvariant()) {
                throw new InvalidDataException("Release manifest version or digest is invalid.");
            }
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKey);
            if (rsa.KeySize < 2048 || !rsa.VerifyData(Encoding.UTF8.GetBytes(manifest.Version + "\n" + manifest.Sha256 + "\n"),
                Convert.FromBase64String(manifest.Signature), HashAlgorithmName.SHA256, RSASignaturePadding.Pss))
                {
                throw new InvalidDataException("Release signature is invalid.");
            }
            return manifest;
        }
        catch (Exception error) when (error is JsonException or CryptographicException or FormatException or ArgumentException)
        { throw new InvalidDataException("Release manifest or signature is invalid.", error); }
    }
}
