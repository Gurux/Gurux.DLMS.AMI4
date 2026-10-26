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

using System.Text.Json.Serialization;

namespace Gurux.DLMS.AMI.Module
{
    /// <summary>
    /// Describes a component that can be displayed inside an AMI UI block.
    /// </summary>
    public interface IAmiComponent
    {
        /// <summary>
        /// Gets the display name of the component view.
        /// </summary>
        string Name
        {
            get;
        }

        /// <summary>
        /// Gets the configuration component type, or <see langword="null"/> when no configuration UI is provided.
        /// </summary>
        [JsonIgnore]
        Type? ConfigurationUI
        {
            get;
        }

        /// <summary>
        /// Gets a CSS icon name or Base64 image, or <see langword="null"/> when no icon is provided.
        /// </summary>
        [JsonIgnore]
        string? Icon
        {
            get;
        }
    }
}
