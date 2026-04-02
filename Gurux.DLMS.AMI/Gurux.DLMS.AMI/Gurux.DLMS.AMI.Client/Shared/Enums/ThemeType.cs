using System.Xml.Serialization;

namespace Gurux.DLMS.AMI.Client.Shared.Enums
{
    /// <summary>
    /// Theme type.
    /// </summary>
    public enum ThemeType : int
    {
        /// <summary>
        /// Theme color.
        /// </summary>
        [XmlEnum("0")]
        Color = 0,
        /// <summary>
        /// Theme range.
        /// </summary>
        [XmlEnum("1")]
        Range = 1
    }
}