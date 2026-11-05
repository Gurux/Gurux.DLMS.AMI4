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
