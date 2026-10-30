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
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.User;
using Gurux.Service.Orm.Common;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

using System.ComponentModel;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>Response containing the requested block information.</summary>
    [Description("Response containing the requested block information.")]
    public class GetBlockResponse
    {
        /// <summary>
        /// Block information.
        /// </summary>        
        [Description("Block information.")]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXBlockGroup), nameof(GXBlockGroup.Id), nameof(GXBlockGroup.Name))]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [IncludeOpenApi(typeof(GXBlockGroup), nameof(GXBlockGroup.Id))]
        [ExcludeOpenApi(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language))]
        [ExcludeOpenApi(typeof(GXBlock),
            nameof(GXBlock.Parent))]
        [IncludeOpenApi(typeof(GXLanguage), nameof(GXLanguage.Id),
            nameof(GXLanguage.EnglishName),
            nameof(GXLanguage.NativeName))]
        public GXBlock? Item { get; set; }
    }

    /// <summary>Requests a list of blocks.</summary>
    [Description("Requests a list of blocks.")]
    [DataContract]
    public class ListBlocks : IGXRequest<ListBlocksResponse>
    {
        /// <summary>Zero-based index of the first item to retrieve. Use 0 to start from the first item.</summary>
        [DefaultValue(0)]
        [Description("Zero-based index of the first item to retrieve. Use 0 to start from the first item.")]
        public long Index { get; set; }

        /// <summary>Maximum number of blocks to retrieve. Use 0 to return all matching items without paging.</summary>
        [DefaultValue(0)]
        [Description("Maximum number of blocks to retrieve. Use 0 to return all matching items without paging.")]
        public long Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter blocks.
        /// </summary>
        [Description("Optional criteria used to filter blocks.")]
        [ExcludeOpenApi(typeof(GXBlock),
            nameof(GXBlock.BlockGroups),
            nameof(GXBlock.User),
            nameof(GXBlock.Creator),
            nameof(GXBlock.Parent),
            nameof(GXBlock.ComponentView),
            nameof(GXBlock.ScriptMethod))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
            nameof(GXUser.UserName))]
        [ExcludeOpenApi(typeof(GXBlock),
            nameof(GXBlock.Title), nameof(GXBlock.BlockType), nameof(GXBlock.CssClass),
            nameof(GXBlock.CssRole), nameof(GXBlock.Style), nameof(GXBlock.Body),
            nameof(GXBlock.PageVisibility), nameof(GXBlock.Pages), nameof(GXBlock.Closable), nameof(GXBlock.PublishTime),
            nameof(GXBlock.EndTime), nameof(GXBlock.ConcurrencyStamp))]
        public GXBlock? Filter { get; set; }

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
        /// Admin user can access blocks from all users.
        /// </summary>
        /// <remarks>
        /// If true, blocks from all users are retrieved, not just current user.
        /// </remarks>
        [Description("Admin user can access blocks from all users.")]
        public bool AllUsers { get; set; }

        /// <summary>
        /// Exclude blocks closed by the current user when loading displayed content.
        /// Management lists leave this false so closed blocks remain editable.
        /// </summary>
        [Description("Exclude blocks closed by the current user when loading displayed content. Management lists leave this false so closed blocks remain editable.")]
        public bool ExcludeClosed { get; set; }

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

    /// <summary>Response containing the requested blocks.</summary>
    [Description("Response containing the requested blocks.")]
    [DataContract]
    public class ListBlocksResponse
    {
        /// <summary>
        /// List of block items.
        /// </summary>
        [Description("List of block items.")]
        [DataMember]
        [ExcludeOpenApi(typeof(GXBlock), nameof(GXBlock.BlockGroups),
            nameof(GXBlock.User),
            nameof(GXBlock.Parent))]
        [IncludeOpenApi(typeof(GXBlockGroup), nameof(GXBlockGroup.Id), nameof(GXBlockGroup.Name))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        public IEnumerable<GXBlock>? Blocks { get; set; }

        /// <summary>
        /// Total number of matching blockrs before paging.
        /// </summary>
        [Description("Total number of matching blockrs before paging.")]
        [DataMember]
        public long Count { get; set; }
    }

    /// <summary>Requests changes to the specified blocks.</summary>
    [Description("Requests changes to the specified blocks.")]
    [DataContract]
    public class UpdateBlock : IGXRequest<UpdateBlockResponse>
    {
        /// <summary>Initializes a new instance of UpdateBlock.</summary>
        [Description("Initializes a new instance of UpdateBlock.")]
        public UpdateBlock()
        {
            Blocks = [];
        }

        /// <summary>
        /// Blocks to update.
        /// </summary>
        [Description("Blocks to update.")]
        [DataMember]
        [IncludeOpenApi(typeof(GXBlockGroup), nameof(GXBlockGroup.Id))]
        [ExcludeOpenApi(typeof(GXBlock),
            nameof(GXBlock.Parent),
            nameof(GXBlock.User))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [ExcludeOpenApi(typeof(GXBlock), nameof(GXBlock.CreationTime), nameof(GXBlock.Updated))]
        public List<GXBlock> Blocks { get; set; }
    }

    /// <summary>Response to updating the specified blocks.</summary>
    [Description("Response to updating the specified blocks.")]
    [DataContract]
    public class UpdateBlockResponse
    {
        /// <summary>Identifiers of the added or updated block records.</summary>
        [Description("Identifiers of the added or updated block records.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>Requests deletion of the selected blocks.</summary>
    [Description("Requests deletion of the selected blocks.")]
    [DataContract]
    public class RemoveBlock : IGXRequest<RemoveBlockResponse>
    {
        /// <summary>Identifiers of the block records to delete.</summary>
        [Description("Identifiers of the block records to delete.")]
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

    /// <summary>Response to deleting the selected blocks.</summary>
    [Description("Response to deleting the selected blocks.")]
    [DataContract]
    public class RemoveBlockResponse
    {
    }

    /// <summary>
    /// Close block.
    /// </summary>
    [Description("Close block.")]
    [DataContract]
    public class CloseBlock : IGXRequest<CloseBlockResponse>
    {
        /// <summary>Blocks identifiers to close.</summary>
        [Description("Blocks identifiers to close.")]
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Close blocks response.
    /// </summary>
    [Description("Close blocks response.")]
    [DataContract]
    public class CloseBlockResponse
    {
    }

    /// <summary>
    /// Regenerate blocks.
    /// </summary>
    [Description("Regenerate blocks.")]
    [DataContract]
    public class RegenerateBlock : IGXRequest<RegenerateBlockResponse>
    {
        /// <summary>Existing identifiers to reset to built-in defaults. Null or empty adds only missing defaults and preserves existing records.</summary>
        [Description("Existing identifiers to reset to built-in defaults. Null or empty adds only missing defaults and preserves existing records.")]
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>
    /// Regenerate block response.
    /// </summary>
    [Description("Regenerate block response.")]
    [DataContract]
    public class RegenerateBlockResponse
    {
    }
}
