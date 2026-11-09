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

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Extensions.Options;

namespace Gurux.DLMS.AMI.Server.Services;

/// <summary>Host-specific storage for uploaded appearance images.</summary>
public sealed class ImageStorageOptions
{
    /// <summary>Absolute storage directory outside the web root; empty uses the service account's local application data.</summary>
    public string RootPath { get; set; } = "";
    /// <summary>Maximum decoded file size in bytes.</summary>
    public int MaxFileSize { get; set; } = 10 * 1024 * 1024;
}

/// <summary>Stores images under generated names. Original names never participate in filesystem paths.</summary>
public sealed partial class GXImageStorage
{
    private readonly string root;
    private readonly int maxSize;
    private readonly ILogger<GXImageStorage> logger;
    private static readonly Regex KeyPattern = MyRegex();

    /// <summary>Validates storage configuration; the directory is created only when writing an image.</summary>
    public GXImageStorage(IOptions<ImageStorageOptions> options, IWebHostEnvironment environment, ILogger<GXImageStorage> logger)
    {
        var path = options.Value.RootPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Gurux", "AMI", "Images");
        }
        if (!Path.IsPathFullyQualified(path))
        {
            throw new ArgumentException("ImageStorage:RootPath must be absolute.");
        }
        root = Path.GetFullPath(path);
        if (!string.IsNullOrEmpty(environment.WebRootPath))
        {
            var relative = Path.GetRelativePath(Path.GetFullPath(environment.WebRootPath), root);
            if (relative == "." || !Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar))
            {
                throw new ArgumentException("Image storage must be outside the web root.");
            }
        }
        maxSize = options.Value.MaxFileSize;
        if (maxSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Image size limit must be positive.");
        }
        this.logger = logger;
    }

    /// <summary>Recognizes file content rather than trusting the supplied extension; returns null for inline icons and HTML symbols.</summary>
    public (byte[] Bytes, string Extension, string ContentType)? Decode(string? value, bool requireImage = false)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }
        if (value.Length > (long)maxSize * 4 / 3 + 1024)
        {
            throw new ArgumentException("Image exceeds the configured size limit.");
        }
        byte[] bytes;
        var svg = value.TrimStart().StartsWith("<svg", StringComparison.OrdinalIgnoreCase) || value.TrimStart().StartsWith("<?xml", StringComparison.Ordinal);
        try
        {
            bytes = svg ? Encoding.UTF8.GetBytes(value) : Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            if (requireImage)
            {
                throw new ArgumentException("Unsupported image content.");
            }
        
            return null;
        }
        if (bytes.Length > maxSize)
        {
            throw new ArgumentException("Image exceeds the configured size limit.");
        }
        string? extension = null, type = null;
        if (bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) { extension = "png"; type = "image/png"; }
        else if (bytes.AsSpan().StartsWith(new byte[] { 255, 216, 255 })) { extension = "jpg"; type = "image/jpeg"; }
        else if (bytes.AsSpan().StartsWith("GIF87a"u8) || bytes.AsSpan().StartsWith("GIF89a"u8)) { extension = "gif"; type = "image/gif"; }
        else if (bytes.AsSpan().StartsWith("BM"u8)) { extension = "bmp"; type = "image/bmp"; }
        else if (bytes.AsSpan().StartsWith(new byte[] { 0, 0, 1, 0 })) { extension = "ico"; type = "image/x-icon"; }
        else if (bytes.Length >= 12 && bytes.AsSpan().StartsWith("RIFF"u8) && bytes.AsSpan(8).StartsWith("WEBP"u8)) { extension = "webp"; type = "image/webp"; }
        else if (svg)
        {
            using var reader = XmlReader.Create(new StringReader(value), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = maxSize });
            reader.MoveToContent();
            if (reader.LocalName != "svg")
            {
                throw new ArgumentException("Invalid SVG image.");
            }
            while (reader.Read()) { }
            extension = "svg"; type = "image/svg+xml";
        }
        if (extension == null) { if (requireImage)
        {
            throw new ArgumentException("Unsupported image content.");
        } return null; }
        return (bytes, extension, type!);
    }

    /// <summary>Creates a new immutable image file and returns its relative storage key.</summary>
    public async Task<string> WriteAsync(byte[] bytes, string extension, CancellationToken token = default)
    {
        if (bytes.Length > maxSize)
        {
            throw new ArgumentException("Image exceeds the configured size limit.");
        }
        var key = DateTimeOffset.UtcNow.ToString("yyyy/MM", CultureInfo.InvariantCulture) + "/" + Guid.NewGuid().ToString("N") + "." + extension;
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        bool created = false;
        try
        {
            await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
            created = true;
            await stream.WriteAsync(bytes, token);
        }
        catch
        {
            if (created)
            {
                DeleteImmediate(key);
            }
        
            throw;
        }
        Repository.GXHistoryScope.OnRollback(() => DeleteImmediate(key));
        return key;
    }

    /// <summary>Reads a generated key; user-supplied paths are never accepted.</summary>
    public async Task<byte[]> ReadAsync(string key, CancellationToken token = default)
    {
        await using var stream = new FileStream(Resolve(key), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 81920, FileOptions.Asynchronous);
        if (stream.Length > maxSize)
        {
            throw new InvalidDataException("Stored image exceeds the configured size limit.");
        }
        var bytes = new byte[checked((int)stream.Length)];
        await stream.ReadExactlyAsync(bytes, token);
        return bytes;
    }

    /// <summary>Attempts cleanup after a committed replacement/delete or failed insertion.</summary>
    public void Delete(string key)
    {
        _ = Resolve(key);
        if (Repository.GXHistoryScope.Defer(() => DeleteImmediate(key))) {
            return;
        }
        DeleteImmediate(key);
    }

    private void DeleteImmediate(string key)
    {
        var path = Resolve(key);
        try
        {
            File.Delete(path);
        }
        catch (Exception error)when (error is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(error, "Could not remove unreferenced image {StorageKey}.", key);
        }
    }

    /// <summary>Retains a display filename without accepting directory components.</summary>
    public static string? OriginalName(string? name)
    {
        var result = name?.Replace('\\', '/').Split('/').LastOrDefault();
        if (result?.Length > 255 || result?.Any(char.IsControl) == true)
        {
            throw new ArgumentException("Invalid original image filename.");
        }
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private string Resolve(string key)
    {
        if (!KeyPattern.IsMatch(key))
        {
            throw new ArgumentException("Invalid image storage key.");
        }
        return Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar));
    }

    [GeneratedRegex(@"\A[0-9]{4}/[0-9]{2}/[0-9a-f]{32}\.(png|jpg|gif|bmp|ico|webp|svg)\z", RegexOptions.CultureInvariant)]
    private static partial Regex MyRegex();
}
