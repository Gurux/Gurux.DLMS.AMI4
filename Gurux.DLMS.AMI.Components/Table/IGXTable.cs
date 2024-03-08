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

namespace Gurux.DLMS.AMI.Components.Table
{
    /// <summary>
    /// Sort mode.
    /// </summary>
    public interface IGXTable
    {
        /// <summary>
        /// Sorted column.
        /// </summary>
        string? OrderBy { get; set; }
        /// <summary>
        /// Sort mode.
        /// </summary>
        SortMode SortMode { get; set; }

        /// <summary>
        /// Notify that sort has been updated.
        /// </summary>
        void NotifyShortChange();

        /// <summary>
        /// User has selected the row.
        /// </summary>
        /// <param name="selected">Selected row.</param>
        void SelectRow(object selected);

        /// <summary>
        /// User has selected the cell.
        /// </summary>
        /// <param name="selected">Selected cell.</param>
        void SelectCell(object selected);

        /// <summary>
        /// Is edit allowed.
        /// </summary>
        bool CanEdit { get;}

        /// <summary>
        /// Is column hidden.
        /// </summary>
        /// <param name="name">Column name.</param>
        /// <returns>True, if column is hidden.</returns>
        bool IsHidden(string? name);
    }
}