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
using Gurux.DLMS.AMI.Shared.DTOs.User;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.ComponentView
{
    /// <summary>
    /// List of components that can be shown in the UI.
    /// </summary>
    public class GXComponentView : IUnique<Guid>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXComponentView()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXComponentView(string? name)
        {
            Name = name;
            ComponentViewGroups = new List<GXComponentViewGroup>();
        }

        /// <summary>
        /// Name of the component view class.
        /// </summary>
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// The creator of the Component view.
        /// </summary>
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.None)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXUser? Creator
        {
            get;
            set;
        }

        /// <summary>
        /// Name of the component view class.
        /// </summary>
        [StringLength(128)]
        [IsRequired]
        public string? ClassName
        {
            get;
            set;
        }

        /// <summary>
        /// Name of the component view.
        /// </summary>
        [DataMember]
        [StringLength(32)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name
        {
            get;
            set;
        }

        /// <summary>
        /// Icon name.
        /// </summary>
        [Description("Icon name.")]
        public string? Icon
        {
            get;
            set;
        }

        /// <summary>
        /// Name of the configuration view class.
        /// </summary>
        [StringLength(128)]
        public string? ConfigurationUI
        {
            get;
            set;
        }

        /// <summary>
        /// Is component view active.
        /// </summary>
        [DefaultValue(true)]
        [IsRequired]
        [Filter(FilterType.Exact)]
        public bool? Active { get; set; }

        /// <summary>
        /// List of component view groups where this component view belongs.
        /// </summary>
        [DataMember, ForeignKey(typeof(GXComponentViewGroup), typeof(GXUserGroupComponentViewGroup))]
        public List<GXComponentViewGroup>? ComponentViewGroups
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
        /// Time when block group was removed.
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
        /// When the block is updated for the last time.
        /// </summary>
        [Description("When the block is updated for the last time.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated
        {
            get;
            set;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string str;
            if (!string.IsNullOrEmpty(Name))
            {
                str = Name;
            }
            else
            {
                str = nameof(GXComponentView);
            }
            return str;
        }
    }
}
