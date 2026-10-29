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

using System.Globalization;
using System.Security.Claims;

namespace Gurux.DLMS.AMI.Module;

/// <summary>Identifies the user, culture and optional target for template token resolution.</summary>
public sealed class AmiTemplateTokenContext
{
    /// <summary>User whose permissions must be enforced when resolving token values.</summary>
    public required ClaimsPrincipal User { get; init; }

    /// <summary>Culture used for localized text, numbers and dates.</summary>
    public CultureInfo Culture { get; init; } = CultureInfo.InvariantCulture;

    /// <summary>Optional page scope, for example maintenance. This does not grant authorization.</summary>
    public string? Page { get; init; }
    /// <summary>Optional page values supplied for a draft or a known server-side snapshot.</summary>
    public IReadOnlyDictionary<string, string?>? Values { get; init; }

    /// <summary>Optional target category, for example Device.</summary>
    public string? TargetType { get; init; }

    /// <summary>Optional identifier of the target associated with the template.</summary>
    public string? TargetId { get; init; }
}
