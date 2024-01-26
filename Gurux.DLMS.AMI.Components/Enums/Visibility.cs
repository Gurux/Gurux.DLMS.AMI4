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
    /// Responsive determines the order 
    /// in which the contents are hidden if they do not fit the screen.
    /// </summary>
    public enum Visibility
    {
        /// <summary>
        /// Content is always shown.
        /// </summary>
        All,
        /// <summary>
        /// Content is hidden for screens smaller than extra large.
        /// </summary>
        ExtraLargeLarge,
        /// <summary>
        /// Content is hidden for screens smaller than extra large.
        /// </summary>
        ExtraLarge,
        /// <summary>
        /// Content is hidden for screens smaller than large.
        /// </summary>
        Large,
        /// <summary>
        /// Content is hidden for screens smaller medium.
        /// </summary>
        Medium,
        /// <summary>
        /// Content is hidden for screens smaller than medium.
        /// </summary>
        Small,
        /// <summary>
        /// Content is hidden for screens smaller than small.
        /// </summary>
        ExtraSmall,
        /// <summary>
        /// Content is shown only when printing.
        /// </summary>
        Print,
        /// <summary>
        /// Content is hidden when printing.
        /// </summary>
        PrintHide
    }
}
