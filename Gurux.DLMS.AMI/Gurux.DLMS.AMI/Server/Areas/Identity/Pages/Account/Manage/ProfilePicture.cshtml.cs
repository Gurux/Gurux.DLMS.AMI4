using Gurux.DLMS.AMI.Server.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Gurux.DLMS.AMI.Server.Areas.Identity.Pages.Account.Manage
{
    public partial class ProfilePictureModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public ProfilePictureModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [Inject]
        public NavigationManager NavManager { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public UserUpdateModel Input { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }
            Input = new UserUpdateModel
            {
                ProfilePicture = user.ProfilePicture
            };

            try
            {
                ViewData["Add"] = Properties.Resources.Add;
                ViewData["Delete"] = Properties.Resources.Delete;
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }
            if (Request.Form.Files.Count > 0)
            {
                IFormFile file = Request.Form.Files.FirstOrDefault();
                using (var dataStream = new MemoryStream())
                {
                    await file.CopyToAsync(dataStream);
                    user.ProfilePicture = Convert.ToBase64String(dataStream.ToArray());
                }
                await _userManager.UpdateAsync(user);
                StatusMessage = Properties.Resources.YourProfileHasBeenUpdated;
            }
            return RedirectToPage();
        }
    }
}
