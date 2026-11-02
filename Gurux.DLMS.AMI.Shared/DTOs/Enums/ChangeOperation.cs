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

namespace Gurux.DLMS.AMI.Shared.DTOs.Enums
{
    /// <summary>Specifies the operation performed on the entity by a recorded change.</summary>
    [System.ComponentModel.Description("Specifies the operation performed on the entity by a recorded change.")]
    public enum ChangeOperation
    {
        /// <summary>The entity was created; the history item has no previous JSON snapshot.</summary>
        [System.ComponentModel.Description("The entity was created; the history item has no previous JSON snapshot.")]
        [XmlEnum("0")]
        Create,
        /// <summary>The entity was modified; the history item contains JSON snapshots before and after the change.</summary>
        [System.ComponentModel.Description("The entity was modified; the history item contains JSON snapshots before and after the change.")]
        [XmlEnum("1")]
        Update,
        /// <summary>The entity was deleted; the history item has no subsequent JSON snapshot.</summary>
        [System.ComponentModel.Description("The entity was deleted; the history item has no subsequent JSON snapshot.")]
        [XmlEnum("2")]
        Delete
    }
}
