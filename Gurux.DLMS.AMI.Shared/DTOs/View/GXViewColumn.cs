// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.View;

/// <summary>
/// A column's visibility and position in a saved table view.
/// </summary>
[Description("A column's visibility and position in a saved table view.")]
[DataContract, Serializable]
public class GXViewColumn : IUnique<Guid>
{
    /// <summary>Initializes a new instance of GXViewColumn.</summary>
    [Description("Initializes a new instance of GXViewColumn.")]
    public GXViewColumn()
    {
    }

    /// <summary>Initializes a new instance of GXViewColumn.</summary>
    /// <remarks>
    /// Creates a visible column with no responsive visibility restriction.
    /// </remarks>
    /// <param name="name">The identifier matching the table's header, filter and item cells.</param>
    [Description("Initializes a new instance of GXViewColumn.")]
    public GXViewColumn(string? name)
    {
        Enabled = true;
        Name = name;
        Visibility = 0; // Visibility.All
    }

    /// <summary>View column identifier.</summary>
    [Description("View column identifier.")]
    [DataMember(Name = "ID")]
    [DefaultValue(null)]
    [Filter(FilterType.Exact)]
    public Guid Id { get; set; }

    /// <summary>
    /// View column name.
    /// </summary>
    [Description("View column name.")]
    [DataMember]
    [StringLength(64)]
    [Index(false)]
    [Filter(FilterType.Contains)]
    public string? Name { get; set; }

    /// <summary>
    /// Whether the column is displayed.
    /// </summary>
    [Description("Whether the column is displayed.")]
    [DataMember, System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.Never)]
    public bool Visible { get; set; } = true;

    /// <summary>Whether the view column is enabled.</summary>
    [Description("Whether the view column is enabled.")]
    [DataMember]
    [DefaultValue(true)]
    [Filter(FilterType.Exact)]
    [IsRequired]
    public bool? Enabled { get; set; }

    /// <summary>
    /// The default column order.
    /// </summary>
    [Description("The default column order.")]
    [DataMember] public int Position { get; set; }

    /// <summary>
    /// View column description.
    /// </summary>
    [Description("View column description.")]
    [DataMember]
    [StringLength(128)]
    public string? Description { get; set; }

    /// <summary>The view owning this preference.</summary>
    [Description("The view owning this preference.")]
    [DataMember, IsRequired, ForeignKey(typeof(GXView), OnDelete = ForeignKeyDelete.Cascade)]
    public GXView? View { get; set; }

    /// <summary>Named visibility policy; empty means public within the surrounding resource.</summary>
    [Description("Named visibility policy; empty means public within the surrounding resource.")]
    [DataMember, Ignore(IgnoreType.Db)]
    public List<string>? Policies { get; set; }

    /// <summary>
    /// Gets or sets the visibility level for the associated element.
    /// </summary>
    /// <remarks>Matches the GXTable responsive visibility values; zero displays the column at every screen size.</remarks>
    [Description("Gets or sets the visibility level for the associated element.")]
    [DefaultValue(0)]
    [DataMember, IsRequired]
    public byte? Visibility { get; set; }

    /// <summary>Time when the record was created.</summary>
    [Description("Time when the record was created.")]
    [DataMember]
    [Index(false, Descend = true)]
    [Filter(FilterType.GreaterOrEqual)]
    [IsRequired]
    public DateTimeOffset? CreationTime { get; set; }

    /// <summary>
    /// Time when view column was removed.
    /// </summary>
    /// <remarks>
    /// In filter if the removed time is set it will return values that are not null.
    /// </remarks>
    [Description("Time when view column was removed.")]
    [DataMember]
    [Index(false, Descend = true)]
    [DefaultValue(null)]
    [Filter(FilterType.Null)]
    public DateTimeOffset? Removed { get; set; }

    /// <summary>Time when the view column was last updated.</summary>
    [Description("Time when the view column was last updated.")]
    [DataMember]
    [Filter(FilterType.GreaterOrEqual)]
    public DateTimeOffset? Updated { get; set; }
}
