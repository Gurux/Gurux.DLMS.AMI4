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
using Gurux.DLMS.AMI.Components;
using Gurux.DLMS.AMI.Shared.Enums;

namespace Gurux.DLMS.AMI.Client
{
    /// <summary>
    /// This interface is used to listen and notify events.
    /// </summary>
    public interface IGXNotifier2 : IGXNotifier
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
        /// Clear page history.
        /// </summary>
        void ClearHistory();

        /// <summary>
        /// Update page data.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="data"></param>
        void UpdateData(string page, object? data);
   
        /// <summary>
        /// Get page data.
        /// </summary>
        /// <param name="page">Page</param>
        /// <returns>Page data.</returns>
        object? GetData(string page);
       
        /// <summary>
        /// CRUD action.
        /// </summary>
        CrudAction Action
        {
            get;
            set;
        }

        /// <summary>
        /// Invoke that property has changed.
        /// </summary>
        Task ChangedAsync<T>(string methodName, T value);

        /// <summary>
        /// Progress has started.
        /// </summary>
        event Action? OnProgressStart;
        /// <summary>
        /// Progress has ended.
        /// </summary>
        event Action? OnProgressEnd;

        /// <summary>
        /// Status has cleared.
        /// </summary>
        event Action? OnClearStatus;

        /// <summary>
        /// Exception has occurred.
        /// </summary>
        event Action<Exception>? OnProcessError;

        /// <summary>
        /// Show information.
        /// </summary>
        event Action<string, bool>? OnShowInformation;

        /// <summary>
        /// Maintenance mode is updated.
        /// </summary>
        event Action<bool>? OnMaintenanceMode;
    }
}
