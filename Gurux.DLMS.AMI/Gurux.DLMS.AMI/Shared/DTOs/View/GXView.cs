using Gurux.DLMS.AMI.Shared.DTOs.Authentication;
using Gurux.DLMS.AMI.Shared.DTOs.Block;
using Gurux.DLMS.AMI.Shared.DTOs.Menu;
using Gurux.Service.Orm.Common;
using Gurux.Service.Orm.Common.Enums;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.View;

/// <summary>A user's saved configuration for a table view.</summary>
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
    /// This constuctor is called when a new menu is created. It will create all needed lists.
    /// </remarks>
    /// <param name="name">Menu name.</param>
    public GXView(string? name)
    {
        Active = true;
        Name = name;
        Columns = [];
        Joins = [];
    }

    /// <summary>
    /// View ID.
    /// </summary>
    [DataMember(Name = "ID")]
    [DefaultValue(null)]
    [Filter(FilterType.Exact)]
    public Guid Id { get; set; }

    /// <summary>
    /// Is the menu active.
    /// </summary>
    [DefaultValue(true)]
    [Filter(FilterType.Exact)]
    [IsRequired]
    public bool? Active { get; set; }

    /// <summary>
    /// The menu creator.
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

    /// <summary>
    /// List of view groups where this menu belongs.
    /// </summary>
    [DataMember, ForeignKey(typeof(GXViewGroup), typeof(GXViewGroupView))]
    [Filter(FilterType.Contains)]
    public List<GXViewGroup>? ViewGroups { get; set; }

    /// <summary>
    /// MIKKO
    /// </summary>
    [DataMember, StringLength(128), DefaultValue(null)]
    public string? SourceTable { get; set; }

    /// <summary>
    /// How the view is selected in the UI.
    /// </summary>
    /// <remarks>
    /// Possible values: None, Single, Multiple. This property is nullable to allow for a default behavior if not set.
    /// </remarks>
    public byte? SelectionMode { get; set; }

    /// <summary>
    /// Icon for the view, if any. This is a string that can be used to reference an icon in the UI.
    /// </summary>
    [DataMember] public string? Icon { get; set; }
    [DataMember, StringLength(256)] public string? Url { get; set; }

    /// <summary>
    /// Menu associated with this view. This is a GXMenu object that defines the actions available in the view's context.
    /// </summary>
    [DataMember]
    public GXMenu? Menu { get; set; }

    /// <summary>
    /// Context menu associated with this view. This is a GXMenu object that defines the actions available in the view's context.
    /// </summary>
    [DataMember]
    public GXMenu? ContextMenu { get; set; }

    /// <summary>
    /// Mikko
    /// </summary>
    [DataMember]
    public byte? RowMode { get; set; }

    [DataMember]
    public bool? ShowAllData { get; set; }

    [DataMember]
    public int? PageSize { get; set; }

    [DataMember]
    public GXBlock? Header { get; set; }

    [DataMember]
    public GXBlock? Footer { get; set; }

    /// <summary>
    /// Creation time.
    /// </summary>
    [DataMember]
    [Index(false, Descend = true)]
    [Filter(FilterType.GreaterOrEqual)]
    [IsRequired]
    public DateTimeOffset? CreationTime { get; set; }

    /// <summary>
    /// Time when menu was removed.
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
    /// When the menu is updated for the last time.
    /// </summary>
    [Filter(FilterType.GreaterOrEqual)]
    public DateTimeOffset? Updated { get; set; }

    /// <summary>
    /// Concurrency stamp.
    /// </summary>
    /// <remarks>
    /// Concurrency stamp is used to verify that several user's can't 
    /// modify the target at the same time.
    /// </remarks>
    [DataMember]
    [StringLength(36)]
    [ConcurrencyCheck]
    public string? ConcurrencyStamp { get; set; }

    /// <summary>Column preferences stored separately in GXViewColumn.</summary>
    [DataMember, ForeignKey(typeof(GXViewColumn))]
    public List<GXViewColumn>? Columns { get; set; }

    [DataMember, ForeignKey(typeof(GXViewJoin))]
    public List<GXViewJoin>? Joins { get; set; }

    /// <summary>
    /// Load this resource during client initialization when access is allowed.
    /// </summary>
    [DataMember, IsRequired, DefaultValue(false), Filter(FilterType.Exact, false)]
    public bool? Preload { get; set; }

    public override string ToString()
    {
        return $"{Name} ({Creator})";
    }
}
