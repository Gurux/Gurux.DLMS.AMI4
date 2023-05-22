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
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get object template.
    /// </summary>
    public class GetObjectTemplateResponse
    {
        /// <summary>
        /// Object information.
        /// </summary>        
        public GXObjectTemplate? Item
        {
            get;
            set;
        }
    }


    /// <summary>
    /// Add COSEM object template.
    /// </summary>
    [DataContract]
    public class UpdateObjectTemplate : IGXRequest<UpdateObjectTemplateResponse>
    {
        /// <summary>
        /// Added COSEM object templates.
        /// </summary>
        [DataMember]
        public GXObjectTemplate[] ObjectTemplates
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add COSEM object template Reply.
    /// </summary>
    [DataContract]
    public class UpdateObjectTemplateResponse
    {
        /// <summary>
        /// Object template identifiers.
        /// </summary>
        [DataMember]
        public Guid[] Ids
        {
            get;
            set;
        }
    }


    /// <summary>
    /// Get object templates.
    /// </summary>
    [DataContract]
    public class ListObjectTemplates : IGXRequest<ListObjectTemplatesResponse>
    {
        /// <summary>
        /// Start index.
        /// </summary>
        public UInt64 Index
        {
            get;
            set;
        }

        /// <summary>
        /// Amount of the modules to retreave.
        /// </summary>
        [DataMember]
        public UInt64 Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter object templates.
        /// </summary>
        public GXObjectTemplate? Filter
        {
            get;
            set;
        }

        /// <summary>
        /// Admin user can access agents from all users.
        /// </summary>
        /// <remarks>
        /// If true, agents from all users are retreaved, not just current user. 
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
        public TargetType Select
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
    /// List COSEM object template response.
    /// </summary>
    [DataContract]
    public class ListObjectTemplatesResponse
    {

        /// <summary>
        /// List of COSEM object templates.
        /// </summary>
        [DataMember]
        public GXObjectTemplate[] ObjectTemplates
        {
            get;
            set;
        }
        /// <summary>
        /// Total count of the object templates.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete COSEM object templates.
    /// </summary>
    public class RemoveObjectTemplate : IGXRequest<RemoveObjectTemplateResponse>
    {
        /// <summary>
        /// Removed COSEM object templates identifiers.
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
    /// Delete COSEM object template response.
    /// </summary>
    [DataContract]
    public class RemoveObjectTemplateResponse
    {
    }
}
