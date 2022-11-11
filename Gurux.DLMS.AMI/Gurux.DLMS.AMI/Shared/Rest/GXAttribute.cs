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
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Add COSEM attribute.
    /// </summary>
    [DataContract]
    public class UpdateAttribute : IGXRequest<UpdateAttributeResponse>
    {
        /// <summary>
        /// Added COSEM attributes.
        /// </summary>
        [DataMember]
        public GXAttribute[] Attributes
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add COSEM attribute Reply.
    /// </summary>
    [DataContract]
    public class UpdateAttributeResponse
    {
        /// <summary>
        /// Attribute identifiers.
        /// </summary>
        [DataMember]
        public Guid[] Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get attribute.
    /// </summary>
    [DataContract]
    public class ListAttributes : IGXRequest<ListAttributesResponse>
    {
        /// <summary>
        /// Profile Generic Index. This is used only with Profile Generic attributes.
        /// </summary>
        [DataMember]
        public UInt64 Index
        {
            get;
            set;
        }

        /// <summary>
        /// Profile Generic Count. This is used only with Profile Generic attributes.
        /// </summary>
        [DataMember]
        public UInt64 Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter attributes.
        /// </summary>
        public GXAttribute? Filter
        {
            get;
            set;
        }
    }

    /// <summary>
    /// List COSEM attribute response.
    /// </summary>
    [DataContract]
    public class ListAttributesResponse
    {

        /// <summary>
        /// List of COSEM attributes.
        /// </summary>
        [DataMember]
        public GXAttribute[] Attributes
        {
            get;
            set;
        }
        /// <summary>
        /// Total count of the attributes.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete COSEM attributes.
    /// </summary>
    public class AttributeDelete : IGXRequest<AttributeDeleteResponse>
    {
        /// <summary>
        /// Removed COSEM attributes identifiers.
        /// </summary>
        [DataMember]
        public Guid[] Ids
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Delete COSEM attribute response.
    /// </summary>
    [DataContract]
    public class AttributeDeleteResponse
    {
    }

    /// <summary>
    /// Update data type of the attribute.
    /// </summary>
    [DataContract]
    public class UpdateDatatype : IGXRequest<UpdateAttributeResponse>
    {
        /// <summary>
        /// Updated attributes.
        /// </summary>
        [DataMember]
        public GXAttribute[] Items
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Update data type of the attribute reply.
    /// </summary>
    [DataContract]
    public class UpdateDatatypeResponse
    {
    }
}
