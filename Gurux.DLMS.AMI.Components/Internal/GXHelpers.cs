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
using Microsoft.AspNetCore.Components;

namespace Gurux.DLMS.AMI.Client.Helpers
{
    internal static class GXHelpers
    {
        public static string GetParentUrl(string url)
        {
            return url.Replace("/Edit/", "/").Replace("/Add/", "/").Replace("/Add", "");
        }

        /// <summary>
        /// Navigate to the given url and save the current url.
        /// </summary>
        /// <param name="navigationManager">Navigation manager.</param>
        /// <param name="notifier">Notifier.</param>
        /// <param name="url">Url.</param>
        public static void NavigateTo(this NavigationManager navigationManager, IGXNotifier? notifier, string url)
        {
            notifier?.ChangePage(navigationManager.Uri);
            navigationManager.NavigateTo(url);
        }

        /// <summary>
        /// Navigate to the previous url.
        /// </summary>
        /// <param name="navigationManager">Navigation manager.</param>
        /// <param name="notifier">Notifier.</param>
        public static void NavigateToLastPage(this NavigationManager navigationManager, IGXNotifier notifier)
        {
            if (notifier.LastUrl != null)
            {
                navigationManager.NavigateTo(notifier.LastUrl);
            }
            else
            {
                navigationManager.NavigateTo(GetParentUrl(navigationManager.Uri));
            }
        }
    }
}
