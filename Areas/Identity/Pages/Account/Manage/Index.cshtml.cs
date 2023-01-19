using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using BetterFurniture.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BetterFurniture.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel : PageModel
    {
        private readonly UserManager<BetterFurnitureUser> _userManager;
        private readonly SignInManager<BetterFurnitureUser> _signInManager;

        public IndexModel(
            UserManager<BetterFurnitureUser> userManager,
            SignInManager<BetterFurnitureUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }

            [Display(Name = "your full name")]
            public string CustomerFullName { get; set; }

            [Display(Name = "your age")]
            public int CustomerAge { get; set; }

            [Display(Name = "your address")]
            public string CustomerAddress { get; set; }

            [Display(Name = "your date of birth")]
            public DateTime CustomerDOB { get; set; }
        }

        private async Task LoadAsync(BetterFurnitureUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                CustomerFullName = user.CustomerFullName,
                CustomerAge = user.CustomerAge,
                CustomerAddress = user.CustomerAddress,
                CustomerDOB = user .CustomerDOB

            };
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

            if (Input.CustomerFullName != user.CustomerFullName)
            {
                user.CustomerFullName = Input.CustomerFullName;
            }
            if (Input.CustomerAddress != user.CustomerAddress)
            {
                user.CustomerAddress = Input.CustomerAddress;
            }
            if (Input.CustomerAge != user.CustomerAge)
            {
                user.CustomerAge = Input.CustomerAge;
            }
            if (Input.CustomerDOB != user.CustomerDOB)
            {
                user.CustomerDOB = Input.CustomerDOB;
            }
            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}
