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
using System.Xml;
using System.Xml.Linq;
using Gurux.DLMS.AMI.Client.Shared;

namespace Gurux.DLMS.AMI.Server.Internal;

/// <summary>Bounded RSS/Atom retrieval without executing feed markup or resolving XML entities.</summary>
internal static class GXBlockRssFeed
{
    internal static Uri ValidateAddress(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException("Enter an absolute HTTP or HTTPS RSS feed URL.");
        }
        return uri;
    }

    internal static async Task<GXBlockFeed> ReadAsync(HttpClient http, string? address, CancellationToken token)
    {
        var uri = ValidateAddress(address);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > 2 * 1024 * 1024)
        {
            throw new InvalidDataException("RSS feed is too large.");
        }
        await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
        using var content = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(buffer, deadline.Token)) != 0)
        {
            if (content.Length + count > 2 * 1024 * 1024)
            {
                throw new InvalidDataException("RSS feed is too large.");
            }
            await content.WriteAsync(buffer.AsMemory(0, count), deadline.Token);
        }
        content.Position = 0;
        return await ParseAsync(content, uri, deadline.Token);
    }

    internal static async Task<GXBlockFeed> ParseAsync(Stream stream, Uri address, CancellationToken token)
    {
        using var xml = XmlReader.Create(stream, new XmlReaderSettings
        {
            Async = true, DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null,
            MaxCharactersInDocument = 2 * 1024 * 1024
        });
        var document = await XDocument.LoadAsync(xml, LoadOptions.None, token);
        var root = document.Root ?? throw new InvalidDataException("Empty feed.");
        bool atom = root.Name == XName.Get("feed", "http://www.w3.org/2005/Atom");
        var channel = root.Elements().FirstOrDefault(e => e.Name.LocalName == "channel");
        if (!atom && root.Name.LocalName is not ("rss" or "RDF"))
        {
            throw new InvalidDataException("Not an RSS or Atom feed.");
        }
        var result = new GXBlockFeed { Title = Text(atom ? root : channel, "title") };
        var items = atom ? root.Elements().Where(e => e.Name.LocalName == "entry") :
            (channel ?? root).Elements().Where(e => e.Name.LocalName == "item");
        if (!atom && root.Name.LocalName == "RDF")
        {
            items = root.Elements().Where(e => e.Name.LocalName == "item");
        }
        foreach (var item in items.Take(20))
        {
            var link = atom ? item.Elements().FirstOrDefault(e => e.Name.LocalName == "link" &&
                (e.Attribute("rel") == null || (string?)e.Attribute("rel") == "alternate"))?.Attribute("href")?.Value : Text(item, "link");
            string? safeLink = !string.IsNullOrWhiteSpace(link) && Uri.TryCreate(address, link, out var target) && target.Scheme is "http" or "https" ? target.AbsoluteUri : null;
            var date = Text(item, atom ? "published" : "pubDate") ?? Text(item, atom ? "updated" : "date");
            result.Items.Add(new GXBlockFeedItem
            {
                Title = Text(item, "title"), Link = safeLink,
                Published = DateTimeOffset.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var published) ? published : null
            });
        }
        return result;
    }

    private static string? Text(XElement? element, string name) => element?.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value;
}

