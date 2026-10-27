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

namespace Gurux.DLMS.AMI.Shared.Enums;

/// <summary>Managed secret lifecycle. Numeric values are not wire protocol values.</summary>
[System.ComponentModel.Description("Managed secret lifecycle. Numeric values are not wire protocol values.")]
public enum KeyState
{
    /// <summary>The key has been registered but is not yet active.</summary>
    [System.ComponentModel.Description("The key has been registered but is not yet active.")]
    PreActive = 1,
    /// <summary>The key is active and available for permitted operations.</summary>
    [System.ComponentModel.Description("The key is active and available for permitted operations.")]
    Active,
    /// <summary>The key has been deactivated and is no longer available for new protection operations.</summary>
    [System.ComponentModel.Description("The key has been deactivated and is no longer available for new protection operations.")]
    Deactivated,
    /// <summary>The key is known or suspected to be compromised.</summary>
    [System.ComponentModel.Description("The key is known or suspected to be compromised.")]
    Compromised,
    /// <summary>The key material has been destroyed.</summary>
    [System.ComponentModel.Description("The key material has been destroyed.")]
    Destroyed,
    /// <summary>The key material has been destroyed after being marked as compromised.</summary>
    [System.ComponentModel.Description("The key material has been destroyed after being marked as compromised.")]
    DestroyedCompromised
}
