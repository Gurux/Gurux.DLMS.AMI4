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

/// <summary>Discovers template tokens and replaces bracketed tokens using registered providers.</summary>
public interface IAmiTemplateTokenResolver
{
    /// <summary>Lists tokens available from registered providers in the specified context.</summary>
    /// <param name="context">User, culture and optional target used to determine available tokens.</param>
    /// <param name="cancellationToken">Token used to cancel discovery.</param>
    /// <returns>Definitions of tokens available in this context.</returns>
    Task<IReadOnlyList<AmiTemplateTokenDefinition>> GetDefinitionsAsync(
        AmiTemplateTokenContext context, CancellationToken cancellationToken = default);

    /// <summary>Replaces bracketed tokens, for example [site:name], with their plain text values.</summary>
    /// <param name="text">Template text containing bracketed tokens.</param>
    /// <param name="context">User, culture and optional target used for resolution.</param>
    /// <param name="cancellationToken">Token used to cancel resolution.</param>
    /// <returns>The resolved text with unknown or unavailable tokens left unchanged.</returns>
    /// <remarks>Output encoding belongs to the caller and must match the destination, for example HTML or plain text.</remarks>
    Task<string> ResolveAsync(string text, AmiTemplateTokenContext context,
        CancellationToken cancellationToken = default);
}
