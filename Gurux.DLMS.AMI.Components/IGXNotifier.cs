//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------
using Microsoft.AspNetCore.Components.Forms;

namespace Gurux.DLMS.AMI.Components
{
    /// <summary>
    /// This interface is used to indicate that browser's current location is changing.
    /// </summary>
    public interface IGXLocationChangingContext
    {
        /// <summary>
        /// Gets the target location.
        /// </summary>
        string TargetLocation { get; }

        /// <summary>
        /// Gets the state associated with the target history entry.
        /// </summary>
        string? HistoryEntryState { get; }

        /// <summary>
        /// Gets whether this navigation was intercepted from a link.
        /// </summary>
        bool IsNavigationIntercepted { get; }

        /// <summary>
        /// Gets a <see cref="System.Threading.CancellationToken"/> that can be used to determine if this navigation was canceled
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Prevents this navigation from continuing.
        /// </summary>
        void PreventNavigation();
    }

    /// <summary>
    /// This interface is used to listen and notify events.
    /// </summary>
    public interface IGXNotifier
    {
        /// <summary>
        /// Amount of the rows in the table.
        /// </summary>
        int RowsPerPage
        {
            get;
            set;
        }

        /// <summary>
        /// Last Url.
        /// </summary>
        string? LastUrl { get; }

        /// <summary>
        /// Change page.
        /// </summary>
        /// <param name="page"></param>
        void ChangePage(string page);

        /// <summary>
        /// Add new menu item.
        /// </summary>
        /// <param name="menu"></param>
        void AddMenuItem(GXMenuItem menu);

        /// <summary>
        /// Clear menu item.
        /// </summary>
        void Clear();

        /// <summary>
        /// Progress started.
        /// </summary>
        void ProgressStart();

        /// <summary>
        /// Progress ended.
        /// </summary>
        void ProgressEnd();

        /// <summary>
        /// Clear status.
        /// </summary>
        void ClearStatus();

        /// <summary>
        /// Show information.
        /// </summary>
        void ShowInformation(string info, bool closable = false);

        /// <summary>
        /// Process error.
        /// </summary>
        void ProcessError(Exception ex);

        /// <summary>
        /// Process script logs.
        /// </summary>
        public void ProcessErrors(IEnumerable<object> errors);

        /// <summary>
        /// Title.
        /// </summary>
        string? Title
        {
            get;
            set;
        }

        /// <summary>
        /// Update buttons.
        /// </summary>
        public void UpdateButtons();

        /// <summary>
        /// Listen event notifications.
        /// </summary>
        /// <param name="listener">Listener.</param>
        /// <param name="methodName">Method name.</param>
        /// <param name="handler">Handler.</param>
        /// <returns></returns>
        void On(object listener, string methodName, Action handler);

        /// <summary>
        /// Listen component notifications.
        /// </summary>
        /// <typeparam name="T1">Notification parameter type.</typeparam>
        /// <param name="listener">Listener.</param>
        /// <param name="methodName">Method name.</param>
        /// <param name="handler">Handler.</param>
        /// <remarks>
        /// Event listening is stopped by calling RemoveListener.
        /// </remarks>
        /// <seealso cref="RemoveListener"/>
        void On<T1>(object listener, string methodName, Action<T1> handler);

        /// <summary>
        /// Buttons are updated.
        /// </summary>
        event Action OnUpdateButtons;

        /// <summary>
        /// Page is changing.
        /// </summary>
        event Action<IGXLocationChangingContext>? OnPageChanging;

        /// <summary>
        /// Page has changed.
        /// </summary>
        event Action? OnPageChanged;

        /// <summary>
        /// Stop listen notifications.
        /// </summary>
        /// <param name="listener"></param>
        void RemoveListener(object listener);

        /// <summary>
        /// EditContext is used to show when user edit the page content.
        /// </summary>
        EditContext? EditContext
        {
            get;
            set;
        }

        /// <summary>
        /// The user has modified the page.
        /// </summary>
        event EventHandler<FieldChangedEventArgs>? OnDirty;
    }
}
