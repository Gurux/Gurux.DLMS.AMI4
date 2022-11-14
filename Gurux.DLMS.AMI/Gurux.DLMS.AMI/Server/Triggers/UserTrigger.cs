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

using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Server.Triggers
{
    /// <summary>
    /// User trigger type.
    /// </summary>
    public class UserTrigger : ITriggerAction
    {
        /// <inheritdoc cref="ITriggerAction.Name"/>
        public string Name => "User";

        /// <inheritdoc cref="ITriggerAction.ConfigurationUI"/>
        public Type? ConfigurationUI => null;

        /// <inheritdoc cref="ITriggerAction.Icon"/>
        public string? Icon => "oi oi-person";

        /// <summary>
        /// A new User has created.
        /// </summary>
        public static string Create = "Create";
        /// <summary>
        /// User has login from unknown IP address.
        /// </summary>
        public static string NewLogin = "NewLogin";
        /// <summary>
        /// User has deleted.
        /// </summary>
        public static string Delete = "Delete";
        /// <summary>
        /// User has login.
        /// </summary>
        public static string Login = "Login";
        /// <summary>
        /// User has logout.
        /// </summary>
        public static string Logout = "Logout";

        /// <summary>
        /// User has modified the account.
        /// </summary>
        public static string Modify = "Modify";
    }
}
