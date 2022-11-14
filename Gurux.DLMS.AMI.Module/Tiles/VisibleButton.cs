using System;

namespace Gurux.DLMS.AMI.Module.Tiles
{
    [Flags]
    public enum VisibleButton
    {
        None = 0x0,
        Read = 0x1,
        Write = 0x2,
        Add = 0x4,
        Edit = 0x8,
        Remove = 0x10,
        Clear = 0x20,
        /// <summary>
        /// Readers button.
        /// </summary>
        Readers = 0x40,
        /// <summary>
        /// Actions button.
        /// </summary>
        Actions = 0x80,
        Back = 0x100
    }
}