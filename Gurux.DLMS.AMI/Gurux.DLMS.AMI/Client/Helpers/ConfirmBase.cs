using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.Objects;
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

        [Parameter]
        public string ConfirmationMessage { get; set; } = "Are you sure you want to delete";

        public bool DeleteTarget { get; set; }

        /// <summary>
        /// Can user define is target cancel or deleted.
        /// </summary>
        [Parameter]
        public bool AllowDelete { get; set; } = true;

        public void Show(string?[]? items = null, string? msg = null)
        {
            if (!string.IsNullOrEmpty(msg))
            {
                ConfirmationMessage = msg;
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
