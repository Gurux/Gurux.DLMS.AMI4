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
namespace Gurux.DLMS.AMI.Module.Enums
{
    /// <summary>
    /// Visible type defines how extended UI is handled on the UI.
    /// </summary>
    public enum ExtendedlUIType
    {
        /// <summary>
        /// Extended UI is not shown to the target.
        /// </summary>
        None,
        /// <summary>
        /// Extended UI is append after the target default settings.
        /// </summary>
        Append,
        /// <summary>
        /// The default UI is replaced with extended UI.
        /// </summary>
        /// <remarks>
        /// If there are multiple extended UIs all must replace 
        /// the default UI or default UI is shown. 
        /// </remarks>
        Replace
    }
}