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

namespace Gurux.DLMS.AMI.Client.Shared.Enums
{
    /// <summary>
    /// Defines How can create new users.
    /// </summary>
    public enum UserCreationType : byte
    {
        /// <summary>
        /// Anyone can register new user accounts.
        /// </summary>
        Anyone = 0,
        /// <summary>
        /// Anyone can register new user accounts, but the user must be manually approved 
        /// by an administrator or user manager before they can log in.
        /// </summary>
        AdminApprovalRequired = 1,
        /// <summary>
        /// Only an administrator is allowed to create new user accounts.
        /// Regular self-registration is disabled.
        /// </summary>
        AdminOnly = 2
    }
}
