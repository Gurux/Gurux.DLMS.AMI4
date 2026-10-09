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

namespace Gurux.DLMS.AMI.Shared.Enums
{
    /// <summary>
    /// Specifies the lifecycle states of an Ami module.
    /// </summary>
    public enum AmiModuleState
    {
        /// <summary>
        /// Module is loading. It is not yet ready to be used.
        /// </summary>
        Loading,
        /// <summary>
        /// Module is active and ready to be used.
        /// </summary>
        Active,
        /// <summary>
        /// Module is stopping. It is not yet fully stopped and may still be processing requests.
        /// </summary>
        Stopping,
        /// <summary>
        /// Module is stopped. It is not processing any requests and may be unloaded from memory.
        /// </summary>
        Stopped,
        /// <summary>
        /// Module failed to load or encountered an error during operation. It may not be usable until the issue is resolved.
        /// </summary>
        Failed
    }
}