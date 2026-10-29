//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

namespace Gurux.DLMS.AMI.Module;

/// <summary>Provides template token definitions and values from the server or a module.</summary>
public interface IAmiTemplateTokenProvider
{
    /// <summary>Lists tokens available to the user in the specified context.</summary>
    /// <param name="context">User, culture and optional target used to determine available tokens.</param>
    /// <param name="cancellationToken">Token used to cancel discovery.</param>
    /// <returns>Definitions of tokens available in this context.</returns>
    Task<IReadOnlyList<AmiTemplateTokenDefinition>> GetDefinitionsAsync(
        AmiTemplateTokenContext context, CancellationToken cancellationToken = default);

    /// <summary>Resolves a qualified token name after checking the user's access.</summary>
    /// <param name="name">Token name without brackets, for example site:name.</param>
    /// <param name="context">User, culture and optional target used for resolution.</param>
    /// <param name="cancellationToken">Token used to cancel resolution.</param>
    /// <returns>A plain text value, or null when the token is unknown or unavailable. An empty string is a valid value.</returns>
    /// <remarks>Providers must enforce user permissions and return values without HTML encoding.</remarks>
    Task<string?> ResolveAsync(string name, AmiTemplateTokenContext context,
        CancellationToken cancellationToken = default);
}
