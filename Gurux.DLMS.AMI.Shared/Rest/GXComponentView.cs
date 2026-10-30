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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.Enums;
using Gurux.Service.Orm.Common;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested component view information.</summary>
    [Description("Response containing the requested component view information.")]
    public class GetComponentViewResponse
    {
        /// <summary>
        /// Component view information.
        /// </summary>
        [Description("Component view information.")]
        [IncludeOpenApi(typeof(GXComponentViewGroup),
            nameof(GXComponentViewGroup.Id), nameof(GXComponentViewGroup.Name))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
                nameof(GXUser.UserName))]
        public GXComponentView? Item { get; set; }
    }
    /// <summary>Requests a list of component views.</summary>
    [Description("Requests a list of component views.")]
    [DataContract]
    public class ListComponentViews : IGXRequest<ListComponentViewsResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of component views to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of component views to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter component views.
        /// </summary>
        [Description("Optional criteria used to filter component views.")]
        [ExcludeOpenApi(typeof(GXComponentView),
            nameof(GXComponentView.ComponentViewGroups),
            nameof(GXComponentView.Icon))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.ClassName), nameof(GXComponentView.ConfigurationUI))]
        public GXComponentView? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }

        /// <summary>
        /// Admin user can access component views from all users.
        /// </summary>
        /// <remarks>
        /// If true, component views from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access component views from all users.")]
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

    /// <summary>Response containing the requested component views.</summary>
    [Description("Response containing the requested component views.")]
    [DataContract]
    public class ListComponentViewsResponse
    {
        /// <summary>
        /// List of component view items.
        /// </summary>
        [Description("List of component view items.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXComponentViewGroup),
            nameof(GXComponentViewGroup.ComponentViews),
            nameof(GXComponentViewGroup.UserGroups))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        public IEnumerable<GXComponentView>? ComponentViews { get; set; }

        /// <summary>
        /// Total number of matching component viewrs before paging.
        /// </summary>
        [Description("Total number of matching component viewrs before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests changes to the specified component views.</summary>
    [Description("Requests changes to the specified component views.")]
    [DataContract]
    public class UpdateComponentView : IGXRequest<UpdateComponentViewResponse>
    {
        /// <summary>
        /// ComponentViews to update.
        /// </summary>
        [Description("ComponentViews to update.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXComponentViewGroup),
            nameof(GXComponentViewGroup.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [ExcludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.CreationTime), nameof(GXComponentView.Updated))]
        public IEnumerable<GXComponentView> ComponentViews { get; set; } = default!;
    }

    /// <summary>Response to updating the specified component views.</summary>
    [Description("Response to updating the specified component views.")]
    [DataContract]
    public class UpdateComponentViewResponse
    {
        /// <summary>Identifiers of the added or updated component view records.</summary>
        [Description("Identifiers of the added or updated component view records.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests deletion of the selected component views.</summary>
    [Description("Requests deletion of the selected component views.")]
    [DataContract]
    public class RemoveComponentView : IGXRequest<RemoveComponentViewResponse>
    {
        /// <summary>Identifiers of the component view records to delete.</summary>
        [Description("Identifiers of the component view records to delete.")]
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

    /// <summary>Response to deleting the selected component views.</summary>
    [Description("Response to deleting the selected component views.")]
    [DataContract]
    public class RemoveComponentViewResponse
    {
    }


    /// <summary>
    /// Refresh component views.
    /// </summary>
    [Description("Refresh component views.")]
    [DataContract]
    public class RefreshComponentView : IGXRequest<RefreshComponentViewResponse>
    {
    }

    /// <summary>
    /// Reply from Refresh component views.
    /// </summary>
    [Description("Reply from Refresh component views.")]
    [DataContract]
    public class RefreshComponentViewResponse
    {
        /// <summary>
        /// True, if there are new component views avialable.
        /// </summary>
        [Description("True, if there are new component views avialable.")]
        public bool NewItems { get; set; }
    }
}
