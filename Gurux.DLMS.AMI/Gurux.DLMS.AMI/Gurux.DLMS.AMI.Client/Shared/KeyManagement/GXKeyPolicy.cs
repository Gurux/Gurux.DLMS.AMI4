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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Runtime.Versioning;
using Gurux.DLMS.AMI.Shared.DTOs.Enums;
using Gurux.DLMS.AMI.Shared.DTOs.KeyManagement;
using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Client.Shared.KeyManagement;

/// <summary>
/// Policy shared by the host, providers and agents. Unknown metadata denies use.
/// </summary>
public static class GXKeyPolicy
{
    private const KeyUsage AllUsage = (KeyUsage)511;

    /// <summary>Rejects unsupported or inconsistent registrations, including incomplete external references.</summary>
    /// <param name="key">Key metadata to validate against its DLMS role, encoding and provider reference.</param>
    /// <exception cref="ArgumentException">Metadata is incomplete, unsupported or inconsistent.</exception>
    public static void ValidateRegistration(GXKeyManagementKey key)
    {
        if (key.KeyType is not { } role || !Enum.IsDefined(role) || role == KeyManagementType.None || key.State is not { } state || !Enum.IsDefined(state) || key.ObjectType is not { } objectType || !Enum.IsDefined(objectType) || key.Algorithm is not { } algorithm || !Enum.IsDefined(algorithm) || key.Format is not { } format || !Enum.IsDefined(format) || key.UsageMask is not { } usage || usage == KeyUsage.None || (usage & ~AllUsage) != 0 || key.Extractable == null)
        {
            throw new ArgumentException("Incomplete or unsupported key metadata.");
        }
        var expected = RoleMetadata(role);
        if (objectType != expected.Type || algorithm != expected.Algorithm || (usage & ~expected.Usage) != 0)
        {
            throw new ArgumentException("Key metadata conflicts with its DLMS role.");
        }
        if ((algorithm == KeyAlgorithm.Aes && (key.CryptographicLength is not (128 or 192 or 256) || format != KeyFormat.Raw)) || (algorithm == KeyAlgorithm.Ecdsa && key.CryptographicLength is not (256 or 384)) || (objectType == KeyObjectType.PrivateKey && format is not (KeyFormat.Pkcs8 or KeyFormat.EcPrivateScalar or KeyFormat.PemPrivateKey)) || (objectType == KeyObjectType.PublicKey && format is not (KeyFormat.SubjectPublicKeyInfo or KeyFormat.EcPublicPoint or KeyFormat.PemPublicKey or KeyFormat.PemCertificate)) || (objectType == KeyObjectType.SecretData && format != KeyFormat.Raw))
        {
            throw new ArgumentException("Unsupported key length or encoding.");
        }
        var provider = key.ProviderId ?? key.KeyManagement?.ProviderId;
        if (!string.IsNullOrEmpty(provider) && !string.Equals(provider, "local", StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(key.ExternalId) || string.IsNullOrWhiteSpace(key.ProviderConnectionId ?? key.KeyManagement?.ProviderConnectionId) || key.Key is { Length: > 0 })
            {
                throw new ArgumentException("External keys require a connection and object identifier, without a stored local copy.");
            }
        }
        if (key.ActivationDate > key.DeactivationDate || key.ProcessStartDate > key.ProtectStopDate)
        {
            throw new ArgumentException("Invalid key date interval.");
        }
    }

