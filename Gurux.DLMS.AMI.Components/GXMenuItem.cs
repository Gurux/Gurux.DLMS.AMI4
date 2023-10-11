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
        public Action OnClick;
    }
}
