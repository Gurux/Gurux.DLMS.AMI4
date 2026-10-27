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

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested manufacturer information.</summary>
    [Description("Response containing the requested manufacturer information.")]
    public class GetManufacturerResponse
    {
        /// <summary>
        /// Manufacturer information.
        /// </summary>        
        [Description("Manufacturer information.")]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [ExcludeOpenApi(typeof(GXDeviceModel), nameof(GXDeviceModel.Manufacturer))]
        [ExcludeOpenApi(typeof(GXDeviceVersion), nameof(GXDeviceVersion.Model))]
        [ExcludeOpenApi(typeof(GXDeviceSettings), nameof(GXDeviceSettings.Version), nameof(GXDeviceSettings.Template))]
        [IncludeOpenApi(typeof(GXManufacturerGroup), nameof(GXManufacturerGroup.Id))]
        public GXManufacturer? Item { get; set; }
    }

    /// <summary>Requests the specified model information.</summary>
    [Description("Requests the specified model information.")]
    public class GetModel
    {
        /// <summary>
        /// Model information.
        /// </summary>        
        [Description("Model information.")]
        public Guid? Id { get; set; }
    }

    /// <summary>Response containing the requested model information.</summary>
    [Description("Response containing the requested model information.")]
    public class GetModelResponse
    {
        /// <summary>
        /// Model information.
        /// </summary>        
        [Description("Model information.")]
        [IncludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.Id))]
        [IncludeOpenApi(typeof(GXDeviceVersion), nameof(GXDeviceVersion.Id))]
        public GXDeviceModel? Item { get; set; }
    }

    /// <summary>Requests the specified version information.</summary>
    [Description("Requests the specified version information.")]
    public class GetVersion
    {
        /// <summary>
        /// Version information.
        /// </summary>        
        [Description("Version information.")]
        public Guid? Id { get; set; }
    }

    /// <summary>Response containing the requested version information.</summary>
    [Description("Response containing the requested version information.")]
    public class GetVersionResponse
    {
        /// <summary>
        /// Version information.
        /// </summary>        
        [Description("Version information.")]
        [ExcludeOpenApi(typeof(GXDeviceVersion), nameof(GXDeviceVersion.Settings))]
        [IncludeOpenApi(typeof(GXDeviceModel), nameof(GXDeviceModel.Id))]
        public GXDeviceVersion? Item { get; set; }
    }

    /// <summary>Requests changes to the specified manufacturers.</summary>
    [Description("Requests changes to the specified manufacturers.")]
    [DataContract]
    public class UpdateManufacturer : IGXRequest<UpdateManufacturerResponse>
    {
        /// <summary>
        /// Inserted or updated manufacturers.
        /// </summary>
        [Description("Inserted or updated manufacturers.")]
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

    /// <summary>Response to updating the specified manufacturers.</summary>
    [Description("Response to updating the specified manufacturers.")]
    [DataContract]
    public class UpdateManufacturerResponse
    {
        /// <summary>Identifiers of the added or updated manufacturer records.</summary>
        [Description("Identifiers of the added or updated manufacturer records.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests a list of manufacturers.</summary>
    [Description("Requests a list of manufacturers.")]
    [DataContract]
    public class ListManufacturers : IGXRequest<ListManufacturersResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of manufacturers to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of manufacturers to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter manufacturers.
        /// </summary>
        [Description("Optional criteria used to filter manufacturers.")]
        [ExcludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.Models), nameof(GXManufacturer.ManufacturerGroups))]
        [ExcludeOpenApi(typeof(GXManufacturer),
            nameof(GXManufacturer.Logo), nameof(GXManufacturer.Url), nameof(GXManufacturer.Version),
            nameof(GXManufacturer.AvailableVersion), nameof(GXManufacturer.ProfilesInstalled), nameof(GXManufacturer.ConcurrencyStamp))]
        public GXManufacturer? Filter { get; set; }

        /// <summary>
        /// Admin user can access manufacturers from all users.
        /// </summary>
        /// <remarks>
        /// If true, manufacturers from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access manufacturers from all users.")]
        public bool AllUsers { get; set; }

        /// <summary>Additional related information to include in the response. Reserved for future use.</summary>
        /// <remarks>
        /// This is reserved for later use.
        /// </remarks>
        [Description("Additional related information to include in the response. Reserved for future use.")]
        public IEnumerable<string>? Select { get; set; }

        /// <summary>Name of the property to sort by. If omitted, the endpoint uses its default ordering.</summary>
        /// <remarks>
        /// Uses the endpoint's default ordering when not specified.
        /// </remarks>
        /// <seealso cref="Descending"/>
        [Description("Name of the property to sort by. If omitted, the endpoint uses its default ordering.")]
        public string? OrderBy { get; set; }

        /// <summary>
        /// Whether to sort results in descending order.
        /// </summary>
        /// <seealso cref="OrderBy"/>
        [Description("Whether to sort results in descending order.")]
        public bool Descending { get; set; }

        /// <summary>Identifiers to include in the results. A nonempty collection restricts results to these identifiers.</summary>
        /// <remarks>
        /// A nonempty collection restricts the results to these identifiers.
        /// </remarks>
        [Description("Identifiers to include in the results. A nonempty collection restricts results to these identifiers.")]
        public IEnumerable<Guid>? Included { get; set; }

        /// <summary>
        /// Identifiers to exclude from the results.
        /// </summary>
        /// <remarks>
        /// Items with these identifiers are omitted from the results.
        /// </remarks>
        [Description("Identifiers to exclude from the results.")]
        public IEnumerable<Guid>? Exclude { get; set; }
    }

    /// <summary>Response containing the requested manufacturers.</summary>
    [Description("Response containing the requested manufacturers.")]
    [DataContract]
    public class ListManufacturersResponse
    {
        /// <summary>
        /// List of manufacturers.
        /// </summary>
        [Description("List of manufacturers.")]
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
        [Description("Total number of matching manufacturers before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests deletion of the selected manufacturers.</summary>
    [Description("Requests deletion of the selected manufacturers.")]
    [DataContract]
    public class RemoveManufacturer : IGXRequest<RemoveManufacturerResponse>
    {
        /// <summary>Identifiers of the manufacturer records to delete.</summary>
        [Description("Identifiers of the manufacturer records to delete.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;

        /// <summary>Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.</summary>
        /// <remarks>
        /// If false, marks the items as removed and retains their database records.
        /// </remarks>
        [Description("Whether to permanently delete the items from the database. If false, marks the items as removed and retains their records.")]
        [DataMember]
        [Required]
        public bool Delete { get; set; }
    }

    /// <summary>Response to deleting the selected manufacturers.</summary>
    [Description("Response to deleting the selected manufacturers.")]
    [DataContract]
    public class RemoveManufacturerResponse
    {
    }


    /// <summary>
    /// Install device templates for the manufacturers.
    /// </summary>
    [Description("Install device templates for the manufacturers.")]
    [DataContract]
    public class InstallManufacturers : IGXRequest<InstallManufacturersResponse>
    {
        /// <summary>Installed manufacturers identifiers.</summary>
        [Description("Installed manufacturers identifiers.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXManufacturer), nameof(GXManufacturer.Id))]
        public IEnumerable<GXManufacturer>? Manufacturers { get; set; }

        /// <summary>Installed model identifiers.</summary>
        [Description("Installed model identifiers.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXDeviceModel), nameof(GXDeviceModel.Id))]
        public IEnumerable<GXDeviceModel>? Models { get; set; }

        /// <summary>Installed version identifiers.</summary>
        [Description("Installed version identifiers.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXDeviceVersion), nameof(GXDeviceVersion.Id))]
        public IEnumerable<GXDeviceVersion>? Versions { get; set; }

        /// <summary>Installed settings identifiers.</summary>
        [Description("Installed settings identifiers.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXDeviceSettings), nameof(GXDeviceSettings.Id))]
        public IEnumerable<GXDeviceSettings>? Settings { get; set; }
    }

    /// <summary>
    /// Install device templates response.
    /// </summary>
    [Description("Install device templates response.")]
    [DataContract]
    public class InstallManufacturersResponse
    {
    }

    /// <summary>
    /// Check if there are new manufacturer settings available.
    /// </summary>
    [Description("Check if there are new manufacturer settings available.")]
    [DataContract]
    public class CheckManufacturer : IGXRequest<CheckManufacturerResponse>
    {
    }

    /// <summary>
    /// Check manufacturer settings reply.
    /// </summary>
    [Description("Check manufacturer settings reply.")]
    [DataContract]
    public class CheckManufacturerResponse
    {
        /// <summary>The completed background execution identifier.</summary>
        [Description("The completed background execution identifier.")]
        [DataMember]
        public Guid RunId { get; set; }
    }
}
