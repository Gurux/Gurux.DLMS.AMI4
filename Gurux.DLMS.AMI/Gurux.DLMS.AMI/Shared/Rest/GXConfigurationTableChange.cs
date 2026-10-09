namespace Gurux.DLMS.AMI.Shared.Rest;

/// <summary>Describes the pending schema changes for an application table.</summary>
public sealed class GXConfigurationTableChange
{
    /// <summary>Gets or sets the name of the table with pending schema changes.</summary>
    public string TableName { get; set; } = string.Empty;
    /// <summary>Gets or sets the descriptions of the pending schema changes.</summary>
    public string[] Changes { get; set; } = [];
}
