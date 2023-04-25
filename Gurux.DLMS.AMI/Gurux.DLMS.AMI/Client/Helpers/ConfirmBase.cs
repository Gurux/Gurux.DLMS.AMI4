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
using Microsoft.AspNetCore.Components;

namespace Gurux.DLMS.AMI.Client.Helpers
{
    public class ConfirmArgs
    {
        public bool Confirm { get; set; }
        public bool Delete { get; set; }
    }

    public class ConfirmBase : ComponentBase
    {
        protected bool ShowConfirmation { get; set; }

        /// <summary>
        /// Title of the OK button.
        /// </summary>
        [Parameter]
        public string OkTitle { get; set; } = "Delete";


        [Parameter]
        public string ConfirmationTitle { get; set; } = "Confirm Delete";

        private string confirmationMessage = "Are you sure you want to delete";

        [Parameter]
        public string ConfirmationMessage
        {
            get
            {
                return confirmationMessage;
            }
            set
            {
                confirmationMessage = value;
            }
        }

        /// <summary>
        /// User wants to delete target from the database and not mark it as a removed.
        /// </summary>
        internal bool DeleteTarget { get; set; }

        /// <summary>
        /// Can user define is target cancel or deleted.
        /// </summary>
        [Parameter]
        public bool AllowDelete { get; set; } = true;

        public void Show(string?[]? items = null, string? msg = null)
        {
            if (!string.IsNullOrEmpty(msg))
            {
                confirmationMessage = msg;
            }
            Items = items;
            ShowConfirmation = true;
            StateHasChanged();
        }
        protected void OnValueChange(bool delete)
        {
            DeleteTarget = delete;
        }

        [Parameter]
        public EventCallback<ConfirmArgs> ConfirmationChanged { get; set; }

        public string GetConfirmationMessage()
        {
            if (string.IsNullOrEmpty(confirmationMessage))
            {
                return "";
            }
            return confirmationMessage;
        }

        /// <summary>
        /// List or removed items.
        /// </summary>
        protected string?[]? Items;

        protected async Task OnConfirmationChange(bool value)
        {
            ShowConfirmation = false;
            await ConfirmationChanged.InvokeAsync(new ConfirmArgs()
            {
                Confirm = value,
                Delete = DeleteTarget
            });
        }
    }
}
