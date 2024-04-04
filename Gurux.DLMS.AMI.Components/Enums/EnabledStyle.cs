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

namespace Gurux.DLMS.AMI.Components.Enums
{
    /// <summary>
    /// When the component is enabled.
    /// </summary>
    public enum EnableStyle : byte
    {
        /// <summary>
        /// Component is never enabled.
        /// </summary>
        None,
        /// <summary>
        /// Component is always enabled.
        /// </summary>
        Always,
        /// <summary>
        /// Component is enabled when user has edit the content of the page.
        /// </summary>
        Modified,
        /// <summary>
        /// Component is enabled when user has not edit the content of the page.
        /// </summary>
        Unmodified,
    }
}
