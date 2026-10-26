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
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.View;

/// <summary>Shared default settings for a table view.</summary>
[DataContract, Serializable, IndexCollection(true, nameof(Creator.Id), nameof(Name))]
public class GXView : IUnique<Guid>
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public GXView()
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <remarks>
    /// This constuctor is called when a new view is created. It will create all needed lists.
    /// </remarks>
    /// <param name="name">View name.</param>
    public GXView(string? name)
    {
        Enabled = true;
        Name = name;
        Columns = [];
    }

    /// <summary>
    /// View ID.
    /// </summary>
    [DataMember(Name = "ID")]
    [DefaultValue(null)]
    [Filter(FilterType.Exact)]
    public Guid Id { get; set; }

    /// <summary>
    /// Is the view enabled.
    /// </summary>
    [DefaultValue(true)]
    [Filter(FilterType.Exact)]
    [IsRequired]
    public bool? Enabled { get; set; }

    /// <summary>
    /// The view creator.
    /// </summary>
    [DataMember]
    [ForeignKey(OnDelete = ForeignKeyDelete.Cascade)]
    [Filter(FilterType.Exact)]
    [DefaultValue(null)]
    public GXUser? Creator { get; set; }

    /// <summary>
    /// The view name.
    /// </summary>
    [DataMember, IsRequired, StringLength(64)]
    public string? Name { get; set; }

    /// <summary>
    /// View title. This is the display name of the view, which can be different from the internal name.
    /// </summary>
    [DataMember, StringLength(128)]
    public string? Title { get; set; }

    /// <summary>
    /// View description. This is a longer text that can be used to describe the view in more detail.
    /// </summary>
    [DataMember]
    public string? Description { get; set; }

    /// <summary>
    /// Named visibility policy; empty means public within the surrounding resource.
    /// </summary>
    [DataMember, Ignore(IgnoreType.Db)]
    public List<string>? Policies { get; set; }

    /// <summary>View groups containing this table view.</summary>
    [DataMember, ForeignKey(typeof(GXViewGroup), typeof(GXViewGroupView))]
    [Filter(FilterType.Contains)]
    public List<GXViewGroup>? ViewGroups { get; set; }

    /// <summary>
    /// How the view is selected in the UI.
    /// </summary>
    /// <remarks>
    /// Possible values: None, Single, Multiple. This property is nullable to allow for a default behavior if not set.
    /// </remarks>
    [DataMember]
    public byte? SelectionMode { get; set; }

    /// <summary>Whether users can customize the table columns. If unspecified, only administrators can customize them.</summary>
    [DataMember, DefaultValue(null)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.Never)]
    public bool? AllowColumnCustomization { get; set; }

    /// <summary>Whether all matching rows are shown without paging.</summary>
    [DataMember]
    public bool? ShowAllData { get; set; }

    /// <summary>Default number of rows on each page.</summary>
    [DataMember]
    public int? PageSize { get; set; }

    /// <summary>
    /// Creation time.
    /// </summary>
    [DataMember]
    [Index(false, Descend = true)]
    [Filter(FilterType.GreaterOrEqual)]
    [IsRequired]
    public DateTimeOffset? CreationTime { get; set; }

    /// <summary>
    /// Time when view was removed.
    /// </summary>
    /// <remarks>
    /// In filter if the removed time is set it will return values that are not null.
    /// </remarks>
    [DataMember]
    [Index(false, Descend = true)]
    [DefaultValue(null)]
    [Filter(FilterType.Null)]
    public DateTimeOffset? Removed { get; set; }

    /// <summary>
    /// When the view is updated for the last time.
    /// </summary>
    [Filter(FilterType.GreaterOrEqual)]
    public DateTimeOffset? Updated { get; set; }

    /// <summary>Version token used by the database to detect conflicting updates.</summary>
    /// <remarks>
    /// A stale token causes a conflicting update to be rejected instead of overwriting newer changes.
    /// </remarks>
    [DataMember]
    [StringLength(36)]
    [ConcurrencyCheck]
    public string? ConcurrencyStamp { get; set; }

    /// <summary>Column preferences stored separately in GXViewColumn.</summary>
    [DataMember, ForeignKey(typeof(GXViewColumn))]
    public List<GXViewColumn>? Columns { get; set; }

    /// <summary>
    /// Load this resource during client initialization when access is allowed.
    /// </summary>
    [DataMember, IsRequired, DefaultValue(false), Filter(FilterType.Exact, false)]
    public bool? Preload { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Name} ({Creator})";
    }
}
