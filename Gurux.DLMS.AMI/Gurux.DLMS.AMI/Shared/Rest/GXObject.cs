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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get object.
    /// </summary>
    public class GetObjectResponse
    {
        /// <summary>
        /// Object information.
        /// </summary>        
        [IncludeSwagger(typeof(GXUser), nameof(GXUser.Id))]
        [IncludeSwagger(typeof(GXAttribute), nameof(GXAttribute.Id), nameof(GXAttribute.Template))]
        [IncludeSwagger(typeof(GXAttributeTemplate), nameof(GXAttributeTemplate.Id), nameof(GXAttributeTemplate.Name))]
        [ExcludeSwagger(typeof(GXObject), nameof(GXObject.Tasks), nameof(GXObject.Errors))]
        public GXObject? Item
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add COSEM object.
    /// </summary>
    [DataContract]
    public class UpdateObject : IGXRequest<UpdateObjectResponse>
    {
        /// <summary>
        /// Added COSEM objects.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXObject), nameof(GXObject.Tasks), nameof(GXObject.Errors))]
        [IncludeSwagger(typeof(GXDevice), nameof(GXDevice.Id))]
        [IncludeSwagger(typeof(GXAttribute), nameof(GXAttribute.Id))]
        public GXObject[] Objects
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add COSEM object Reply.
    /// </summary>
    [DataContract]
    public class UpdateObjectResponse
    {
        /// <summary>
        /// Object identifiers.
        /// </summary>
        [DataMember]
        public Guid[] Ids
        {
            get;
            set;
        }
    }


    /// <summary>
    /// Get object.
    /// </summary>
    [DataContract]
    public class ListObjects : IGXRequest<ListObjectsResponse>
    {
        /// <summary>
        /// Start index.
        /// </summary>
        [DataMember]
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
        /// Filter can be used to filter objects.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXObject), nameof(GXObject.Tasks), nameof(GXObject.Errors), nameof(GXObject.Device), nameof(GXObject.Attributes), nameof(GXObject.Parameters))]
        public GXObject? Filter
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
    }

    /// <summary>
    /// List COSEM object response.
    /// </summary>
    [DataContract]
    public class ListObjectsResponse
    {

        /// <summary>
        /// List of COSEM objects.
        /// </summary>
        [DataMember]
        [ExcludeSwagger(typeof(GXObject), nameof(GXObject.Tasks), nameof(GXObject.Errors), nameof(GXObject.Device), nameof(GXObject.Attributes), nameof(GXObject.Parameters))]
        public GXObject[] Objects
        {
            get;
            set;
        }
        /// <summary>
        /// Total count of the objects.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete COSEM objects.
    /// </summary>
    public class RemoveObject : IGXRequest<RemoveObjectResponse>
    {
        /// <summary>
        /// Removed COSEM objects identifiers.
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
    /// Delete COSEM object response.
    /// </summary>
    [DataContract]
    public class RemoveObjectResponse
    {
    }
}
