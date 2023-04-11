﻿namespace Gurux.DLMS.AMI.Client.Helpers.Table
{
    /// <summary>
    /// Sort mode.
    /// </summary>
    public interface IGXTable
    {
        /// <summary>
        /// Sorted column.
        /// </summary>
        string? OrderBy { get; set; }
        /// <summary>
        /// Sort mode.
        /// </summary>
        SortMode SortMode { get; set; }

        /// <summary>
        /// Notify that sort has been updated.
        /// </summary>
        void NotifyShortChange();
    }
}