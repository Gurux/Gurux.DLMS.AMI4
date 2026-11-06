using Gurux.DLMS.AMI.Module.Enums;

namespace Gurux.DLMS.AMI.Module;

/// <summary>Describes a module's help without coupling the module to UI components.</summary>
/// <param name="Kind">The format of the help value.</param>
/// <param name="Value">A URL for Link, or the actual content for Text and Markdown.</param>
public sealed record AmiModuleHelp(HelpKind Kind, string Value);
