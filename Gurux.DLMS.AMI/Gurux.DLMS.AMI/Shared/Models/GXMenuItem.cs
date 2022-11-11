namespace Gurux.DLMS.AMI.Shared.Models
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
    }
}
