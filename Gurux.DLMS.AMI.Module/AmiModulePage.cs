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

/// <summary>Page component exposed by a module.</summary>
/// <param name="Component">Fully qualified component type name.</param>
/// <param name="Policy">Required access policy.</param>
/// <param name="Route">Unique module page URL segment.</param>
public sealed record AmiModulePage(string Route, string Component, string Policy)
{
    /// <summary>Optional title displayed for the module page.</summary>
    public string? Title
    {
        get; init;
    }
    /// <summary>Whether the page appears in module navigation.</summary>
    public bool Navigation
    {
        get; init;
    }
}
