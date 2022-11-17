
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

namespace Gurux.DLMS.AMI.Update
{
    /// <summary>
    /// Gurux.DLMS.AMI version information.
    /// </summary>
    public class GXVersion
    {
        /// <summary>
        /// Creation time.
        /// </summary>
        public DateTime CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Version number.
        /// </summary>
        public string Number
        {
            get;
            set;
        }

        /// <summary>
        /// Url.
        /// </summary>
        public string? Url
        {
            get;
            set;
        }

        /// <summary>
        /// File name.
        /// </summary>
        public string? FileName
        {
            get;
            set;
        }

        /// <summary>
        /// Version description.
        /// </summary>
        public string? Description
        {
            get;
            set;
        }

        /// <summary>
        /// Is this version pre-release.
        /// </summary>
        public bool Prerelease
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Number;
        }
    }
}