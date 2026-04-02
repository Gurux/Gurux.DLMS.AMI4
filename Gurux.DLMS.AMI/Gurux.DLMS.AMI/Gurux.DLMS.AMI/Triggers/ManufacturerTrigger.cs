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
// This file is a part of Gurux Manufacturer Framework.
//
// Gurux Manufacturer Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Manufacturer Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Triggers
{
    /// <summary>
    /// Manufacturer trigger type.
    /// </summary>
    public class ManufacturerTrigger : ITriggerAction
    {
        /// <inheritdoc />
        public string Name => "Manufacturer";

        /// <inheritdoc />
        public Type? ConfigurationUI => null;

        /// <inheritdoc />
        public string? Icon => "oi share-boxed";

        /// <summary>
        /// A new manufacturer is added.
        /// </summary>
        public static string Add = "Add";
        /// <summary>
        /// Manufacturer is removed.
        /// </summary>
        public static string Removed = "Removed";
    }
}
