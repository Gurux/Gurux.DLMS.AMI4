using System.Runtime.Serialization;

namespace Gurux.DLMS.AMI.Shared.DTOs.View;

/// <summary>Persisted header, filter and sorting configuration for one view column.</summary>
[DataContract]
public sealed class GXViewColumnOptions
{
    [DataMember] public string? TextResource { get; set; }
    [DataMember] public string FilterType { get; set; } = "Auto";
    [DataMember] public string? FilterOperator { get; set; }
    [DataMember] public string? Placeholder { get; set; }
    [DataMember] public string? PlaceholderResource { get; set; }
    [DataMember] public string? EnumType { get; set; }
    [DataMember] public Dictionary<string, string> Choices { get; set; } = [];
    [DataMember, System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.Never)] public bool Sortable { get; set; } = true;
    [DataMember] public string DefaultSort { get; set; } = "None";
}
