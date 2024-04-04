using Gurux.DLMS.AMI.Components.Enums;

namespace Gurux.DLMS.AMI.Components
{
    /// <summary>
    /// Top menu item.
    /// </summary>
    public class GXMenuItem
    {
        /// <summary>
        /// Menu text.
        /// </summary>
        public string Text { get; set; } = "";

        /// <summary>
        /// Menu icon.
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// User clients the menu item.
        /// </summary>
        public Action? OnClick;

        /// <summary>
        /// Is button enabled when user has modifed the content of the page.
        /// </summary>
        public EnableStyle? Enabled
        {
            get;
            set;
        } = EnableStyle.Always;
    }
}
