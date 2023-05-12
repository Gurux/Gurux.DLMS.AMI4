using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Gurux.DLMS.AMI.Shared.DIs;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using Gurux.DLMS.AMI.Client.Helpers;
using Gurux.DLMS.AMI.Client.Shared;
using System.Text.Json;
using Microsoft.AspNetCore.Components;

namespace Gurux.DLMS.AMI.Server.Areas.Identity.Pages.Account.Manage
{
    public partial class Notifications : PageModel
    {
        private IUserRepository _userRepository;
        private IUserSettingRepository _userSettingRepository;
        private TargetType _active = TargetType.None;
        private bool _allSelected = false;
        public string SearchText = "";

        /// <summary>
        /// Constructor.
        /// </summary>
        public Notifications(IUserRepository userRepository, IUserSettingRepository userSettingRepository)
        {
            _userRepository = userRepository;
            _userSettingRepository = userSettingRepository;
        }

        /// <summary>
        /// Status message.
        /// </summary>
        [TempData]
        public string? StatusMessage { get; set; }

        public string ToolTip
        {
            get
            {
                return _allSelected ? "Disable all notifications" : "Enable all notifications.";
            }
        }     

        /// <summary>
        /// Notification items.
        /// </summary>
        [BindProperty]
        public List<SelectListItem> Items { get; set; } = new List<SelectListItem>();

        /// <summary>
        /// Load user ignored notifiations.
        /// </summary>
        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["Save"] = Properties.Resources.Save;
            var user = await _userRepository.ReadAsync(User, null);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{User}'.");
            }
            //All notifications are ignored as a default.
            UInt64 ignored = UInt64.MaxValue;
            //Get ignored notifications.
            GXUserSetting? settings = user.Settings.Where(w => w.Name == GXConfigurations.Performance).SingleOrDefault();
            if (settings != null && !string.IsNullOrEmpty(settings.Value))
            {
                var s = JsonSerializer.Deserialize<PerformanceSettings>(settings.Value);
                if (s != null)
                {
                    ignored = (UInt64) s.IgnoreNotification;
                }
            }
            try
            {
                foreach (var it in ClientHelpers.GetNotifications())
                {
                    Items.Add(new SelectListItem(it.ToString(), ((UInt64)it).ToString(), (ignored & (UInt64)it) == 0));
                }
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
            return Page();
        }

        /// <summary>
        /// Save user ignored notifigations.
        /// </summary>
        /// <param name="selectedNotifications">Selected notifications</param>
        public async Task<IActionResult> OnPostAsync(string selectedNotifications)
        {
            var user = await _userRepository.ReadAsync(User, null);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{User}'.");
            }
            string[] values;
            if (selectedNotifications != null)
            {
                values = selectedNotifications.Split(new char[] { ',', ';' });
            }
            else
            {
                values = new string[0]; 
            }
            UInt64 selected = 0;
            foreach (var it in Enum.GetValues(typeof(TargetType)))
            {
                selected |= (UInt64)it;
            }
            UInt64 value;
            foreach (var it in values)
            {
                if (UInt64.TryParse(it, out value))
                {
                    selected &= ~value;
                }
            }
            GXUserSetting? settings = user.Settings.Where(w => w.Name == GXConfigurations.Performance).SingleOrDefault();
            if (settings == null)
            {
                settings = new GXUserSetting()
                {
                    Name = GXConfigurations.Performance,
                };
                user.Settings.Add(settings);
            }
            PerformanceSettings s = new PerformanceSettings()
            {
                IgnoreNotification = (TargetType) selected
            };
            settings.Value = JsonSerializer.Serialize(s);
            await _userSettingRepository.UpdateAsync(User, new GXUserSetting[] { settings });
            StatusMessage = "Notification updated.";
            return RedirectToPage();
        }
    }
}
