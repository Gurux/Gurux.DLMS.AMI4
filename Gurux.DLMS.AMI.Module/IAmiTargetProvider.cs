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

using Gurux.DLMS.AMI.Shared;
using System.Data;
using System.Security.Claims;
using System.Text.Json;
namespace Gurux.DLMS.AMI.Module;

/// <summary>Resolves module targets after checking user access.</summary>
public interface IAmiTargetProvider
{
    /// <summary>Resolves an accessible target for the specified user, or returns null.</summary>
    /// <param name="user">User whose access to the target must be checked.</param>
    /// <param name="target">Module-defined target category.</param>
    /// <param name="targetId">Identifier of the requested target.</param>
    /// <param name="cancellationToken">Token used to cancel target resolution.</param>
    /// <returns>The target's display name and navigation path, or <see langword="null"/> when unavailable to the user.</returns>
    Task<GXAmiTarget?> ResolveAsync(ClaimsPrincipal user, string target, string targetId, CancellationToken cancellationToken);
}
