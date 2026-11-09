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

using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Gurux.DLMS.AMI.Update;

/// <summary>Downloads bounded catalog icons for inline module rendering.</summary>
internal static class GXCatalogModuleIcon
{
    const int MaximumBytes = 1024 * 1024;
    internal static readonly HttpClient Client = new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    internal static async Task<string> ReadAsync(string url, HttpClient client, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(client.Timeout != Timeout.InfiniteTimeSpan && client.Timeout < TimeSpan.FromSeconds(15)
            ? client.Timeout : TimeSpan.FromSeconds(15));
        token = deadline.Token;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || uri.UserInfo.Length != 0)
            {
            throw new InvalidDataException("Catalog module icon must have an absolute HTTPS URL.");
        }
        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > MaximumBytes)
            {
            throw new InvalidDataException("Catalog module icon exceeds 1 MiB.");
        }
        await using var source = await response.Content.ReadAsStreamAsync(token);
        using var destination = new MemoryStream();
        var buffer = new byte[8192];
        int read;
        while ((read = await source.ReadAsync(buffer, token)) != 0)
        {
            if (destination.Length + read > MaximumBytes)
                {
                throw new InvalidDataException("Catalog module icon exceeds 1 MiB.");
            }
            destination.Write(buffer, 0, read);
        }
        byte[] bytes = destination.ToArray();
        if (bytes.Length == 0)
            {
            throw new InvalidDataException("Catalog module icon is empty.");
        }
        string? mime = RasterMime(bytes);
        if (mime is not null)
            {
            return $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 64 64\"><image width=\"64\" height=\"64\" href=\"data:{mime};base64,{Convert.ToBase64String(bytes)}\" /></svg>";
        }
        try
        {
            using var stream = new MemoryStream(bytes);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaximumBytes
            });
            var root = XDocument.Load(reader).Root ?? throw new InvalidDataException("Catalog module icon has no SVG root.");
            if (root.Name.LocalName != "svg" || root.Name.NamespaceName is not ("" or "http://www.w3.org/2000/svg"))
                {
                throw new InvalidDataException("Catalog module icon is not SVG.");
            }
            if (root.DescendantNodes().OfType<XProcessingInstruction>().Any())
                {
                throw new InvalidDataException("SVG processing instructions are not supported.");
            }
            string[] allowed = ["svg", "g", "path", "rect", "circle", "ellipse", "line", "polyline", "polygon", "title", "desc",
                "defs", "linearGradient", "radialGradient", "stop", "clipPath", "mask", "pattern", "symbol", "use", "text", "tspan"];
            string[] attributes = ["id", "role", "aria-label", "aria-labelledby", "width", "height", "viewBox", "fill",
                "fill-opacity", "fill-rule", "stroke", "stroke-width", "stroke-linecap", "stroke-linejoin", "stroke-opacity",
                "stroke-dasharray", "stroke-dashoffset", "stroke-miterlimit", "opacity", "d", "x", "y", "x1", "x2", "y1", "y2",
                "cx", "cy", "r", "rx", "ry", "points", "transform", "clip-path", "clip-rule", "mask", "gradientUnits",
                "gradientTransform", "offset", "stop-color", "stop-opacity", "spreadMethod", "fx", "fy", "fr", "patternUnits",
                "patternContentUnits", "patternTransform", "preserveAspectRatio", "href", "font-size", "font-family",
                "font-weight", "text-anchor", "dominant-baseline", "dx", "dy", "rotate", "lengthAdjust", "textLength", "space"];
            foreach (var element in root.DescendantsAndSelf())
            {
                if (element.Name.Namespace != root.Name.Namespace || !allowed.Contains(element.Name.LocalName))
                    {
                    throw new InvalidDataException("Active or unsupported SVG element in catalog icon.");
                }
                foreach (var attribute in element.Attributes().Where(a => !a.IsNamespaceDeclaration))
                {
                    string name = attribute.Name.LocalName, value = attribute.Value;
                    if (!attributes.Contains(name) || value.Contains('\\') ||
                        name == "href" && !value.StartsWith('#') ||
                        Regex.IsMatch(value, @"javascript\s*:|@import|expression\s*\(", RegexOptions.IgnoreCase) ||
                        Regex.IsMatch(value, @"url\s*\(", RegexOptions.IgnoreCase) &&
                        !Regex.IsMatch(value, @"^url\(\s*#[A-Za-z0-9_.:-]+\s*\)$", RegexOptions.IgnoreCase))
                        {
                        throw new InvalidDataException("Active or external SVG attribute in catalog icon.");
                    }
                }
            }
            return root.ToString(SaveOptions.DisableFormatting);
        }
        catch (XmlException error) { throw new InvalidDataException("Invalid catalog SVG icon.", error); }
    }

    static string? RasterMime(byte[] bytes)
    {
        var span = bytes.AsSpan();
        if (span.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            {
            return "image/png";
        }
        if (span.StartsWith(new byte[] { 255, 216, 255 }))
            {
            return "image/jpeg";
        }
        if (span.StartsWith("GIF87a"u8) || span.StartsWith("GIF89a"u8))
            {
            return "image/gif";
        }
        if (span.Length >= 12 && span[..4].SequenceEqual("RIFF"u8) && span[8..12].SequenceEqual("WEBP"u8))
            {
            return "image/webp";
        }
        if (span.StartsWith(new byte[] { 0, 0, 1, 0 }))
            {
            return "image/x-icon";
        }
        return null;
    }
}
