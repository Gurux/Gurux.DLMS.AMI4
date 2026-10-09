using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Shared.DTOs.Module;

/// <summary>Required identity and inclusive minimum/exclusive maximum compatibility versions.</summary>
public sealed record AmiModuleDependency(AmiModuleDependencyKind Kind, string Id, string? MinimumVersion = null, string? MaximumVersionExclusive = null);
