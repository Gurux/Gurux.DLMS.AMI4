using Microsoft.AspNetCore.Components;

namespace Gurux.DLMS.AMI.Components.Table
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

        /// <summary>
        /// User has selected the row.
        /// </summary>
        /// <param name="selected">Selected row.</param>
        void SelectRow(object selected);

        /// <summary>
        /// User has selected the cell.
        /// </summary>
        /// <param name="selected">Selected cell.</param>
        void SelectCell(object selected);

        /// <summary>
        /// Is edit allowed.
        /// </summary>
        bool CanEdit { get;}

        /// <summary>
        /// Is column hidden.
        /// </summary>
        /// <param name="name">Column name.</param>
        /// <returns>True, if column is hidden.</returns>
        bool IsHidden(string? name);
    }
}