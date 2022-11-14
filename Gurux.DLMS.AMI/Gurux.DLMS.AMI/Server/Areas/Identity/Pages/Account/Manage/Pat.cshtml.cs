using Gurux.DLMS.AMI.Shared.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;

namespace Gurux.DLMS.AMI.Server.Areas.Identity.Pages.Account.Manage
{
    public partial class PatModel : PageModel
    {
        private readonly IPatRepository _patRepository;
        public PatModel(
            IPatRepository patRepository)
        {
            _patRepository = patRepository;
        }
        GXPersonalToken _active;

        public string GetSelectedClass(GXPersonalToken target)
        {
            return _active != null && target.Id == _active.Id ? "table-info" : "table-striped";
        }
        public object RowSelected(GXPersonalToken selected)
        {
            _active = selected;
            return null;
        }      

        [BindProperty]
        public GXPersonalToken[] Tokens { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                ViewData["Name"] = Properties.Resources.Name;
                ViewData["CreationTime"] = Properties.Resources.CreationTime;
                ViewData["Expiration"] = Properties.Resources.Expiration;
                ViewData["Add"] = Properties.Resources.Add;
                ViewData["Delete"] = Properties.Resources.Delete;
                Tokens = await _patRepository.GetPersonalTokensAsync(User, null);
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            return Page();
        }       
    }
}
