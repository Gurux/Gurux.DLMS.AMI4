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

namespace Gurux.DLMS.AMI.Shared.Rest
{
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
        public GXBlock[] Blocks
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
        public Guid[] BlockIds
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete blocks.
    /// </summary>
    [DataContract]
    public class DeleteBlock : IGXRequest<DeleteBlockResponse>
    {
        /// <summary>
        /// Removed block identifiers.
        /// </summary>
        [DataMember]
        public Guid[] BlockIds
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Reply from Delete block.
    /// </summary>
    [DataContract]
    public class DeleteBlockResponse
    {
    }

    /// <summary>
    /// Close block.
    /// </summary>
    [DataContract]
    public class CloseBlock : IGXRequest<CloseBlockResponse>
    {
        /// <summary>
        /// Blocks to close.
        /// </summary>
        [DataMember]
        public Guid[] Blocks
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
