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

using Gurux.DLMS.AMI.Shared.Models;
using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Shared.DIs
{
    /// <summary>
    /// This interface is used to listen and notify events.
    /// </summary>
    public interface IGXNotifier
    {
        /// <summary>
        /// Menu items.
        /// </summary>
        List<GXMenuItem> Items
        {
            get;
            set;
        }

        /// <summary>
        /// Amount of the rows in the table.
        /// </summary>
        int RowsPerPage
        {
            get;
        }

        /// <summary>
        /// Last Url.
        /// </summary>
        string? LastUrl { get;}

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
        /// Clear page history.
        /// </summary>
        void ClearHistory();

        /// <summary>
        /// Update page data.
        /// </summary>
        /// <param name="data"></param>
        void UpdateData(string page, object? data);
        object? GetData(string page);

        /// <summary>
        /// Title.
        /// </summary>
        string? Title
        {
            get;
            set;
        }
        /// <summary>
        /// CRUD action.
        /// </summary>
        CrudAction Action
        {
            get;
            set;
        }

        /// <summary>
        /// Update buttons.
        /// </summary>
        public void UpdateButtons();

        /// <summary>
        /// Invoke that property has changed.
        /// </summary>
        Task ChangedAsync<T>(string methodName, T value);

        /// <summary>
        /// Listen event notifications.
        /// </summary>
        /// <typeparam name="T1">Notification parameter type.</typeparam>
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
        /// Page has changed.
        /// </summary>
        event Action OnPageChanged;
        /// <summary>
        /// Progress has started.
        /// </summary>
        event Action OnProgressStart;
        /// <summary>
        /// Progress has ended.
        /// </summary>
        event Action OnProgressEnd;
        /// <summary>
        /// Status has cleared.
        /// </summary>
        event Action OnClearStatus;
        /// <summary>
        /// Exception has occurred.
        /// </summary>
        Action<Exception> OnProcessError { get; set; }

        /// <summary>
        /// Show information.
        /// </summary>
        Action<string, bool> OnShowInformation { get; set; }
        /// <summary>
        /// Maintenance mode is updated.
        /// </summary>
        Action<bool> OnMaintenanceMode { get; set; }

        /// <summary>
        /// Stop listen notifications.
        /// </summary>
        /// <param name="listener"></param>
        void RemoveListener(object listener);
    }
}