    /// <summary>Checks use of one key for one purpose at a specified instant.</summary>
    /// <param name="key">Key whose metadata, lifecycle and usage mask are checked.</param>
    /// <param name="purpose">Exact operation to authorize.</param>
    /// <param name="now">Instant used to evaluate lifecycle dates.</param>
    /// <param name="allowRecovery">True to allow deactivated or compromised keys for recovery operations; never permits new protection.</param>
    /// <exception cref="UnauthorizedAccessException">The lifecycle, time interval or usage mask prohibits the operation.</exception>
    public static void ValidateUse(GXKeyManagementKey key, GXKeyPurpose purpose, DateTimeOffset now, bool allowRecovery = false)
    {
        ValidateRegistration(key);
        if (!Enum.IsDefined(purpose) || key.Removed != null || key.DestroyDate != null || key.State is not (KeyState.Active or KeyState.Deactivated or KeyState.Compromised))
        {
            throw new UnauthorizedAccessException("Key is not usable.");
        }
        bool protection = purpose is GXKeyPurpose.Encrypt or GXKeyPurpose.Sign or GXKeyPurpose.MacGenerate or GXKeyPurpose.Wrap or GXKeyPurpose.Password;
        var effectiveState = key.CompromiseDate <= now ? KeyState.Compromised : key.DeactivationDate <= now ? KeyState.Deactivated : key.State;
        if (effectiveState != KeyState.Active && (!allowRecovery || protection))
        {
            throw new UnauthorizedAccessException("Key lifecycle does not permit this operation.");
        }
        if (key.ActivationDate > now || key.ProcessStartDate > now || (protection && key.ProtectStopDate <= now))
        {
            throw new UnauthorizedAccessException("Key is outside its permitted time interval.");
        }
        if ((key.UsageMask!.Value & PurposeUsage(purpose)) == 0)
        {
            throw new UnauthorizedAccessException("Key usage does not permit this operation.");
        }
    }

    /// <summary>Checks release of material separately from metadata access.</summary>
    /// <param name="key">Key whose extractability and lifecycle are checked.</param>
    /// <exception cref="UnauthorizedAccessException">The key is nonextractable, removed or destroyed.</exception>
    public static void ValidateExport(GXKeyManagementKey key)
    {
        ValidateRegistration(key);
        if (key.Extractable != true || key.State is KeyState.Destroyed or KeyState.DestroyedCompromised || key.DestroyDate != null || key.Removed != null)
        {
            throw new UnauthorizedAccessException("Key material cannot be exported.");
        }
    }

    /// <summary>Checks the KMIP lifecycle transition graph.</summary>
    /// <param name="current">Current lifecycle state.</param>
    /// <param name="target">Requested next lifecycle state.</param>
    /// <exception cref="InvalidOperationException">The transition is not permitted, including a transition to the same state.</exception>
    public static void ValidateTransition(KeyState current, KeyState target)
    {
        bool allowed = current switch
        {
            KeyState.PreActive => target is KeyState.Active or KeyState.Compromised or KeyState.Destroyed,
            KeyState.Active => target is KeyState.Deactivated or KeyState.Compromised,
            KeyState.Deactivated => target is KeyState.Compromised or KeyState.Destroyed,
            KeyState.Compromised => target == KeyState.DestroyedCompromised,
            KeyState.Destroyed => target == KeyState.DestroyedCompromised,
            _ => false
        };
        if (!allowed)
        {
            throw new InvalidOperationException("Invalid key lifecycle transition.");
        }
    }

