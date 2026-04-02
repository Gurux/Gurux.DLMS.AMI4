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
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.ContentType
{
    /// <summary>
    /// A data contract class representing content group to content binding object.
    /// </summary>
    [DataContract(Name = "GXContentTypeGroupContentType"), Serializable]
    [IndexCollection(true, nameof(ContentTypeGroupId), nameof(ContentTypeId), Clustered = true)]
    public class GXContentTypeGroupContentType : GXTableBase
    {
        /// <summary>
        /// The database ID of the content group.
        /// </summary>
        [DataMember(Name = "ContentTypeGroupId")]
        [ForeignKey(typeof(GXContentTypeGroup), OnDelete = ForeignKeyDelete.None)]
        [IsRequired]
        public Guid ContentTypeGroupId
        {
            //ForeignKeyDelete is None because creator of the content type is causing multiple cascade paths error in MSSQL.
            get;
            set;
        }

        /// <summary>
        /// The content ID.
        /// </summary>
        [DataMember(Name = "ContentTypeId")]
        [ForeignKey(typeof(GXContentType), OnDelete = ForeignKeyDelete.Cascade)]
        [IsRequired]
        public Guid ContentTypeId
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// The time when the content was added to the content group.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Time when block was removed from content group.
        /// </summary>
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed
        {
            get;
            set;
        }

        /// <summary>
        /// Update creation time before update.
        /// </summary>
        public override void BeforeAdd()
        {
            if (CreationTime == null)
            {
                CreationTime = DateTime.Now;
            }
        }
    }
}
