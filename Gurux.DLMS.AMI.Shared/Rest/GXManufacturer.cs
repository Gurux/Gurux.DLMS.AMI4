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
using Gurux.Service.Orm.Common;
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
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [ExcludeOpenApi(typeof(GXDeviceModel), nameof(GXDeviceModel.Manufacturer))]
        [ExcludeOpenApi(typeof(GXDeviceVersion), nameof(GXDeviceVersion.Model))]
        [ExcludeOpenApi(typeof(GXDeviceSettings), nameof(GXDeviceSettings.Version), nameof(GXDeviceSettings.Template))]
        [IncludeOpenApi(typeof(GXManufacturerGroup), nameof(GXManufacturerGroup.Id))]
        public GXManufacturer? Item { get; set; }
    }

    /// <summary>
    /// Get model data by ID.
    /// </summary>
    public class GetModel
    {
        /// <summary>
        /// Model information.
        /// </summary>        
        public Guid? Id { get; set; }
    }

    /// <summary>
    /// Get model response.
    /// </summary>
    public class GetModelResponse
    {
        /// <summary>
        /// Model information.
        /// </summary>        
        [IncludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.Id))]
        [IncludeOpenApi(typeof(GXDeviceVersion), nameof(GXDeviceVersion.Id))]
        public GXDeviceModel? Item { get; set; }
    }

    /// <summary>
    /// Get version data by ID.
    /// </summary>
    public class GetVersion
    {
        /// <summary>
        /// Version information.
        /// </summary>        
        public Guid? Id { get; set; }
    }

    /// <summary>
    /// Get version response.
    /// </summary>
    public class GetVersionResponse
    {
        /// <summary>
        /// Version information.
        /// </summary>        
        [ExcludeOpenApi(typeof(GXDeviceVersion), nameof(GXDeviceVersion.Settings))]
        [IncludeOpenApi(typeof(GXDeviceModel), nameof(GXDeviceModel.Id))]
        public GXDeviceVersion? Item { get; set; }
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
        [IncludeOpenApi(typeof(GXDeviceModel), nameof(GXDeviceModel.Id), nameof(GXDeviceModel.Versions))]
        [IncludeOpenApi(typeof(GXDeviceVersion), nameof(GXDeviceVersion.Id), nameof(GXDeviceVersion.Settings))]
        [ExcludeOpenApi(typeof(GXDeviceSettings), nameof(GXDeviceSettings.Version), nameof(GXDeviceSettings.Template),
            nameof(GXDeviceSettings.CreationTime), nameof(GXDeviceSettings.InstallationTime),
            nameof(GXDeviceSettings.Removed), nameof(GXDeviceSettings.Updated))]
        [IncludeOpenApi(typeof(GXManufacturerGroup), nameof(GXManufacturerGroup.Id))]
        [ExcludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.CreationTime), nameof(GXManufacturer.Removed), nameof(GXManufacturer.Updated))]
        public IEnumerable<GXManufacturer>? Manufacturers { get; set; }
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
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Get available manufacturers.
    /// </summary>
    [DataContract]
    public class ListManufacturers : IGXRequest<ListManufacturersResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of manufacturers to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter manufacturers.
        /// </summary>
        [ExcludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.Models), nameof(GXManufacturer.ManufacturerGroups))]
        public GXManufacturer? Filter { get; set; }

        /// <summary>
        /// Admin user can access manufacturers from all users.
        /// </summary>
        /// <remarks>
        /// If true, manufacturers from all users are retrieved, not just current user.
        /// </remarks>
        public bool AllUsers { get; set; }

        /// <summary>
        /// Selected extra information.
        /// </summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        public IEnumerable<string>? Select { get; set; }

        /// <summary>
        /// Name of the property to sort by.
        /// </summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        public bool Descending { get; set; }

        /// <summary>
        /// Identifiers to include in the results.
        /// </summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        public IEnumerable<Guid>? Exclude { get; set; }
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
        [IncludeOpenApi(typeof(GXDeviceModel), nameof(GXDeviceModel.Id), nameof(GXDeviceModel.Versions))]
        [IncludeOpenApi(typeof(GXDeviceVersion), nameof(GXDeviceVersion.Id), nameof(GXDeviceVersion.Settings))]
        [ExcludeOpenApi(typeof(GXDeviceSettings), nameof(GXDeviceSettings.Version),
            nameof(GXDeviceSettings.CreationTime), nameof(GXDeviceSettings.InstallationTime),
            nameof(GXDeviceSettings.Removed), nameof(GXDeviceSettings.Updated))]
        [IncludeOpenApi(typeof(GXManufacturerGroup), nameof(GXManufacturerGroup.Id))]
        [ExcludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.CreationTime), nameof(GXManufacturer.Removed), nameof(GXManufacturer.Updated))]
        //Select device template ID and name.
        [IncludeOpenApi(typeof(GXDeviceTemplate), nameof(GXDeviceTemplate.Id), nameof(GXDeviceTemplate.Name))]
        public IEnumerable<GXManufacturer>? Manufacturers { get; set; }

        /// <summary>
        /// Total number of matching manufacturers before paging.
        /// </summary>
        [DataMember]
        public int Count { get; set; }
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
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>
        /// Whether to permanently delete the items from the database.
        /// </summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [DataMember]
        [Required]
        public bool Delete { get; set; }
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
        [IncludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.Id))]
        public IEnumerable<GXManufacturer>? Manufacturers { get; set; }

        /// <summary>
        /// Installed model IDs.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXDeviceModel), nameof(GXDeviceModel.Id))]
        public IEnumerable<GXDeviceModel>? Models { get; set; }

        /// <summary>
        /// Installed version IDs.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXDeviceVersion), nameof(GXDeviceVersion.Id))]
        public IEnumerable<GXDeviceVersion>? Versions { get; set; }

        /// <summary>
        /// Installed settings IDs.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXDeviceSettings), nameof(GXDeviceSettings.Id))]
        public IEnumerable<GXDeviceSettings>? Settings { get; set; }
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
        /// <summary>The completed background execution ID.</summary>
        [DataMember]
        public Guid RunId { get; set; }
    }
}
