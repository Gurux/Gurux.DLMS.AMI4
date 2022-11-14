// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Gurux.DLMS.AMI.Server.Models;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.DLMS.AMI.Shared.DIs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Gurux.DLMS.AMI.Server.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILocalizationRepository _localizationRepository;
        private readonly IUserRepository _userRepository;


        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILocalizationRepository localizationRepository,
            IUserRepository userRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _localizationRepository = localizationRepository;
            _userRepository = userRepository;
        }

        public SelectList AvailableCultures
        {
            get;
            set;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            public string GivenName { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            public string Surname { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            public DateTimeOffset? DateOfBirth { get; set; }
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            public string Culture { get; set; }
        }

        private void Localize()
        {
            ViewData["Name"] = Properties.Resources.Name;
            ViewData["PhoneNumber"] = Properties.Resources.PhoneNumber;
            ViewData["DateOfBirth"] = Properties.Resources.DateOfBirth;
            ViewData["Language"] = Properties.Resources.Language;
            ViewData["GivenName"] = Properties.Resources.GivenName;
            ViewData["Surname"] = Properties.Resources.Surname;
        }


        private async Task LoadAsync(ApplicationUser user)
        {
            List<GXLanguage> list = new List<GXLanguage>();
            list.Add(new GXLanguage() { Id = "", EnglishName = "" });
            list.AddRange(await _localizationRepository.GetInstalledCulturesAsync(null, true));
            AvailableCultures = new SelectList(list, nameof(GXLanguage.Id), nameof(GXLanguage.EnglishName));
            var userName = await _userManager.GetUserNameAsync(user);
            Username = userName;
            Input = new InputModel
            {
                PhoneNumber = user.PhoneNumber,
                GivenName = user.GivenName,
                Surname = user.Surname,
                DateOfBirth = user.DateOfBirth,
                Culture = user.Language
            };
            Localize();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }
            var updates = false;
            if (Input.GivenName != user.GivenName)
            {
                updates = true;
                user.GivenName = Input.GivenName;
            }
            if (Input.Surname != user.Surname)
            {
                updates = true;
                user.Surname = Input.Surname;
            }
            if (Input.DateOfBirth != user.DateOfBirth)
            {
                updates = true;
                user.DateOfBirth = Input.DateOfBirth;
            }
            if (Input.Culture != user.Language)
            {
                updates = true;
                user.Language = Input.Culture;
                if (user.Language != null)
                {
                    CultureInfo culture = new CultureInfo(user.Language);
                    Thread.CurrentThread.CurrentCulture = culture;
                    Thread.CurrentThread.CurrentUICulture = culture;
                }
            }
            if (updates)
            {
                await _userManager.UpdateAsync(user);
            }
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = Properties.Resources.YourProfileHasBeenUpdated;
            return RedirectToPage();
        }
    }
}
