namespace Gurux.DLMS.AMI.Module;

/// <summary>A module is unavailable for the requested operation.</summary>
public class AmiModuleUnavailableException(string message) : InvalidOperationException(message);
