using System.ComponentModel;

namespace Gurux.DLMS.AMI.Client.Shared;

/// <summary>Permissions for receiving and processing raw meter notifications.</summary>
[Description("User can control raw notifications.")]
public static class GXRawNotificationPolicies
{
    /// <summary>View accessible raw notifications.</summary>
    [Description("User can view raw notifications.")]
    public const string View = "raw-notification.view";

    /// <summary>Store received notification chunks.</summary>
    [Description("User can add raw notifications.")]
    public const string Add = "raw-notification.add";

    /// <summary>Reserve, renew and acknowledge notification processing.</summary>
    [Description("User can process raw notifications.")]
    public const string Edit = "raw-notification.edit";

    /// <summary>Delete selected notification connections.</summary>
    [Description("User can delete raw notifications.")]
    public const string Delete = "raw-notification.delete";

    /// <summary>Remove completed notifications by retention cutoff.</summary>
    [Description("User can clear completed raw notifications.")]
    public const string Clear = "raw-notification.clear";
}
