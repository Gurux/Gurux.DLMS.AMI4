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

using Gurux.DLMS.AMI.Module.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Module;

/// <summary>Required identity and inclusive minimum/exclusive maximum compatibility versions.</summary>
/// <param name="Kind">Whether the requirement identifies a host assembly, another module, or an exported service.</param>
/// <param name="Id">Assembly name, module identifier, or fully qualified service type name, according to <paramref name="Kind"/>.</param>
/// <param name="MinimumVersion">Inclusive minimum compatibility version, or <see langword="null"/> for no lower bound.</param>
/// <param name="MaximumVersionExclusive">Exclusive maximum compatibility version, or <see langword="null"/> for no upper bound.</param>
public sealed record AmiModuleDependency(AmiModuleDependencyKind Kind, string Id, string? MinimumVersion = null, string? MaximumVersionExclusive = null);
