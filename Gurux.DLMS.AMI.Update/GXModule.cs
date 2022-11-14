﻿//
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
    public class GXModule
    {
        /// <summary>
        /// Module name.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Installation Url.
        /// </summary>
        public string? Url
        {
            get;
            set;
        }

        /// <summary>
        /// Folder name.
        /// </summary>
        public string FolderName
        {
            get;
            set;
        }

        /// <summary>
        /// Module description.
        /// </summary>
        public string Desription
        {
            get;
            set;
        }
        /// <summary>
        /// Module versions.
        /// </summary>
        public List<GXVersion> Versions
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXModule()
        {
            Versions = new List<GXVersion>();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Name;
        }
    }
}