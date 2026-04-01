namespace Gurux.DLMS.AMI.Components.Enums
{
    /// <summary>
    /// Used image type.
    /// </summary>
    public enum ImageType : byte
    {
        /// <summary>
        /// From file extension.
        /// </summary>
        Auto,
        /// <summary>
        /// Png/jpg/gif/webp/ico format.
        /// </summary>
        Raster,
        /// <summary>
        /// Svg format
        /// </summary>
        Svg,
        /// <summary>
        /// Icon name e.g. from material Icons.
        /// </summary>
        IconLigature,
        /// <summary>
        /// Html icon.
        /// </summary>
        Html
    }
}