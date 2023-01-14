using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using BetterFurniture.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BetterFurniture.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<BetterFurnitureUser> _signInManager;
        private readonly UserManager<BetterFurnitureUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public SelectList RoleSelectList = new SelectList(
            new List<SelectListItem>
                {
                    new SelectListItem { Selected =true, Text = "Select Role", Value = ""},
                    new SelectListItem { Selected =false, Text = "Admin", Value = "Admin"},
                    new SelectListItem { Selected =false, Text = "Customer", Value = "Customer"},
            }, "Value", "Text", 1);

        public RegisterModel(
            UserManager<BetterFurnitureUser> userManager,
            SignInManager<BetterFurnitureUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Required(ErrorMessage = "you must fill in your full name.")]
            [Display(Name = "your full name")]
            public string CustomerFullName { get; set; }

            [Required(ErrorMessage = "plz enter your age")]
            [Range (12,100, ErrorMessage = "Only 12 years old and above are allowed to register. ")]
            public int CustomerAge { get; set; }

            [Required]
            [Display(Name = "user ole")]
            public string userrole { get; set; }

            //public string CustomerAddress { get; set; }

            //public DateTime CustomerDOB { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var user = new BetterFurnitureUser { 
                    UserName = Input.Email, 
                    Email = Input.Email ,
                    CustomerFullName = Input.CustomerFullName,
                    CustomerAge = Input.CustomerAge,
                    EmailConfirmed = true,
                    userrole = Input.userrole

                };
                var result = await _userManager.CreateAsync(user, Input.Password);
                bool roleresult = await _roleManager.RoleExistsAsync("Admin");
                if (!roleresult)
                {
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                }
                roleresult = await _roleManager.RoleExistsAsync("Customer");
                if (!roleresult)
                {
                    await _roleManager.CreateAsync(new IdentityRole("Customer"));
                }
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, Input.userrole);
                    //_logger.LogInformation("User created a new account with password.");

                    //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    //code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    //var callbackUrl = Url.Page(
                    //    "/Account/ConfirmEmail",
                    //    pageHandler: null,
                    //    values: new { area = "Identity", userId = user.Id, code = code, returnUrl = returnUrl },
                    //    protocol: Request.Scheme);

                    //await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                    //    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        //return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                        return RedirectToPage("Login");
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
