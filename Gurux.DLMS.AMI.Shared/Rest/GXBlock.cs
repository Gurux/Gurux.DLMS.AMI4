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

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get block.
    /// </summary>
    public class GetBlockResponse
    {
        /// <summary>
        /// Block information.
        /// </summary>        
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

    /// <summary>
    /// Get list from blocks.
    /// </summary>
    [DataContract]
    public class ListBlocks : IGXRequest<ListBlocksResponse>
    {
        /// <summary>
        /// Zero-based index of the first item to retrieve.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Maximum number of blocks to retrieve; zero disables paging.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Optional criteria used to filter blocks.
        /// </summary>
        [ExcludeOpenApi(typeof(GXBlock),
            nameof(GXBlock.BlockGroups),
            nameof(GXBlock.User),
            nameof(GXBlock.Creator),
            nameof(GXBlock.Parent),
            nameof(GXBlock.ComponentView),
            nameof(GXBlock.ScriptMethod))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id),
            nameof(GXUser.UserName))]
        public GXBlock? Filter { get; set; }

        /// <summary>
        /// Admin user can access blocks from all users.
        /// </summary>
        /// <remarks>
        /// If true, blocks from all users are retrieved, not just current user.
        /// </remarks>
        public bool AllUsers { get; set; }

        /// <summary>
        /// Exclude blocks closed by the current user when loading displayed content.
        /// Management lists leave this false so closed blocks remain editable.
        /// </summary>
        public bool ExcludeClosed { get; set; }

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
    /// Block items reply.
    /// </summary>
    [DataContract]
    public class ListBlocksResponse
    {
        /// <summary>
        /// List of block items.
        /// </summary>
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
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// Update blocks.
    /// </summary>
    [DataContract]
    public class UpdateBlock : IGXRequest<UpdateBlockResponse>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UpdateBlock()
        {
            Blocks = new List<GXBlock>();
        }

        /// <summary>
        /// Blocks to update.
        /// </summary>
        [DataMember]
        [IncludeOpenApi(typeof(GXBlockGroup), nameof(GXBlockGroup.Id))]
        [ExcludeOpenApi(typeof(GXBlock),
            nameof(GXBlock.Parent),
            nameof(GXBlock.User))]
        [IncludeOpenApi(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeOpenApi(typeof(GXUser), nameof(GXUser.Id))]
        [IncludeOpenApi(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeOpenApi(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        public List<GXBlock> Blocks { get; set; }
    }

    /// <summary>
    /// Update blocks reply.
    /// </summary>
    [DataContract]
    public class UpdateBlockResponse
    {
        /// <summary>
        /// Block identifiers.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Delete blocks.
    /// </summary>
    [DataContract]
    public class RemoveBlock : IGXRequest<RemoveBlockResponse>
    {
        /// <summary>
        /// Block identifiers.
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
    /// Reply from Delete block.
    /// </summary>
    [DataContract]
    public class RemoveBlockResponse
    {
    }

    /// <summary>
    /// Close block.
    /// </summary>
    [DataContract]
    public class CloseBlock : IGXRequest<CloseBlockResponse>
    {
        /// <summary>
        /// Blocks IDs to close.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid> Ids { get; set; } = default!;
    }

    /// <summary>
    /// Close blocks response.
    /// </summary>
    [DataContract]
    public class CloseBlockResponse
    {
    }

    /// <summary>
    /// Regenerate blocks.
    /// </summary>
    [DataContract]
    public class RegenerateBlock : IGXRequest<RegenerateBlockResponse>
    {
        /// <summary>
        /// Block IDs to regenerate.
        /// </summary>
        [DataMember]
        public IEnumerable<Guid>? Ids { get; set; }
    }

    /// <summary>
    /// Regenerate block response.
    /// </summary>
    [DataContract]
    public class RegenerateBlockResponse
    {
    }
}
