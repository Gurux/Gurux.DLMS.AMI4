using Gurux.DLMS.AMI.Client.Shared.Enums;

namespace Gurux.DLMS.AMI.Client.Shared
{
    /// <summary>
    /// Account settings.
    /// </summary>
    public class AccountSettings
    {
        /// <summary>
        /// User approval mode
        /// </summary>
        public UserCreationType UserCreationType { get; set; }
    }
}