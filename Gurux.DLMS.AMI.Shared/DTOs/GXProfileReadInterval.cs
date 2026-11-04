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

namespace Gurux.DLMS.AMI.Shared.DTOs;

/// <summary>An authorized interval that an agent reads completely before committing.</summary>
public sealed class GXProfileReadInterval
{
    /// <summary>Identifier of the persisted read interval.</summary>
    public Guid IntervalId { get; set; }
    /// <summary>Identifier of the claimed task.</summary>
    public Guid TaskId { get; set; }
    /// <summary>Identifier of the Profile Generic buffer attribute.</summary>
    public Guid AttributeId { get; set; }
    /// <summary>Inclusive start instant.</summary>
    public DateTimeOffset Start { get; set; }
    /// <summary>Exclusive end instant.</summary>
    public DateTimeOffset End { get; set; }
}
