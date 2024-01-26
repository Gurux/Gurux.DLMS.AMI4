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

using Gurux.DLMS.AMI.Module;

namespace Gurux.DLMS.AMI.Client.Development.DeviceImporter
{
    /// <summary>
    /// Device importer module.
    /// </summary>
    public class DeviceImporterModule : GXAmiModuleBase
    {
        /// <inheritdoc/>
        public override string Id
        {
            get
            {
                return "Gurux.Device.Importer";
            }
        }

        /// <inheritdoc/>
        public override string Name
        {
            get
            {
                return "Gurux Device Importer";
            }
        }

        /// <inheritdoc/>
        public override string Description
        {
            get
            {
                return "This module is used to import meters from the comma separated file.";
            }
        }

        /// <inheritdoc/>
        public override Type? Extension
        {
            get
            {
                return typeof(DeviceImporterExtension);
            }
        }
    }
}
