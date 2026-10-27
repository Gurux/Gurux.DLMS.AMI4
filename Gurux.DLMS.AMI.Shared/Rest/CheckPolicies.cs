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

namespace Gurux.DLMS.AMI.Shared.Rest;

/// <summary>Policies to evaluate for the current HTTP user.</summary>
[System.ComponentModel.Description("Policies to evaluate for the current HTTP user.")]
public sealed class CheckPolicies
{
    /// <summary>Gets or sets the names of the policies to evaluate for the current user.</summary>
    [System.ComponentModel.Description("Gets or sets the names of the policies to evaluate for the current user.")]
    public string[] PolicyNames { get; set; } = [];
}

/// <summary>Server authorization decisions, without policy definitions.</summary>
[System.ComponentModel.Description("Server authorization decisions, without policy definitions.")]
public sealed class CheckPoliciesResponse
{
    /// <summary>Gets or sets the authorization decision for each evaluated policy name.</summary>
    [System.ComponentModel.Description("Gets or sets the authorization decision for each evaluated policy name.")]
    public Dictionary<string, bool> Results { get; set; } = new(StringComparer.Ordinal);
}
