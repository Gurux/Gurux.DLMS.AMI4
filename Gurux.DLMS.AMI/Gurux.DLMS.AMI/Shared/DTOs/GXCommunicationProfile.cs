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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// Communication profile.
    /// </summary>
    public class GXCommunicationProfile
    {
        /// <summary>
        /// Used interface type.
        /// </summary>
        [Description("Interface type.")]
        public int InterfaceType
        {
            get;
            set;
        }

        /// <summary>
        /// Used logical client ID.
        /// </summary>
        /// <remarks>
        /// Client ID is always 1 byte long in HDLC and 2 bytes long when WRAPPER is used.
        /// </remarks>
        [DefaultValue(0x10)]
        [Description("Client address.")]
        [Range(0, 127, ErrorMessage = "Value must be between 1 to 127")]

        public int ClientAddress
        {
            get;
            set;
        }

        /// <summary>
        /// Used Physical address.
        /// </summary>
        /// <remarks>
        /// Server HDLC Address (Logical + Physical address)  might be 1,2 or 4 bytes long.
        /// </remarks>
        [DefaultValue(1)]
        [Range(1, 16383, ErrorMessage = "Value must be between 1 to 16383")]
        virtual public int PhysicalAddress
        {
            get;
            set;
        }

        /// <summary>
        /// Used logical address. Logical address is used only with HDLC.
        /// </summary>
        [DefaultValue(0)]
        [Range(0, 16383, ErrorMessage = "Value must be between 0 to 16383")]
        public int LogicalAddress
        {
            get;
            set;
        }
    }   
}
