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
[DataContract, Serializable]
public class GXViewColumn : IUnique<Guid>
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public GXViewColumn()
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <remarks>
    /// This constuctor is called when a new content type is created. It will create all needed lists.
    /// </remarks>
    /// <param name="name">Menu link name.</param>
    public GXViewColumn(string? name)
    {
        Active = true;
        Name = name;
        Visibility = 2;//Visibility.All
    }

    /// <summary>
    /// View column ID.
    /// </summary>
    [DataMember(Name = "ID")]
    [DefaultValue(null)]
    [Filter(FilterType.Exact)]
    public Guid Id { get; set; }

    /// <summary>
    /// View column name.
    /// </summary>
    [DataMember]
    [StringLength(64)]
    [Index(false)]
    [Filter(FilterType.Contains)]
    public string? Name { get; set; }

    /// <summary>
    /// Whether the column is displayed.
    /// </summary>
    [DataMember, System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.Never)]
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Is the view column active.
    /// </summary>
    [DataMember]
    [DefaultValue(true)]
    [Filter(FilterType.Exact)]
    [IsRequired]
    public bool? Active { get; set; }

    /// <summary>
    /// The column position defined by the table templates.
    /// </summary>
    [DataMember] public int Position { get; set; }

    /// <summary>
    /// View column description.
    /// </summary>
    [DataMember]
    [StringLength(128)]
    public string? Description { get; set; }

    /// <summary>The view owning this preference.</summary>
    [DataMember, IsRequired, ForeignKey(typeof(GXView), OnDelete = ForeignKeyDelete.Cascade)]
    public GXView? View { get; set; }

    /// <summary>Named visibility policy; empty means public within the surrounding resource.</summary>
    [DataMember, Ignore(IgnoreType.Db)]
    public List<string>? Policies { get; set; }

    /// <summary>The column identifier shared by filter, header and item cells.</summary>
    [DataMember, StringLength(64), DefaultValue("source")] public string SourceAlias { get; set; } = "source";
    [DataMember, StringLength(128), DefaultValue("")] public string SourceColumn { get; set; } = "";
    [DataMember, StringLength(128)] public string? Text { get; set; }

    [DataMember]
    public byte? Aggregate { get; set; }

    [DataMember, StringLength(32), DefaultValue("default")]
    public string Formatter { get; set; } = "default";

    [DataMember, StringLength(128)] public string? Format { get; set; }

    [DataMember, StringLength(128)]
    public string? EnumType { get; set; }

    [DataMember] public string? MappingJson { get; set; }

    [DataMember] public string? NullText { get; set; }

    /// <summary>
    /// Gets or sets the visibility level for the associated element.
    /// </summary>
    /// <remarks>A value of 0 typically indicates that the element is always visible, while higher
    /// values may represent different visibility states depending on the application's logic. The meaning of each
    /// value should be defined by the consuming application.</remarks>
    [DefaultValue(0)]
    [DataMember, IsRequired]
    public byte? Visibility { get; set; }

    [DataMember] public string? Icon { get; set; }
    [DataMember, StringLength(256)] public string? RowLinkPrefix { get; set; }

    [DataMember, StringLength(256)] public string? Target { get; set; }
    /// <summary>Serialized column options in database storage.</summary>

    [DataMember, System.Text.Json.Serialization.JsonIgnore] public string? ColumnOptionsJson { get; set; }
    /// <summary>Header, filter and sorting options exposed by the API.</summary>
    [DataMember, Ignore(IgnoreType.Db)]
    public GXViewColumnOptions ColumnOptions
    {
        get => string.IsNullOrEmpty(ColumnOptionsJson) ? new() : System.Text.Json.JsonSerializer.Deserialize<GXViewColumnOptions>(ColumnOptionsJson) ?? new();
        set => ColumnOptionsJson = System.Text.Json.JsonSerializer.Serialize(value ?? new());
    }

    /// <summary>
    /// Creation time.
    /// </summary>
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
    [DataMember]
    [Index(false, Descend = true)]
    [DefaultValue(null)]
    [Filter(FilterType.Null)]
    public DateTimeOffset? Removed { get; set; }

    /// <summary>
    /// When the view column is updated for the last time.
    /// </summary>
    [DataMember]
    [Filter(FilterType.GreaterOrEqual)]
    public DateTimeOffset? Updated { get; set; }
}
