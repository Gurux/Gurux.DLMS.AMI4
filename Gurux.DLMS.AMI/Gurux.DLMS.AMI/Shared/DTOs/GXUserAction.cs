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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace Gurux.DLMS.AMI.Shared.DTOs
{
    /// <summary>
    /// User log is used to save user actions to the DB.
    /// </summary>
    [Description("User Log.")]
    public class GXUserAction : GXTableBase, IUnique<Guid>
    {
        /// <summary>
        /// Action identifier.
        /// </summary>
        [DataMember]
        [Description("Action identifier.")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// User.
        /// </summary>
        [DataMember(Name = "UserID")]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Description("User.")]
        [DefaultValue(null)]
        [Index(false)]
        [Filter(FilterType.Exact)]
        public GXUser? User
        {
            get;
            set;
        }

        /// <summary>
        /// Creation time.
        /// </summary>
        [DataMember]
        [Description("Creation time.")]
        [DefaultValue(null)]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTime CreationTime
        {
            get;
            set;
        }

        /// <summary>
        /// Target.
        /// </summary>
        [DataMember]
        [Description("Target")]
        [DefaultValue(TargetType.None)]
        public TargetType Target
        {
            get;
            set;
        }

        /// <summary>
        /// Crud Action.
        /// </summary>
        [DataMember]
        [Description("Action")]
        [DefaultValue(CrudAction.None)]
        public CrudAction Action
        {
            get;
            set;
        }

        /// <summary>
        /// Status code.
        /// </summary>
        [DataMember]
        [Description("Status code")]
        [DefaultValue(0)]

        public int Status
        {
            get;
            set;
        }

        /// <summary>
        /// User action data.
        /// </summary>
        [DataMember]
        [Description("User data.")]
        [DefaultValue(null)]

        public string? Data
        {
            get;
            set;
        }

        /// <summary>
        /// User action reply data.
        /// </summary>
        [DataMember]
        [Description("Reply data.")]
        [DefaultValue(null)]
        public string? Reply
        {
            get;
            set;
        }

        /// <summary>
        /// Update creation time before update.
        /// </summary>
        public override void BeforeAdd()
        {
            if (CreationTime == DateTime.MinValue)
            {
                CreationTime = DateTime.Now;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (User != null && !string.IsNullOrEmpty(User.UserName))
            {
                return User.UserName;
            }
            return nameof(GXUserAction);
        }
    }
}
