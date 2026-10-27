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
using System.Xml.Serialization;

namespace Gurux.DLMS.AMI.Shared.Enums
{
    /// <summary>Determines how a policy combines its requirements.</summary>
    [System.ComponentModel.Description("Determines how a policy combines its requirements.")]
    public enum PolicyRequirementOperator : byte
    {
        /// <summary>Satisfy the policy when at least one requirement is met.</summary>
        [System.ComponentModel.Description("Satisfy the policy when at least one requirement is met.")]
        [XmlEnum("0")]
        Or,
        /// <summary>Satisfy the policy only when all requirements are met.</summary>
        [System.ComponentModel.Description("Satisfy the policy only when all requirements are met.")]
        [XmlEnum("1")]
        And
    }
}
