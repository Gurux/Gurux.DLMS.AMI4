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
using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.ComponentView
{
    /// <summary>The definition of a component that can be displayed in the user interface.</summary>
    [Description("The definition of a component that can be displayed in the user interface.")]
    public class GXComponentView : GXTableBase, IUnique<Guid>
    {
        /// <summary>Initializes a new instance of GXComponentView.</summary>
        [Description("Initializes a new instance of GXComponentView.")]
        public GXComponentView()
        {
        }

        /// <summary>Initializes a new instance of GXComponentView.</summary>
        [Description("Initializes a new instance of GXComponentView.")]
        public GXComponentView(string? name)
        {
            Name = name;
            ComponentViewGroups = [];
        }

        /// <summary>
        /// Name of the component view class.
        /// </summary>
        [Description("Name of the component view class.")]
        [DefaultValue(null)]
        [Filter(FilterType.Exact)]
        public Guid Id { get; set; }

        /// <summary>
        /// The creator of the Component view.
        /// </summary>
        [Description("The creator of the Component view.")]
        [DataMember]
        [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
        [Filter(FilterType.Exact)]
        [DefaultValue(null)]
        public GXUser? Creator { get; set; }

        /// <summary>
        /// Name of the component view class.
        /// </summary>
        [Description("Name of the component view class.")]
        [StringLength(128)]
        [IsRequired]
        public string? ClassName { get; set; }

        /// <summary>
        /// Name of the component view.
        /// </summary>
        [Description("Name of the component view.")]
        [DataMember]
        [StringLength(32)]
        [Index(false)]
        [Filter(FilterType.Contains)]
        [IsRequired]
        public string? Name { get; set; }

        /// <summary>
        /// Icon name.
        /// </summary>
        [Description("Icon name.")]
        public string? Icon { get; set; }

        /// <summary>
        /// Name of the configuration view class.
        /// </summary>
        [Description("Name of the configuration view class.")]
        [StringLength(128)]
        public string? ConfigurationUI { get; set; }

        /// <summary>Whether the component view is enabled.</summary>
        [Description("Whether the component view is enabled.")]
        [DefaultValue(true)]
        [IsRequired]
        [Filter(FilterType.Exact)]
        public bool? Enabled { get; set; }

        /// <summary>Component view groups containing this component view.</summary>
        [Description("Component view groups containing this component view.")]
        [DataMember, ForeignKey(typeof(GXComponentViewGroup),
            typeof(GXComponentViewGroupComponentView), OnDelete = ForeignKeyDelete.Cascade)]
        public List<GXComponentViewGroup>? ComponentViewGroups { get; set; }

        /// <summary>Time when the record was created.</summary>
        [DataMember]
        [Description("Time when the record was created.")]
        [DefaultValue(null)]
        [Index(false, Descend = true)]
        [Filter(FilterType.GreaterOrEqual)]
        [IsRequired]
        public DateTimeOffset? CreationTime { get; set; }

        /// <summary>
        /// Time when block group was removed.
        /// </summary>
        [Description("Time when block group was removed.")]
        [DataMember]
        [Index(false, Descend = true)]
        [DefaultValue(null)]
        [Filter(FilterType.Null)]
        public DateTimeOffset? Removed { get; set; }

        /// <summary>Time when the component view was last updated.</summary>
        [Description("Time when the component view was last updated.")]
        [Filter(FilterType.GreaterOrEqual)]
        public DateTimeOffset? Updated { get; set; }

        /// <summary>
        /// Initializes the creation timestamp before insertion if it has not been set.
        /// </summary>
        [Description("Initializes the creation timestamp before insertion if it has not been set.")]
        public override void BeforeAdd()
        {
            if (CreationTime == null)
            {
                CreationTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Updates the last-modified timestamp before persisting changes.
        /// </summary>
        [Description("Updates the last-modified timestamp before persisting changes.")]
        public override void BeforeUpdate()
        {
            Updated = DateTime.Now;
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
