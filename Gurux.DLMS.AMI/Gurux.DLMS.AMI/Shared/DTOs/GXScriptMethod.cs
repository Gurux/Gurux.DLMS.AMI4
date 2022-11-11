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
using Gurux.Common.Db;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    [DataContract(Name = "GXScriptMethod"), Serializable]
    public class GXScriptMethod : IUnique<Guid>
    {
        /// <summary>
        /// Script group ID.
        /// </summary>
        [DataMember(Name = "ID"), Index(Unique = true)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// Name of the script method.
        /// </summary>
		[DataMember]
        [StringLength(128)]
        [Filter(FilterType.Contains)]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Script method description.
        /// </summary>
        [DataMember]
        [Filter(FilterType.Contains)]
        public string? Description
        {
            get;
            set;
        }

        /// <summary>
        /// Is method a function that is returning a value.
        /// </summary>
		[DataMember]
        public bool Function
        {
            get;
            set;
        }

        /// <summary>
        /// Is method an asyncronous method.
        /// </summary>
        [DataMember]
        public bool Asyncronous
        {
            get;
            set;
        }

        /// <summary>
        /// Parent script.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXScript), OnDelete = ForeignKeyDelete.Cascade)]
        [Index(false)]
        [Filter(FilterType.Exact)]
        public GXScript? Script
        {
            get;
            set;
        }

        /// <summary>
        /// List of blocks where this script method belongs.
        /// </summary>
        [DataMember]
        [ForeignKey(typeof(GXBlock))]
        [Filter(FilterType.Contains)]
        public List<GXBlock> Blocks
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXScriptMethod()
        {
            Blocks = new List<GXBlock>(); 
        }
    }
}
