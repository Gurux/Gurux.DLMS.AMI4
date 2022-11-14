using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Server.Areas.Identity.Pages.Account.Manage
{
    public partial class PatDelete : PageModel
    {
        private readonly IPatRepository _patRepository;
        public PatDelete(
            IPatRepository patRepository)
        {
            _patRepository = patRepository;
        }

        [BindProperty]
        public GXPersonalToken Token { get; set; }

        [TempData]
        public string TokenId { get; set; }

        [TempData]
        public string StatusMessage { get; set; }
      
        public async Task<IActionResult> OnGetAsync(string id)
        {
            try
            {
                ViewData["Name"] = Properties.Resources.Name;
                ViewData["CreationTime"] = Properties.Resources.CreationTime;
                ViewData["Expiration"] = Properties.Resources.Expiration;
                ViewData["Delete"] = Properties.Resources.Delete;
                ViewData["Cancel"] = Properties.Resources.Cancel;
                if (id != null)
                {
                    TokenId = id;
                }
                Token = await _patRepository.GetPersonalTokenByIdAsync(User, TokenId);
                if (Token == null)
                {
                    throw new Exception("Unknown token.");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                StatusMessage = "Invalid token ID.";
            }
            else
            {
                try
                {
                    GXPersonalToken token =  await _patRepository.RemovePersonalTokenAsync(User, id);
                    StatusMessage = "Token removed: " + token.Name;
                    return RedirectToPage("Pat");
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
