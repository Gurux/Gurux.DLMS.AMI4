using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Server.Areas.Identity.Pages.Account.Manage
{
    public partial class PatCreate : PageModel
    {
        private readonly IPatRepository _patRepository;

        public PatCreate(
            IPatRepository patRepository)
        {
            _patRepository = patRepository;
        }

        [BindProperty]
        public string TokenName { get; set; }

        /// <summary>
        /// Token expiration date.
        /// </summary>
        [DataType(DataType.DateTime)]
        [BindProperty]
        public DateTimeOffset? TokenExpiration { get; set; }

        [BindProperty]
        public bool FullAccess { get; set; }

        [TempData]
        public string PersonalTokenName { get; set; }

        [TempData]
        public string PersonalUserToken { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        private void Localize()
        {
            ViewData["Succeeded"] = Properties.Resources.Succeeded;
            ViewData["TokenCreateSucceeded"] = Properties.Resources.TokenCreateSucceeded;
            ViewData["TokenCreateInfo"] = Properties.Resources.TokenCreateInfo;
            ViewData["Copy"] = Properties.Resources.Copy;
            ViewData["Clear"] = Properties.Resources.Clear;
            ViewData["Close"] = Properties.Resources.Close;
            ViewData["View"] = Properties.Resources.View;
            ViewData["Add"] = Properties.Resources.Add;
            ViewData["Edit"] = Properties.Resources.Edit;
            ViewData["Delete"] = Properties.Resources.Delete;
            ViewData["Name"] = Properties.Resources.Name;
            ViewData["Create"] = Properties.Resources.Create;
            ViewData["Cancel"] = Properties.Resources.Cancel;
            ViewData["CreationTime"] = Properties.Resources.CreationTime;
            ViewData["Expiration"] = Properties.Resources.Expiration;
            ViewData["Cron"] = Properties.Resources.Cron;
        }


        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                Localize();
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string selectedScopes)
        {
            if (string.IsNullOrEmpty(TokenName))
            {
                StatusMessage = "Invalid token name.";
            }
            else
            {
                try
                {
                    GXPersonalToken token = new GXPersonalToken();
                    token.Name = TokenName;
                    token.Expiration = TokenExpiration;
                    if (!string.IsNullOrEmpty(selectedScopes))
                    {
                        token.Scopes = selectedScopes.Split(new char[] { ',', ';' });
                    }
                    PersonalUserToken = await _patRepository.AddPersonalTokenAsync(User, token);
                    PersonalTokenName = token.Name;
                }
                catch (Exception ex)
                {
                    StatusMessage = ex.Message;
                }
            }
            return RedirectToPage();
        }
    }
}
