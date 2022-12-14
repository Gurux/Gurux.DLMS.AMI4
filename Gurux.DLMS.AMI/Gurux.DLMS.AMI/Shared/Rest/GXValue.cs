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
using System.ComponentModel;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Shared.Rest
{
    /// <summary>
    /// Get historical value from the DB.
    /// </summary>
    [DataContract]
    public class ListValues : IGXRequest<ListValuesResponse>
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
        /// Amount of the values to retreave.
        /// </summary>
        public int Count
        {
            get;
            set;
        }

        /// <summary>
        /// Filter can be used to filter values.
        /// </summary>
        public GXValue? Filter
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Get values reply.
    /// </summary>
    [DataContract]
    public class ListValuesResponse
    {
        /// <summary>
        /// List of value items.
        /// </summary>
        [DataMember]
        public GXValue[] Values
        {
            get;
            set;
        }

        /// <summary>
        /// Total count of the value items.
        /// </summary>
        [DataMember]
        public int Count
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Add attribute value.
    /// </summary>
    [DataContract]
    public class AddValue : IGXRequest<AddValueResponse>
    {
        /// <summary>
        /// Values to add.
        /// </summary>
        [DataMember]
        [Description("Values to add")]
        public GXValue[] Values
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Value add response.
    /// </summary>
    [Description("Value add response.")]
    [DataContract]
    public class AddValueResponse
    {
        /// <summary>
        /// New value identifiers.
        /// </summary>
        [DataMember]
        public Guid[] ValueIds
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Clear attribute, object or device value.
    /// </summary>
    [DataContract]
    public class ClearValue : IGXRequest<ClearValueResponse>
    {
        /// <summary>
        /// Devices to clear.
        /// </summary>
        [DataMember]
        public GXDevice[]? Devices
        {
            get;
            set;
        }
        /// <summary>
        /// Objects to clear.
        /// </summary>
        [DataMember]
        public GXObject[]? Objects
        {
            get;
            set;
        }
        /// <summary>
        /// Attributes to clear.
        /// </summary>
        [DataMember]
        public GXAttribute[]? Attributes
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Clear Value response.
    /// </summary>
    [DataContract]
    public class ClearValueResponse
    {

    }
}
