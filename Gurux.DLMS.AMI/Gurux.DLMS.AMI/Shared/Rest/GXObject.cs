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
    /// Add COSEM object.
    /// </summary>
    [DataContract]
    public class UpdateObject : IGXRequest<UpdateObjectResponse>
    {
        /// <summary>
        /// Added COSEM objects.
        /// </summary>
        [DataMember]
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
        /// Profile Generic Index. This is used only with Profile Generic objects.
        /// </summary>
        [DataMember]
        public UInt64 Index
        {
            get;
            set;
        }

        /// <summary>
        /// Profile Generic Count. This is used only with Profile Generic objects.
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
        public GXObject? Filter
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
    public class ObjectDelete : IGXRequest<ObjectDeleteResponse>
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
    }

    /// <summary>
    /// Delete COSEM object response.
    /// </summary>
    [DataContract]
    public class ObjectDeleteResponse
    {
    }
}
