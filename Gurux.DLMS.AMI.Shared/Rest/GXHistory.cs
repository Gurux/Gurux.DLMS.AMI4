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

using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.Service.Orm.Common;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested history information.</summary>
    [Description("Response containing the requested history information.")]
    public class GetHistoryResponse
    {
        /// <summary>
        /// The requested history item.
        /// </summary>        
        [Description("The requested history item.")]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language))]
        [IncludeOpenApi(typeof(GXRole), nameof(GXRole.Id))]
        public GXChangeHistoryItem? Item { get; set; }
    }

    /// <summary>Requests a list of history items.</summary>
    [Description("Requests a list of history items.")]
    [DataContract]
    public class ListHistoryItems : IGXRequest<ListHistoryItemsResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of history items to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of history items to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter history items.
        /// </summary>
        [Description("Optional criteria used to filter history items.")]
        public GXChangeHistoryItem? Filter { get; set; }

        /// <summary>Gets or sets inclusive date and time ranges for properties of the filter type.</summary>
        /// <remarks>
        /// Ranges are combined with each other and with <see cref="Filter"/> using AND.
        /// Specify a public stored date and time property name. Null or an empty collection
        /// applies no additional date restrictions. Filtering precedes counting and paging.
        /// </remarks>
        [DataMember]
        [Description("Inclusive date and time ranges combined with the other filters using AND.")]
        public IEnumerable<GXDateTimeFilter>? DateFilters { get; set; }

        /// <summary>
        /// Admin user can access history items from all users.
        /// </summary>
        /// <remarks>
        /// If true, history items from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access history items from all users.")]
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

    /// <summary>Response containing the requested history items.</summary>
    [Description("Response containing the requested history items.")]
    [DataContract]
    public class ListHistoryItemsResponse
    {
        /// <summary>
        /// List of history items.
        /// </summary>
        [Description("List of history items.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        public IEnumerable<GXChangeHistoryItem>? Items { get; set; }

        /// <summary>
        /// Total number of matching history items before paging.
        /// </summary>
        [Description("Total number of matching history items before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests restoration of a saved entity state as a new latest revision, preserving existing history.</summary>
    [Description("Requests restoration of a saved entity state as a new latest revision, preserving existing history.")]
    [DataContract]
    public class RestoreHistoryItem : IGXRequest<RestoreHistoryItemResponse>
    {
        /// <summary>Gets or sets the identifier of the history item containing the saved entity state to restore.</summary>
        [Description("Identifier of the history item containing the saved entity state to restore.")]
        [DataMember(IsRequired = true)]
        [Required]
        public Guid HistoryItemId { get; set; }

        /// <summary>Gets or sets the expected current revision of the target entity.</summary>
        /// <remarks>The restoration must fail if the entity has changed since this revision was read.</remarks>
        [Description("Expected current revision of the target entity. Restoration fails if the revision has changed.")]
        [DataMember(IsRequired = true)]
        [Required]
        public long ExpectedRevision { get; set; }
    }

    /// <summary>Response containing the identifier of the new history item created by the restoration.</summary>
    [Description("Response containing the identifier of the new history item created by the restoration.")]
    [DataContract]
    public class RestoreHistoryItemResponse
    {
        /// <summary>Gets or sets the identifier of the new history item created by the restoration.</summary>
        [Description("Identifier of the new history item created by the restoration.")]
        [DataMember]
        public Guid Id { get; set; }
    }
}
