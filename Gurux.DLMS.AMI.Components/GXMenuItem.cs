using Gurux.DLMS.AMI.Components.Enums;
using Microsoft.AspNetCore.Components;

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
        public EventCallback OnClick;

        /// <summary>
        /// Is button enabled when user has modifed the content of the page.
        /// </summary>
        public EnableStyle? Enabled
        {
            get;
            set;
        } = EnableStyle.Always;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GXMenuItem()
        {
        }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="text">Menu text.</param>
        /// <param name="icon">Menu icon.</param>
        /// <param name="onClick">Menu action.</param>
        /// <param name="enabled">Is menu ebabled.</param>
        public GXMenuItem(string text, string? icon, EventCallback onClick, EnableStyle enabled = EnableStyle.Always)
        {
            Text = text;
            Icon = icon;
            OnClick = onClick;
            Enabled = enabled;
        }
    }
}