    /// <summary>Validates and initializes a legacy key exactly once. Does not change key identity or material.</summary>
    /// <param name="key">Legacy key updated in place from its role and material, or validated unchanged when its state is already set.</param>
    [UnsupportedOSPlatform("browser")]
    public static void InitializeLegacy(GXKeyManagementKey key)
    {
        if (key.State != null)
        { ValidateRegistration(key); return; }
        var metadata = RoleMetadata(key.KeyType ?? KeyManagementType.None);
        if (key.Key is not { Length: > 0 })
        {
            throw new ArgumentException("Legacy key material is missing.");
        }
        key.ObjectType = metadata.Type;
        key.Algorithm = metadata.Algorithm;
        key.UsageMask = metadata.Usage;
        key.Extractable = true;
        key.Format = KeyFormat.Raw;
        key.CryptographicLength = checked(key.Key.Length * 8);
        if (metadata.Algorithm == KeyAlgorithm.Ecdsa)
        {
            using var ec = ECDsa.Create();
            string pem = Encoding.ASCII.GetString(key.Key);
            if (pem.StartsWith("-----BEGIN", StringComparison.Ordinal))
            {
                if (pem.StartsWith("-----BEGIN CERTIFICATE-----", StringComparison.Ordinal) && metadata.Type == KeyObjectType.PublicKey)
                {
                    using var certificate = X509Certificate2.CreateFromPem(pem);
                    using var publicKey = certificate.GetECDsaPublicKey() ?? throw new ArgumentException("Certificate has no ECDSA public key.");
                    ec.ImportParameters(publicKey.ExportParameters(false));
                    key.Format = KeyFormat.PemCertificate;
                }
                else
                {
                    ec.ImportFromPem(pem);
                    key.Format = metadata.Type == KeyObjectType.PrivateKey ? KeyFormat.PemPrivateKey : KeyFormat.PemPublicKey;
                    if (metadata.Type == KeyObjectType.PrivateKey)
                    {
                        _ = ec.ExportParameters(true);
                    }
                }
            }
            else if (metadata.Type == KeyObjectType.PrivateKey)
            {
                if (key.Key.Length is 32 or 48)
                {
                    key.Format = KeyFormat.EcPrivateScalar;
                    ec.ImportParameters(new ECParameters { Curve = Curve(key.Key.Length * 8), D = key.Key });
                }
                else
                {
                    ec.ImportPkcs8PrivateKey(key.Key, out int read);
                    if (read != key.Key.Length)
                    {
                        throw new ArgumentException("Trailing private key data.");
                    }
        
                    key.Format = KeyFormat.Pkcs8;
                }
            }
            else
            {
                if (key.Key.Length is 64 or 96)
                {
                    key.Format = KeyFormat.EcPublicPoint;
                    int n = key.Key.Length / 2;
                    ec.ImportParameters(new ECParameters { Curve = Curve(n * 8), Q = new ECPoint { X = key.Key[..n], Y = key.Key[n..] } });
                }
                else
                {
                    ec.ImportSubjectPublicKeyInfo(key.Key, out int read);
                    if (read != key.Key.Length)
                    {
                        throw new ArgumentException("Trailing public key data.");
                    }
        
                    key.Format = KeyFormat.SubjectPublicKeyInfo;
                }
            }
        
            key.CryptographicLength = ec.KeySize;
        }
        key.State = KeyState.Active;
        try
        {
            ValidateRegistration(key);
            ValidateMaterial(key);
        }
        catch
        {
            key.State = null;
            throw;
        }
    }

    /// <summary>Checks actual bytes against declared metadata before persistence or consumption.</summary>
    /// <param name="key">Key containing the bytes and declared cryptographic metadata to check.</param>
    /// <remarks>Checks material presence, AES length, and supported ECDSA encodings and curves; it does not replace registration validation.</remarks>
    [UnsupportedOSPlatform("browser")]
    public static void ValidateMaterial(GXKeyManagementKey key)
    {
        if (key.Key is not { Length: > 0 })
        {
            throw new ArgumentException("Key material is missing.");
        }
        if (key.Algorithm == KeyAlgorithm.Aes && key.Key.Length * 8 != key.CryptographicLength)
        {
            throw new ArgumentException("AES key length mismatch.");
        }
        if (key.Algorithm == KeyAlgorithm.Ecdsa)
        {
            using var ec = ECDsa.Create();
            int bits = key.CryptographicLength ?? 0;
            switch (key.Format)
            {
                case KeyFormat.Pkcs8:
                    ec.ImportPkcs8PrivateKey(key.Key, out int privateRead);
                    if (privateRead != key.Key.Length)
                    {
                        throw new ArgumentException("Trailing private key data.");
                    }
        
                    break;
                case KeyFormat.SubjectPublicKeyInfo:
                    ec.ImportSubjectPublicKeyInfo(key.Key, out int publicRead);
                    if (publicRead != key.Key.Length)
                    {
                        throw new ArgumentException("Trailing public key data.");
                    }
        
                    break;
                case KeyFormat.EcPrivateScalar:
                    if (key.Key.Length != bits / 8)
                    {
                        throw new ArgumentException("EC scalar length mismatch.");
                    }
        
                    ec.ImportParameters(new ECParameters { Curve = Curve(bits), D = key.Key });
                    break;
                case KeyFormat.EcPublicPoint:
                    if (key.Key.Length != bits / 4)
                    {
                        throw new ArgumentException("EC point length mismatch.");
                    }
        
                    ec.ImportParameters(new ECParameters { Curve = Curve(bits), Q = new ECPoint { X = key.Key[..(bits / 8)], Y = key.Key[(bits / 8)..] } });
                    break;
                case KeyFormat.PemPrivateKey:
                case KeyFormat.PemPublicKey:
                    ec.ImportFromPem(Encoding.ASCII.GetString(key.Key));
                    if (key.Format == KeyFormat.PemPrivateKey)
                    {
                        _ = ec.ExportParameters(true);
                    }
        
                    break;
                case KeyFormat.PemCertificate:
                    using (var certificate = X509Certificate2.CreateFromPem(Encoding.ASCII.GetString(key.Key)))
                    {
                        using (var publicKey = certificate.GetECDsaPublicKey() ?? throw new ArgumentException("Certificate has no ECDSA key."))
                        {
                            ec.ImportParameters(publicKey.ExportParameters(false));
                        }
                    }

                    break;
                default:
                    throw new ArgumentException("Unsupported EC key format.");
            }
        
            if (ec.KeySize != bits || ec.ExportParameters(false).Curve.Oid.Value != Curve(bits).Oid.Value)
            {
                throw new ArgumentException("Only the declared NIST P-256/P-384 curve is supported.");
            }
        }
    }

