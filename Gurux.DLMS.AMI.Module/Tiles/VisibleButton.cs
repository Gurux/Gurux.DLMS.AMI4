using System;

namespace Gurux.DLMS.AMI.Module.Tiles
{
    /// <summary>Buttons displayed by a module tile.</summary>
    [Flags]
    public enum VisibleButton
    {
        /// <summary>No buttons are visible.</summary>
        None = 0x0,
        /// <summary>Shows the read button.</summary>
        Read = 0x1,
        /// <summary>Shows the write button.</summary>
        Write = 0x2,
        /// <summary>Shows the add button.</summary>
        Add = 0x4,
        /// <summary>Shows the edit button.</summary>
        Edit = 0x8,
        /// <summary>Shows the remove button.</summary>
        Remove = 0x10,
        /// <summary>Shows the clear button.</summary>
        Clear = 0x20,
        /// <summary>
        /// Readers button.
        /// </summary>
        Readers = 0x40,
        /// <summary>
        /// Actions button.
        /// </summary>
        Actions = 0x80,
        /// <summary>Shows the back button.</summary>
        Back = 0x100
    }
}
