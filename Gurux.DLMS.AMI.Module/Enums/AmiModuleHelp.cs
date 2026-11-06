namespace Gurux.DLMS.AMI.Module.Enums;

/// <summary>Specifies how the host presents module help.</summary>
public enum HelpKind
{
    /// <summary>A URL opened by the host.</summary>
    Link,
    /// <summary>Plain text displayed by the host.</summary>
    Text,
    /// <summary>Markdown content rendered by the host.</summary>
    Markdown
}
