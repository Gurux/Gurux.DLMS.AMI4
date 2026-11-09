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

using System.Text;
using System.Text.RegularExpressions;
using Gurux.DLMS.AMI.Module;

namespace Gurux.DLMS.AMI.Server.Token;

/// <summary>Resolves plain-text tokens once, using only the current operation's providers and context.</summary>
public sealed partial class GXTemplateTokenResolver(IEnumerable<IGXAmiTemplateTokenProvider> providers,
    IGXAmiModuleServices modules) : IGXAmiTemplateTokenResolver
{
    private const int MaximumTextLength = 65536;
    private const int MaximumResultLength = 262144;

    /// <inheritdoc />
    public async Task<IReadOnlyList<AmiTemplateTokenDefinition>> GetDefinitionsAsync(
        AmiTemplateTokenContext context, CancellationToken cancellationToken = default)
    {
        var definitions = new Dictionary<string, AmiTemplateTokenDefinition>(StringComparer.Ordinal);
        foreach (var provider in providers)
            {
            foreach (var definition in await provider.GetDefinitionsAsync(context, cancellationToken))
                {
                definitions.TryAdd(definition.Name, definition);
            }
        }
        if (modules.IsAvailable<IGXAmiTemplateTokenProvider>())
        {
            try
            {
                var extra = await modules.InvokeAsync<IGXAmiTemplateTokenProvider, IReadOnlyList<AmiTemplateTokenDefinition>>(
                    (provider, ct) => provider.GetDefinitionsAsync(context, ct), cancellationToken);
                foreach (var definition in extra)
                    {
                    definitions.TryAdd(definition.Name, definition);
                }
            }
            catch (AmiModuleUnavailableException) { /* A module may stop between discovery and invocation. */ }
        }
        cancellationToken.ThrowIfCancellationRequested();
        return definitions.Values.OrderBy(d => d.Name, StringComparer.Ordinal).ToArray();
    }

    /// <inheritdoc />
    public async Task<string> ResolveAsync(string text, AmiTemplateTokenContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(context);
        if (text.Length > MaximumTextLength)
            {
            throw new ArgumentException("Template text must not exceed 65536 characters.", nameof(text));
        }
        cancellationToken.ThrowIfCancellationRequested();
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        var result = new StringBuilder();
        int position = 0;
        foreach (Match match in TokenPattern().Matches(text))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string name = match.Groups[1].Value;
            if (!values.TryGetValue(name, out var value))
            {
                foreach (var provider in providers)
                {
                    value = await provider.ResolveAsync(name, context, cancellationToken);
                    if (value != null)
                        {
                        break;
                    }
                }
                if (value == null && modules.IsAvailable<IGXAmiTemplateTokenProvider>())
                {
                    try
                    {
                        value = await modules.InvokeAsync<IGXAmiTemplateTokenProvider, string?>(
                            (provider, ct) => provider.ResolveAsync(name, context, ct), cancellationToken);
                    }
                    catch (AmiModuleUnavailableException) { }
                }
                values[name] = value;
            }
            result.Append(text, position, match.Index - position);
            result.Append(value ?? match.Value);
            if (result.Length > MaximumResultLength)
                {
                throw new InvalidOperationException("Resolved template text is too long.");
            }
            position = match.Index + match.Length;
        }
        result.Append(text, position, text.Length - position);
        if (result.Length > MaximumResultLength)
            {
            throw new InvalidOperationException("Resolved template text is too long.");
        }
        return result.ToString();
    }

    [GeneratedRegex(@"\[([A-Za-z][A-Za-z0-9_-]{0,63}:[A-Za-z0-9_.:-]{1,128})\]", RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();
}
