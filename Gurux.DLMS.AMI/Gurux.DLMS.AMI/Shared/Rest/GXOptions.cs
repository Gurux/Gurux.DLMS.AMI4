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
using Gurux.Common;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{    
    /// <summary>
    /// Get configuration settings.
    /// </summary>
    [DataContract]
    public class ListOptions : IGXRequest<ListConfigurationResponse>
    {
        /// <summary>
        /// Configuration.
        /// </summary>
        public string Configuration
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get configuration settings reply.
    /// </summary>
    [DataContract]
    public class ListOptionsResponse
    {
        /// <summary>
        /// Configuration settings as JSON.
        /// </summary>
        [DataMember]
        public string? Settings
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get site information.
    /// </summary>
    [DataContract]
    public class UpdateOptions : IGXRequest<UpdateOptionsResponse>
    {
        /// <summary>
        /// Configuration.
        /// </summary>
        public string Configuration
        {
            get;
            set;
        }      

        /// <summary>
        /// Settings data type.
        /// </summary>
        [DataMember]
        public string SettingsType
        {
            get;
            set;
        }

        /// <summary>
        /// Configuration settings as JSON.
        /// </summary>
        [DataMember]
        public string Settings
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get site information reply.
    /// </summary>
    [DataContract]
    public class UpdateOptionsResponse
    {
    }
}
