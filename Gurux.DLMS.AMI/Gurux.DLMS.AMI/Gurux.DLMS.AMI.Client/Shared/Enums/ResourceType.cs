using System.Xml.Serialization;

namespace Gurux.DLMS.AMI.Client.Shared.Enums
{
    /// <summary>
    /// Resource type.
    /// </summary>
    public enum ResourceType : UInt32
    {
        /// <summary>
        /// General Image.
        /// </summary>
        [XmlEnum("0")]
        Image = 0,
        /// <summary>
        /// Icon pack image.
        /// </summary>
        [XmlEnum("1")]
        Iconpack = 1,
        /// <summary>
        /// Theme variable.
        /// </summary>
        [XmlEnum("2")]
        Theme = 2,
    }
}