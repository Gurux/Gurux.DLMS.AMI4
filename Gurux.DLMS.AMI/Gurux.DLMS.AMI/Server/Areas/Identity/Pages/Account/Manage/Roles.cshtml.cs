using Gurux.DLMS.AMI.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Gurux.DLMS.AMI.Server.Areas.Identity.Pages.Account.Manage
{
    public partial class RolesModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public RolesModel(
            UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public string[] Roles { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }
            var list = await _userManager.GetRolesAsync(user);
            if (list == null || list.Count == 0)
            {
                return NotFound($"Unable to load roles with ID '{_userManager.GetUserId(User)}'.");
            }
            Roles = new string[list.Count];
            list.CopyTo(Roles, 0);
            return Page();
        }
    }
}
