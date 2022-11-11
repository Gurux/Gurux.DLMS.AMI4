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
using System.Runtime.Serialization;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get user settings from the DB.
    /// </summary>
    [DataContract]
    public class ListUserSettings
    {
        /// <summary>
        /// Start index.
        /// </summary>
        public int Index
        {
            get;
            set;

        }

        /// <summary>
        /// Amount of the user settings to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter user settings.
        /// </summary>
        public GXValue? Filter
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get user settings reply.
    /// </summary>
    [DataContract]
    public class ListUserSettingsResponse
    {
        /// <summary>
        /// List of user settings items.
        /// </summary>
        [DataMember]
        public GXUserSetting[] Settings
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the user settings items.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }
}
