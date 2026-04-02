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

namespace Gurux.DLMS.AMI.Client.Helpers.Toaster
{
    /// <summary>
    /// This interface is used to handle toaster services.
    /// </summary>
    public interface IGXToasterService
    {
        /// <summary>
        /// Maximum toast count.
        /// </summary>
        int MaxCount
        {
            get;
            set;
        }

        /// <summary>
        /// Add new toast.
        /// </summary>
        /// <param name="toast"></param>
        void Add(GXToast toast);

        /// <summary>
        /// Are the any toasts.
        /// </summary>
        bool Any { get; }

        /// <summary>
        /// Get toasts.
        /// </summary>
        /// <returns></returns>
        List<GXToast> GetToasts();

        /// <summary>
        /// Remove toasts.
        /// </summary>
        /// <param name="toast">Toast to remove.</param>
        public void Remove(GXToast toast);

        /// <summary>
        /// Notification that toaster has changed.
        /// </summary>
        event EventHandler? ToasterChanged;
        /// <summary>
        /// Notification that toaster time has elapsed and it's removed.
        /// </summary>
        event EventHandler? ToasterTimerElapsed;
    }
}