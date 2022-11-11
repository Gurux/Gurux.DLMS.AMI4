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
using System.ComponentModel;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Update device template information. Device template is added if ID is zero.
    /// </summary>
    [DataContract]
    [Description("Add or Update device template information. Device template is added if ID is zero.")]
    public class UpdateDeviceTemplate : IGXRequest<UpdateDeviceTemplateResponse>
    {
        /// <summary>
        /// Inserted or updated device templates.
        /// </summary>
        [DataMember]
        public GXDeviceTemplate[] Templates
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Device template response.
    /// </summary>
    [DataContract]
    [Description("Insert or update device template response.")]
    public class UpdateDeviceTemplateResponse
    {
        /// <summary>
        /// New device template identifier(s).
        /// </summary>
        [DataMember]
        public Guid[] Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from available device templates.
    /// </summary>
    [DataContract]
    public class ListDeviceTemplates : IGXRequest<ListDeviceTemplatesResponse>
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
        /// Amount of the device templates to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter device templates.
        /// </summary>
        public GXDeviceTemplate? Filter
        {
            get;
            set;
        }


        /// <summary>
        /// Admin user can access templates from all users.
        /// </summary>
        /// <remarks>
        /// If true, templates from all users are retreaved, not just current user. 
        /// </remarks>
        public bool AllUsers
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Available devices templates response.
    /// </summary>
    [DataContract]
    [Description("Available devices templates response.")]
    public class ListDeviceTemplatesResponse
    {
        /// <summary>
        /// List of device templates.
        /// </summary>
        [DataMember]
        [Description("List of device templates.")]
        public GXDeviceTemplate[] Templates
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the device templates.
        /// </summary>
        [DataMember]
        [Description("Total count of the device templates.")]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete device template.
    /// </summary>
    [DataContract]
    public class DeviceTemplateDelete : IGXRequest<DeviceTemplateDeleteResponse>
    {
        /// <summary>
        /// Removed device identifier(s).
        /// </summary>
        [Description("Removed device identifier(s).")]
        [DataMember]
        public Guid[] Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete device template response.
    /// </summary>
    [DataContract]
    [Description("Delete device template response.")]
    public class DeviceTemplateDeleteResponse
    {
    }
}
