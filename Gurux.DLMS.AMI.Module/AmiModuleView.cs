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
namespace Gurux.DLMS.AMI.Module;

/// <summary>Serializable UI registration; contains no runtime Type references.</summary>
/// <param name="Area">Host location in which the component is displayed.</param>
/// <param name="Component">Fully qualified component type name within the module.</param>
/// <param name="Policy">Authorization policy required to display the component.</param>
/// <param name="Key">Optional view identifier, unique within the module and area ignoring case.</param>
/// <param name="Title">Optional display title; the host uses the module identifier when omitted.</param>
/// <param name="Icon">Optional icon displayed with the view's tab.</param>
/// <param name="Order">Display order within the host location; lower values appear first.</param>
/// <param name="Display">How the component participates in the host location; defaults to a tab.</param>
public sealed record AmiModuleView(AmiViewArea Area, string Component, string Policy,
    string? Key = null, string? Title = null, string? Icon = null, int Order = 0,
    AmiViewDisplay Display = AmiViewDisplay.Tab);
