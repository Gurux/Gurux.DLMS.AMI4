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

namespace Gurux.DLMS.AMI.Components
{

    /// <summary>
    /// Theme style.
    /// </summary>
    public class GXThemeStyle
    {
        /// <summary>
        /// Style name.
        /// </summary>
        public string Name { get; set; } = default!;
        /// <summary>
        /// Style description.
        /// </summary>
        public string? Description { get; set; }
        /// <summary>
        /// Style value.
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// Style type.
        /// </summary>
        public byte Type { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXThemeStyle()
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">Theme type.</param>
        /// <param name="name">Name</param>
        /// <param name="description">Description</param>
        /// <param name="value">Value</param>
        public GXThemeStyle(byte type, string name, string? description, string? value)
        {
            Type = type;
            Name = name;
            Description = description;
            Value = value;
        }
    }

    /// <summary>
    /// Theme info
    /// </summary>
    public class GXThemeInfo
    {
        /// <summary>
        /// Theme name.
        /// </summary>
        public string Name { get; set; } = default!;
        /// <summary>
        /// Theme configuration file name.
        /// </summary>
        public string CssFile { get; set; } = default!;
        /// <summary>
        /// Custom theme styles.
        /// </summary>
        public List<GXThemeStyle> Styles { get; set; } = new List<GXThemeStyle>();
    }
}
