namespace Gurux.DLMS.AMI.Client.Shared
{
    /// <summary>
    /// Icon pack settings.
    /// </summary>
    public class IconPack
    {
        /// <summary>
        /// Iconpack name.
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// Iconpack url.
        /// </summary>
        public string? Url { get; set; }
    }

    /// <summary>
    /// Appearance settings.
    /// </summary>
    public class AppearanceSettings
    {
        /// <summary>
        /// When the appearance settings are updated.
        /// </summary>
        public DateTimeOffset? Updated { get; set; }

        /// <summary>
        /// When the theme was updated for the last time.
        /// </summary>
        public DateTimeOffset? ThemeUpdated { get; set; }

        /// <summary>
        /// When the Iconpacks are updated for the last time.
        /// </summary>
        public DateTimeOffset? IconpackUpdated { get; set; }
    }
}