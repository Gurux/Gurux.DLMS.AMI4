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

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Assembly information.
    /// </summary>
    [System.ComponentModel.Description("Assembly information.")]
    public class GXAssembly
    {
        /// <summary>
        /// Assembly name.
        /// </summary>
        [System.ComponentModel.Description("Assembly name.")]
        public string Name { get; set; } = "";
        /// <summary>
        /// Assembly version.
        /// </summary>
        [System.ComponentModel.Description("Assembly version.")]
        public string Version { get; set; } = "";
        /// <summary>
        /// Assembly location.
        /// </summary>
        [System.ComponentModel.Description("Assembly location.")]
        public string? Location { get; set; } = "";
        /// <summary>Whether the assembly contains a script.</summary>
        [System.ComponentModel.Description("Whether the assembly contains a script.")]
        public bool? IsScript { get; set; }
    }
}
