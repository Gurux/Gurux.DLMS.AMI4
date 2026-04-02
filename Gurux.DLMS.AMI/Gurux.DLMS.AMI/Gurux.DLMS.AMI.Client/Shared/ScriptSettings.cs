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

namespace Gurux.DLMS.AMI.Client.Shared
{
    /// <summary>
    /// Script settings.
    /// </summary>
    public class ScriptSettings
    {
        /// <summary>
        /// Scripts' versions.
        /// </summary>
        /// <remarks>
        /// All scripts must be rebuild if the references like Gurux.DLMS.AMI.Shared changes.
        /// </remarks>
        public string? Versions
        {
            get;
            set;
        }

        /// <summary>
        /// Current shared version.
        /// </summary>
        /// <remarks>
        /// All scripts must be rebuild if the Gurux.DLMS.AMI.Shared changes.
        /// </remarks>
        public string? CurrentVersions
        {
            get;
            set;
        }
    }
}