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
using Gurux.DLMS.AMI.Shared.DTOs.Module;
using System.ComponentModel;
using Gurux.Service.Orm.Common;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Common identity, value and version contract for named properties associated with resources.
    /// </summary>
    [Description("Common identity, value and version contract for named properties associated with resources.")]
    public interface IGXProperty : IUnique<Guid>
    {
        /// <summary>Writable property identifier.</summary>
        [Description("Writable property identifier.")]
        new Guid Id { get; set; }

        /// <summary>
        /// Module settings unique name.
        /// </summary>
        [Description("Module settings unique name.")]
        public string? Name { get; set; }


        /// <summary>
        /// The module whose settings these are.
        /// </summary>
        [Description("The module whose settings these are.")]
        public GXModule? Module { get; set; }

        /// <summary>
        /// Module settings.
        /// </summary>
        [Description("Module settings.")]
        public string? Value { get; set; }

        /// <summary>
        /// Version expected by optimistic updates and deletes.
        /// </summary>
        [Description("Version expected by optimistic updates and deletes.")]
        public string? ConcurrencyStamp { get; set; }
    }
}
