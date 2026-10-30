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
namespace Gurux.DLMS.AMI.Shared
{
    /// <summary>
    /// Specifies an inclusive date and time range for a property in a list query.
    /// </summary>
    /// <remarks>
    /// Either bound can be omitted to define an open-ended range. When both bounds
    /// are specified, a value must satisfy both. Null property values do not match
    /// a specified bound. The receiving service validates the property and applies
    /// the filter; this class only describes the requested range.
    /// </remarks>
    public sealed class GXDateTimeFilter
    {
        /// <summary>
        /// Gets or sets the name of the date and time property to filter.
        /// </summary>
        /// <remarks>
        /// Use the DTO property name, for example <c>nameof(GXUser.LastLogin)</c>.
        /// The property must be supported by the receiving list operation.
        /// </remarks>
        public required string Property { get; set; }

        /// <summary>
        /// Gets or sets the inclusive lower bound. Values must be greater than or
        /// equal to this bound. Null means that no lower bound is applied.
        /// </summary>
        public DateTimeOffset? From { get; set; }

        /// <summary>
        /// Gets or sets the inclusive upper bound. Values must be less than or
        /// equal to this bound. Null means that no upper bound is applied.
        /// </summary>
        public DateTimeOffset? To { get; set; }
    }
}
