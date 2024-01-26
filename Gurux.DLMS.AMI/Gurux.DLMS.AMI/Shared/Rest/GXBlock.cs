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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.Script;
using Gurux.DLMS.AMI.Shared.DTOs.ComponentView;
using Gurux.DLMS.AMI.Shared.DTOs.User;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get block.
    /// </summary>
    public class GetBlockResponse
    {
        /// <summary>
        /// Device information.
        /// </summary>        
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeSwagger(typeof(GXBlockGroup), nameof(GXBlockGroup.Id), nameof(GXBlockGroup.Name))]
        [IncludeSwagger(typeof(GXLanguage), nameof(GXLanguage.Id), nameof(GXLanguage.Resources))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeSwagger(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeSwagger(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        [IncludeSwagger(typeof(GXBlockGroup), nameof(GXBlockGroup.Id))]        
        [ExcludeSwagger(typeof(GXLocalizedResource), nameof(GXLocalizedResource.Language),
            nameof(GXLocalizedResource.Module), nameof(GXLocalizedResource.Block),
            nameof(GXLocalizedResource.Script))]
        [ExcludeSwagger(typeof(GXBlock), 
            nameof(GXBlock.Resources),
            nameof(GXBlock.Parent))]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public GXBlock Item
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get list from blocks.
    /// </summary>
    [DataContract]
    public class ListBlocks : IGXRequest<ListBlocksResponse>
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
        /// Amount of the blocks to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter blocks.
        /// </summary>
        [ExcludeSwagger(typeof(GXBlock), 
            nameof(GXBlock.BlockGroups),
            nameof(GXBlock.User),
            nameof(GXBlock.Resources), 
            nameof(GXBlock.Languages),
            nameof(GXBlock.Parent),
            nameof(GXBlock.ComponentView), 
            nameof(GXBlock.ScriptMethod))]
        public GXBlock? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access blocks from all users.
        /// </summary>
        /// <remarks>
        /// If true, blocks from all users are retreaved, not just current user. 
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
    /// Block items reply.
    /// </summary>
    [DataContract]
    public class ListBlocksResponse
    {
        /// <summary>
        /// List of block items.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXBlock), nameof(GXBlock.BlockGroups),
            nameof(GXBlock.User), 
            nameof(GXBlock.Resources),
            nameof(GXBlock.Parent), 
            nameof(GXBlock.Languages))]
        [IncludeSwagger(typeof(GXBlockGroup), nameof(GXBlockGroup.Id), nameof(GXBlockGroup.Name))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id), nameof(GXUser.UserName))]
        [IncludeSwagger(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeSwagger(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]        
        public GXBlock[]? Blocks
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the blockrs.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
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
        [IncludeSwagger(typeof(GXBlockGroup), nameof(GXBlockGroup.Id))]
        [ExcludeSwagger(typeof(GXBlock), 
            nameof(GXBlock.Resources),
            nameof(GXBlock.Parent), 
            nameof(GXBlock.Languages), 
            nameof(GXBlock.User))]
        [IncludeSwagger(typeof(GXUserGroup), nameof(GXUserGroup.Id))]
        [IncludeSwagger(typeof(GXComponentView), nameof(GXComponentView.Id))]
        [IncludeSwagger(typeof(GXScriptMethod), nameof(GXScriptMethod.Id))]
        public List<GXBlock> Blocks
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update blocks reply.
    /// </summary>
    [DataContract]
    public class UpdateBlockResponse
    {
        /// <summary>
        /// New block identifiers.
        /// </summary>
        [DataMember]
        public Guid[]? Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete blocks.
    /// </summary>
    [DataContract]
    public class RemoveBlock : IGXRequest<RemoveBlockResponse>
    {
        /// <summary>
        /// Removed block identifiers.
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
        public Guid[] Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Close blocks response.
    /// </summary>
    [DataContract]
    public class CloseBlockResponse
    {
    }
}
