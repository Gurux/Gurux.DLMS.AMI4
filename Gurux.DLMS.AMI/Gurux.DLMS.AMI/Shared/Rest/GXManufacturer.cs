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
using Gurux.DLMS.AMI.Shared.DTOs.Device;
using Gurux.DLMS.AMI.Shared.DTOs.Manufacturer;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get manufacturer.
    /// </summary>
    public class GetManufacturerResponse
    {
        /// <summary>
        /// Manufacturer information.
        /// </summary>        
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [ExcludeSwagger(typeof(GXDeviceModel), nameof(GXDeviceModel.Manufacturer))]
        [ExcludeSwagger(typeof(GXDeviceVersion), nameof(GXDeviceVersion.Model))]
        [ExcludeSwagger(typeof(GXDeviceSettings), nameof(GXDeviceSettings.Version))]
        [IncludeSwagger(typeof(GXManufacturerGroup), nameof(GXManufacturerGroup.Id))]
        public GXManufacturer? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get model data by ID.
    /// </summary>
    public class GetModel
    {
        /// <summary>
        /// Model information.
        /// </summary>        
        public Guid? Id
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get model response.
    /// </summary>
    public class GetModelResponse
    {
        /// <summary>
        /// Model information.
        /// </summary>        
        [IncludeSwagger(typeof(GXManufacturer), nameof(GXManufacturer.Id))]
        [IncludeSwagger(typeof(GXDeviceVersion), nameof(GXDeviceVersion.Id))]
        public GXDeviceModel? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get version data by ID.
    /// </summary>
    public class GetVersion
    {
        /// <summary>
        /// Version information.
        /// </summary>        
        public Guid? Id
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get version response.
    /// </summary>
    public class GetVersionResponse
    {
        /// <summary>
        /// Version information.
        /// </summary>        
        [ExcludeSwagger(typeof(GXDeviceVersion), nameof(GXDeviceVersion.Settings))]
        [IncludeSwagger(typeof(GXDeviceModel), nameof(GXDeviceModel.Id))]        
        public GXDeviceVersion? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add or Update manufacturer information.
    /// </summary>
    [DataContract]
    public class UpdateManufacturer : IGXRequest<UpdateManufacturerResponse>
    {
        /// <summary>
        /// Inserted or updated manufacturers.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXDeviceModel), nameof(GXDeviceModel.Id), nameof(GXDeviceModel.Versions))]
        [IncludeSwagger(typeof(GXDeviceVersion), nameof(GXDeviceVersion.Id), nameof(GXDeviceVersion.Settings))]
        [ExcludeSwagger(typeof(GXDeviceSettings), nameof(GXDeviceSettings.Version),
            nameof(GXDeviceSettings.CreationTime), nameof(GXDeviceSettings.InstallationTime),
            nameof(GXDeviceSettings.Removed), nameof(GXDeviceSettings.Updated))]
        [IncludeSwagger(typeof(GXManufacturerGroup), nameof(GXManufacturerGroup.Id))]
        [ExcludeSwagger(typeof(GXManufacturer), nameof(GXManufacturer.CreationTime), nameof(GXManufacturer.Removed), nameof(GXManufacturer.Updated))]
        public GXManufacturer[] Manufacturers
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update manufacturer response.
    /// </summary>
    [DataContract]
    public class UpdateManufacturerResponse
    {
        /// <summary>
        /// New manufacturer identifier(s).
        /// </summary>
        [DataMember]
        public Guid[] Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get available manufacturers.
    /// </summary>
    [DataContract]
    public class ListManufacturers : IGXRequest<ListManufacturersResponse>
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
        /// Amount of the manufacturers to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter manufacturers.
        /// </summary>
        [ExcludeSwagger(typeof(GXManufacturer), nameof(GXManufacturer.Models), nameof(GXManufacturer.ManufacturerGroups))]
        public GXManufacturer? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access devices from all users.
        /// </summary>
        /// <remarks>
        /// If true, devices from all users are retreaved, not just current user. 
        /// </remarks>
        public bool AllUsers
        {
            get;
            set;
        }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public string[]? Select
        {
            get;
            set;
        }

        /// <summary>
        /// Order by name.
        /// </summary>
        /// <remarks>
        /// Default order by is used if this is not set.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy
        {
            get;
            set;
        }

        /// <summary>
        /// Are values shown as descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending
        {
            get;
            set;
        }

        /// <summary>
        /// Included Ids.
        /// </summary>
        /// <remarks>
        /// Included Ids can be used to get only part of large data.
        /// </remarks>
        public Guid[]? Included
        {
            get;
            set;
        }

        /// <summary>
        /// Excluded Ids.
        /// </summary>
        /// <remarks>
        /// Excluded Ids can be used to filter data.
        /// </remarks>
        public Guid[]? Exclude
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Returns manufacturers.
    /// </summary>
    [DataContract]
    public class ListManufacturersResponse
    {
        /// <summary>
        /// List of manufacturers.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXDeviceModel), nameof(GXDeviceModel.Id), nameof(GXDeviceModel.Versions))]
        [IncludeSwagger(typeof(GXDeviceVersion), nameof(GXDeviceVersion.Id), nameof(GXDeviceVersion.Settings))]
        [ExcludeSwagger(typeof(GXDeviceSettings), nameof(GXDeviceSettings.Version),
            nameof(GXDeviceSettings.CreationTime), nameof(GXDeviceSettings.InstallationTime),
            nameof(GXDeviceSettings.Removed), nameof(GXDeviceSettings.Updated))]
        [IncludeSwagger(typeof(GXManufacturerGroup), nameof(GXManufacturerGroup.Id))]
        [ExcludeSwagger(typeof(GXManufacturer), nameof(GXManufacturer.CreationTime), nameof(GXManufacturer.Removed), nameof(GXManufacturer.Updated))]
        //Select device template ID and name.
        [IncludeSwagger(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id), nameof(GXDeviceTemplate.Name))]
        public GXManufacturer[] Manufacturers
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the manufacturers.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove manufacturer.
    /// </summary>
    [DataContract]
    public class RemoveManufacturer : IGXRequest<RemoveManufacturerResponse>
    {
        /// <summary>
        /// Removed manufacturers.
        /// </summary>
        [DataMember]
        public Guid[] Ids
        {
            get;
            set;
        }

        /// <summary>
        /// Items are removed from the database.
        /// </summary>
        /// <remarks>
        /// If false, the Removed date is set for the items, but items are kept on the database.
        /// </remarks>
        [DataMember]
        [Required]
        public bool Delete
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Remove manufacturers response.
    /// </summary>
    [DataContract]
    public class RemoveManufacturerResponse
    {
    }


    /// <summary>
    /// Install device templates for the manufacturers.
    /// </summary>
    [DataContract]
    public class InstallManufacturers : IGXRequest<InstallManufacturersResponse>
    {
        /// <summary>
        /// Installed manufacturers IDs.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXManufacturer), nameof(GXManufacturer.Id))]
        public GXManufacturer[]? Manufacturers
        {
            get;
            set;
        }

        /// <summary>
        /// Installed model IDs.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXDeviceModel), nameof(GXDeviceModel.Id))]
        public GXDeviceModel[]? Models
        {
            get;
            set;
        }

        /// <summary>
        /// Installed version IDs.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXDeviceVersion), nameof(GXDeviceVersion.Id))]
        public GXDeviceVersion[]? Versions
        {
            get;
            set;
        }

        /// <summary>
        /// Installed settings IDs.
        /// </summary>
        [DataMember]
        [IncludeSwagger(typeof(GXDeviceSettings), nameof(GXDeviceSettings.Id))]
        public GXDeviceSettings[]? Settings
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Install device templates response.
    /// </summary>
    [DataContract]
    public class InstallManufacturersResponse
    {
    }

    /// <summary>
    /// Check if there are new manufacturer settings available.
    /// </summary>
    [DataContract]
    public class CheckManufacturer : IGXRequest<CheckManufacturerResponse>
    {
    }

    /// <summary>
    /// Check manufacturer settings reply.
    /// </summary>
    [DataContract]
    public class CheckManufacturerResponse
    {
    }
}