    /// <summary>Maps an exact purpose to its usage bit.</summary>
    /// <param name="purpose">Exact operation to map.</param>
    /// <returns>The single usage flag required for the operation.</returns>
    /// <exception cref="ArgumentException">The purpose is unknown.</exception>
    public static KeyUsage PurposeUsage(GXKeyPurpose purpose) => purpose switch
    {
        GXKeyPurpose.Password => KeyUsage.Password,
        GXKeyPurpose.Encrypt => KeyUsage.Encrypt,
        GXKeyPurpose.Decrypt => KeyUsage.Decrypt,
        GXKeyPurpose.Sign => KeyUsage.Sign,
        GXKeyPurpose.Verify => KeyUsage.Verify,
        GXKeyPurpose.MacGenerate => KeyUsage.MacGenerate,
        GXKeyPurpose.MacVerify => KeyUsage.MacVerify,
        GXKeyPurpose.Wrap => KeyUsage.Wrap,
        GXKeyPurpose.Unwrap => KeyUsage.Unwrap,
        _ => throw new ArgumentException("Unknown key purpose.")
    };
    /// <summary>Maximum usage permitted by a DLMS key role.</summary>
    /// <param name="role">Single supported DLMS key role.</param>
    /// <returns>The maximum set of usage flags allowed for the role.</returns>
    public static KeyUsage AllowedUsage(KeyManagementType role) => RoleMetadata(role).Usage;

    /// <summary>Supported named EC curves.</summary>
    /// <param name="bits">Curve size: 256 or 384 bits.</param>
    /// <returns>The corresponding NIST P-256 or P-384 named curve.</returns>
    /// <exception cref="ArgumentException">The requested size is unsupported.</exception>
    public static ECCurve Curve(int bits) => bits switch { 256 => ECCurve.NamedCurves.nistP256, 384 => ECCurve.NamedCurves.nistP384, _ => throw new ArgumentException("Unsupported EC curve.") };

    private static (KeyObjectType Type, KeyAlgorithm Algorithm, KeyUsage Usage) RoleMetadata(KeyManagementType role) => role switch
    {
        KeyManagementType.LLSPassword or KeyManagementType.HLSPassword => (KeyObjectType.SecretData, KeyAlgorithm.None, KeyUsage.Password),
        KeyManagementType.BlockCipher or KeyManagementType.Broadcast => (KeyObjectType.SymmetricKey, KeyAlgorithm.Aes, KeyUsage.Encrypt | KeyUsage.Decrypt | KeyUsage.MacGenerate | KeyUsage.MacVerify),
        KeyManagementType.Authentication => (KeyObjectType.SymmetricKey, KeyAlgorithm.Aes, KeyUsage.MacGenerate | KeyUsage.MacVerify),
        KeyManagementType.MasterKey => (KeyObjectType.SymmetricKey, KeyAlgorithm.Aes, KeyUsage.Wrap | KeyUsage.Unwrap),
        KeyManagementType.PrivateKey => (KeyObjectType.PrivateKey, KeyAlgorithm.Ecdsa, KeyUsage.Sign),
        KeyManagementType.PublicKey => (KeyObjectType.PublicKey, KeyAlgorithm.Ecdsa, KeyUsage.Verify),
        _ => throw new ArgumentException("Unknown or combined DLMS key role.")
    };
}

